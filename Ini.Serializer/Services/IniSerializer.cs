using System.Collections;
using System.Globalization;
using System.Reflection;
using Oeeng.Ini.Serializer.Attributes;
using Oeeng.Ini.Serializer.Contracts;
using Oeeng.Ini.Serializer.Extensions;

namespace Oeeng.Ini.Serializer.Services;

public class IniSerializer
{
    internal List<IIniValueConverter> CustomConverters { get; } = [];
    private Type? mappedCommentModelType;
    private CommentMap? mappedComments;

    /// <summary>
    /// Serializes the provided data model into INI text.
    /// </summary>
    /// <param name="dataModel">The model instance to serialize.</param>
    /// <returns>The serialized INI text.</returns>
    public string Serialize(object dataModel)
    {
        ArgumentNullException.ThrowIfNull(dataModel);

        string serializedStr = string.Empty;
        serializedStr = SerializeSection(dataModel, dataModel.GetType().Name, serializedStr);

        if (mappedComments is null)
        {
            return serializedStr;
        }

        if (mappedCommentModelType != dataModel.GetType())
        {
            ClearCommentMap();
            return serializedStr;
        }

        string merged = ApplyComments(serializedStr, mappedComments);
        ClearCommentMap();
        return merged;
    }

    /// <summary>
    /// Deserializes INI text into the provided model instance.
    /// </summary>
    /// <param name="iniText">The INI text to deserialize.</param>
    /// <param name="dataModel">The model instance to populate.</param>
    /// <returns>The same <paramref name="dataModel"/> instance after deserialization.</returns>
    public object Deserialize(string iniText, object dataModel, Dictionary<string, object>? replaceMappingList = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniText);
        ArgumentNullException.ThrowIfNull(dataModel);

        Type modelType = dataModel.GetType();
        mappedComments = ParseComments(iniText);
        mappedCommentModelType = modelType;

