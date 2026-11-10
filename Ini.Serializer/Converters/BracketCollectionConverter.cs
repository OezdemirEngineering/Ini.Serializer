using System;
using System.Collections;
using System.Collections.Generic;

namespace Ini.Serializer.Converters;

public sealed class BracketCollectionConverter : IIniValueConverter
{
    public bool CanConvert(Type targetType)
    {
        Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }

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
