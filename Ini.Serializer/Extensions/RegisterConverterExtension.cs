using System;
using Ini.Serializer.Converters;

namespace Ini.Serializer.Extensions;

public static class RegisterConverterExtension
{
    public static IniSerializer RegisterConverter(this IniSerializer iniSerializer, IIniValueConverter converter)
    {
        ArgumentNullException.ThrowIfNull(iniSerializer);
        ArgumentNullException.ThrowIfNull(converter);

        iniSerializer.CustomConverters.Add(converter);

        return iniSerializer;
    }

    internal static bool TryGetConverter(
        this IniSerializer iniSerializer, 
        Type targetType, 
        out IIniValueConverter converter)
    {
        for (int i = iniSerializer.CustomConverters.Count - 1; i >= 0; i--)
        {
            IIniValueConverter current = iniSerializer.CustomConverters[i];
            if (current.CanConvert(targetType))
            {
                converter = current;
                return true;
            }
        }

        converter = default;
        return false;
    }
}