        var sections = ParseIniSections(iniText);
        DeserializeSection(dataModel, modelType.Name, sections, replaceMappingList);
        return dataModel;
    }

    internal string SerializeValue(object? value, Type declaredType)
    {
        if (value is null)
        {
            return string.Empty;
        }

        Type targetType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
        if (this.TryGetConverter(targetType, out var converter))
        {
            return converter!.Serialize(value, targetType, this);
        }

        if (targetType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(targetType))
        {
            throw new InvalidOperationException($"No converter registered for type '{targetType.FullName}'.");
        }

        if (targetType.IsClass && this.TryGetConverter(declaredType, out _))
        {
            throw new InvalidOperationException($"No converter registered for type '{targetType.FullName}'.");
        }

        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value.ToString() ?? string.Empty;
    }

    internal object? DeserializeValue(string rawValue, Type targetType)
    {
        Type effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (this.TryGetConverter(effectiveType, out var converter))
        {
            return converter!.Deserialize(rawValue, effectiveType, this);
        }

        if (effectiveType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(effectiveType))
        {
            throw new InvalidOperationException($"No converter registered for type '{effectiveType.FullName}'.");
        }

        if (effectiveType.IsClass && this.TryGetConverter(effectiveType, out _))
        {
            throw new InvalidOperationException($"No converter registered for type '{effectiveType.FullName}'.");
        }

        if (effectiveType == typeof(string))
        {
            return rawValue;
        }

        if (effectiveType.IsEnum)
        {
            return Enum.Parse(effectiveType, rawValue, ignoreCase: true);
        }

        return Convert.ChangeType(rawValue, effectiveType, CultureInfo.InvariantCulture);
    }

    private string SerializeSection(object dataModel, string sectionPath, string serializedStr)
    {
        serializedStr += string.Format("[{0}]{1}", sectionPath, Environment.NewLine);

        PropertyInfo[] props = dataModel.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (PropertyInfo prop in props)
        {
            if (!prop.CanRead)
            {
                continue;
            }

            object? value = prop.GetValue(dataModel, null);
            if (value is null)
            {
                continue;
            }


            if (!prop.IsTemplatedProperty() && !this.TryGetConverter(prop.PropertyType, out _) && prop.PropertyType.IsComplexModelType())
            {
                serializedStr = SerializeSection(value, string.Format("{0}.{1}", sectionPath, value.GetType().Name), serializedStr);
                continue;
            }

            var serializedValue = prop.IsTemplatedProperty() ? prop.GetTemplatePlaceholder() : SerializeValue(value, prop.PropertyType);

            serializedStr += string.Format("{0}={1}{2}", prop.Name, serializedValue, Environment.NewLine);
        }
        return serializedStr;
    }



    private void DeserializeSection(object dataModel, string sectionPath, Dictionary<string, Dictionary<string, string>> sections, Dictionary<string, object>? replaceMappingList)
    {
        PropertyInfo[] props = dataModel.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        sections.TryGetValue(sectionPath, out Dictionary<string, string>? valuesByName);

        foreach (PropertyInfo prop in props)
        {
            if (!prop.CanWrite)
            {
                continue;
            }

            if (!prop.IsTemplatedProperty() && !this.TryGetConverter(prop.PropertyType, out _) && prop.PropertyType.IsComplexModelType())
            {
                if (!prop.CanRead)
                {
                    continue;
                }

                object? nestedModel = prop.GetValue(dataModel, null);
                if (nestedModel is null)
                {
                    Type nestedType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    nestedModel = Activator.CreateInstance(nestedType);
                    if (nestedModel is null)
                    {
                        continue;
                    }

                    prop.SetValue(dataModel, nestedModel, null);
                }

                string nestedSectionPath = string.Format("{0}.{1}", sectionPath, nestedModel.GetType().Name);
                DeserializeSection(nestedModel, nestedSectionPath, sections, replaceMappingList);
                continue;
            }

            if (valuesByName is null || !valuesByName.TryGetValue(prop.Name, out string? rawValue))
            {
                continue;
            }
            object? convertedValue = null;
            if (prop.IsTemplatedProperty())
            {
                var trimmedValue = rawValue.Replace("%", "");
                replaceMappingList?.TryGetValue(trimmedValue, out convertedValue);
            }
            else
            {
                convertedValue = DeserializeValue(rawValue, prop.PropertyType);
            }


            prop.SetValue(dataModel, convertedValue, null);
        }
    }



    private static CommentMap ParseComments(string iniText)
    {
        var map = new CommentMap();
        string[] lines = iniText.Split(["\r\n", "\n"], StringSplitOptions.None);
        string? currentSection = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith(';') || trimmed.StartsWith('#'))
            {
                map.LineComments[i] = line;
                continue;
            }

            string cleanLine = StripComments(line).Trim();
            if (cleanLine.Length == 0)
            {
                continue;
            }

            if (cleanLine.StartsWith('[') && cleanLine.EndsWith(']') && cleanLine.Length > 2)
            {
                currentSection = cleanLine[1..^1].Trim();
                continue;
            }

            int separatorIndex = cleanLine.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            int commentIndex = FindCommentStartIndex(line);
            if (commentIndex <= separatorIndex)
            {
                continue;
            }

            string key = cleanLine[..separatorIndex].Trim();
            if (currentSection is null)
            {
                continue;
            }

            map.InlineComments[$"{currentSection}|{key}"] = line[commentIndex..].TrimStart();
        }

        return map;
    }

    private static Dictionary<string, Dictionary<string, string>> ParseIniSections(string iniText)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string[] lines = iniText.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        string? currentSection = null;

        foreach (string line in lines)
        {
            string trimmed = StripComments(line).Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']') && trimmed.Length > 2)
            {
                currentSection = trimmed[1..^1].Trim();
                if (!sections.ContainsKey(currentSection))
                {
                    sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                continue;
            }

            if (currentSection is null)
            {
                continue;
            }

            int separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string key = trimmed[..separatorIndex].Trim();
            string value = trimmed[(separatorIndex + 1)..].Trim();
            sections[currentSection][key] = value;
        }

        return sections;
    }

    private static string ApplyComments(string iniText, CommentMap commentMap)
    {
        string lineBreak = iniText.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var lines = iniText.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();

        foreach ((int index, string comment) in commentMap.LineComments.OrderBy(c => c.Key))
        {
            int insertIndex = Math.Clamp(index, 0, lines.Count);
            lines.Insert(insertIndex, comment);
        }

        string? currentSection = null;
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            string trimmed = line.Trim();

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']') && trimmed.Length > 2)
            {
                currentSection = trimmed[1..^1].Trim();
                continue;
            }

            if (currentSection is null)
            {
                continue;
            }

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string key = line[..separatorIndex].Trim();
            string mapKey = $"{currentSection}|{key}";
            if (!commentMap.InlineComments.TryGetValue(mapKey, out string? inlineComment))
            {
                continue;
            }

            string valuePart = line[(separatorIndex + 1)..];
            if (FindCommentStartIndex(valuePart) >= 0)
            {
                continue;
            }

            lines[i] = string.Concat(line, " ", inlineComment);
        }

        return string.Join(lineBreak, lines);
    }

    private void ClearCommentMap()
    {
        mappedComments = null;
        mappedCommentModelType = null;
    }

    private static string StripComments(string line)
    {
        int commentIndex = FindCommentStartIndex(line);
        return commentIndex >= 0 ? line[..commentIndex] : line;
    }

    private static int FindCommentStartIndex(string text)
    {
        bool escaped = false;

        for (int i = 0; i < text.Length; i++)
        {
            char current = text[i];

            if (!escaped && (current == ';' || current == '#'))
            {
                return i;
            }

            escaped = current == '\\' && !escaped;
            if (current != '\\')
            {
                escaped = false;
            }
        }

        return -1;
    }
}
