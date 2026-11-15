using Oeeng.Ini.Serializer.Contracts;
using Oeeng.Ini.Serializer.Services;
using System.Collections;

namespace Oeeng.Ini.Serializer.Converters;

/// <summary>
/// Converts enumerable values to and from bracket notation (for example: <c>[a,b,c]</c>).
/// </summary>
public sealed class BracketCollectionConverter : IIniValueConverter
{
    /// <summary>
    /// Determines whether the provided type is a supported enumerable type.
    /// </summary>
    /// <param name="targetType">The type to evaluate.</param>
    /// <returns><see langword="true"/> when the type is an enumerable (except <see cref="string"/>).</returns>
    public bool CanConvert(Type targetType)
    {
        Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }

    /// <summary>
    /// Serializes an enumerable value into bracket notation.
    /// </summary>
    /// <param name="value">The enumerable value to serialize.</param>
    /// <param name="declaredType">The declared type of the value.</param>
    /// <param name="serializer">The active serializer instance.</param>
    /// <returns>The serialized bracket notation string.</returns>
    public string Serialize(object? value, Type declaredType, IniSerializer serializer)
    {
        if (value is not IEnumerable values)
        {
            return string.Empty;
        }

        Type collectionType = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
        Type elementType = GetCollectionElementType(collectionType);
        var itemValues = new List<string>();
        foreach (object? item in values)
        {
            itemValues.Add(serializer.SerializeValue(item, elementType));
        }

        return $"[{string.Join(',', itemValues)}]";
    }

    /// <summary>
    /// Deserializes bracket notation into the target enumerable type.
    /// </summary>
    /// <param name="rawValue">The raw INI value in bracket notation.</param>
    /// <param name="targetType">The requested target enumerable type.</param>
    /// <param name="serializer">The active serializer instance.</param>
    /// <returns>The deserialized enumerable object.</returns>
    public object Deserialize(string rawValue, Type targetType, IniSerializer serializer)
    {
        Type collectionType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        Type elementType = GetCollectionElementType(collectionType);
        string[] items = ParseItems(rawValue);

        if (collectionType.IsArray)
        {
            Array array = Array.CreateInstance(elementType, items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                array.SetValue(serializer.DeserializeValue(items[i], elementType), i);
            }

            return array;
        }

        Type listType = typeof(List<>).MakeGenericType(elementType);
        object listInstance = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;

        foreach (string item in items)
        {
            addMethod.Invoke(listInstance, [serializer.DeserializeValue(item, elementType)]);
        }

        if (collectionType.IsAssignableFrom(listType))
        {
            return listInstance;
        }

        object? targetCollection = Activator.CreateInstance(collectionType);
        if (targetCollection is null)
        {
            return listInstance;
        }

        var targetAddMethod = collectionType.GetMethod("Add", [elementType]);
        if (targetAddMethod is null)
        {
            return listInstance;
        }

        foreach (string item in items)
        {
            targetAddMethod.Invoke(targetCollection, [serializer.DeserializeValue(item, elementType)]);
        }

        return targetCollection;
    }

    private static string[] ParseItems(string rawValue)
    {
        string value = rawValue.Trim();
        if (value.StartsWith('[') && value.EndsWith(']') && value.Length >= 2)
        {
            value = value[1..^1];
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value.Split(',', StringSplitOptions.TrimEntries);
    }

    private static Type GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType() ?? typeof(string);
        }

        if (collectionType.IsGenericType)
        {
            return collectionType.GetGenericArguments()[0];
        }

        foreach (Type implementedInterface in collectionType.GetInterfaces())
        {
            if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return implementedInterface.GetGenericArguments()[0];
            }
        }

        return typeof(string);
    }
}
