using Oeeng.Ini.Serializer.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Oeeng.Ini.Serializer.Extensions;

/// <summary>
/// Provides helper methods for type classification.
/// </summary>
internal static class TypeExtensions
{
    /// <summary>
    /// Determines whether a type should be treated as a nested complex model.
    /// </summary>
    /// <param name="type">The type to classify.</param>
    /// <returns><see langword="true"/> when the type represents a complex model.</returns>
    internal static bool IsComplexModelType(this Type type)
    {
        Type targetType = Nullable.GetUnderlyingType(type) ?? type;


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
}
