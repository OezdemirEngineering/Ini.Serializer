using Oeeng.Ini.Serializer.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Oeeng.Ini.Serializer.Extensions;

internal static class TypeExtensions
{
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
