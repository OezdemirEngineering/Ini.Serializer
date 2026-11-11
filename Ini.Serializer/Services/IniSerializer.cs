using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Ini.Serializer.Contracts;
using Ini.Serializer.Extensions;

namespace Ini.Serializer.Services;

public class IniSerializer
{
    internal List<IIniValueConverter> CustomConverters { get; } = [];

    public string Serialize(object dataModel)
    {
        ArgumentNullException.ThrowIfNull(dataModel);

        string serializedStr = string.Empty;
        serializedStr = SerializeSection(dataModel, dataModel.GetType().Name, serializedStr);
        return serializedStr;
    }

    public object Deserialize(string iniText, object dataModel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniText);
        ArgumentNullException.ThrowIfNull(dataModel);

        Type modelType = dataModel.GetType();
        var sections = ParseIniSections(iniText);
        DeserializeSection(dataModel, modelType.Name, sections);
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

        if (targetType.IsClass && IsComplexModelType(targetType))
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
            return converter.Deserialize(rawValue, effectiveType, this);
        }

        if (effectiveType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(effectiveType))
        {
            throw new InvalidOperationException($"No converter registered for type '{effectiveType.FullName}'.");
        }

        if (effectiveType.IsClass && IsComplexModelType(effectiveType))
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
        serializedStr+=string.Format("[{0}]{1}", sectionPath, Environment.NewLine);

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

            if (IsComplexModelType(prop.PropertyType))
            {
                serializedStr = SerializeSection(value, string.Format("{0}.{1}", sectionPath, value.GetType().Name), serializedStr);
                continue;
            }

            serializedStr +=string.Format("{0}={1}{2}", prop.Name, SerializeValue(value, prop.PropertyType), Environment.NewLine);
        }
        return serializedStr;
    }

    private void DeserializeSection(object dataModel, string sectionPath, Dictionary<string, Dictionary<string, string>> sections)
    {
        PropertyInfo[] props = dataModel.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        sections.TryGetValue(sectionPath, out Dictionary<string, string>? valuesByName);

        foreach (PropertyInfo prop in props)
        {
            if (!prop.CanWrite)
            {
                continue;
            }

            if (IsComplexModelType(prop.PropertyType))
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
                DeserializeSection(nestedModel, nestedSectionPath, sections);
                continue;
            }

            if (valuesByName is null || !valuesByName.TryGetValue(prop.Name, out string? rawValue))
            {
                continue;
            }

            object? convertedValue = DeserializeValue(rawValue, prop.PropertyType);
            prop.SetValue(dataModel, convertedValue, null);
        }
    }

    private bool IsComplexModelType(Type type)
    {
        Type targetType = Nullable.GetUnderlyingType(type) ?? type;

        if (this.TryGetConverter(targetType, out _))
        {
            return false;
        }

        if (targetType == typeof(string))
        {
            return false;
        }

        if (targetType.IsEnum || targetType.IsPrimitive)
        {
            return false;
        }

        if (targetType == typeof(decimal) || targetType == typeof(DateTime) || targetType == typeof(DateTimeOffset) || targetType == typeof(TimeSpan) || targetType == typeof(Guid))
        {
            return false;
        }

        if (typeof(IEnumerable).IsAssignableFrom(targetType))
        {
            return false;
        }

        return true;
    }

    private static Dictionary<string, Dictionary<string, string>> ParseIniSections(string iniText)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string[] lines = iniText.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        string? currentSection = null;

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
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
}
