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
        out IIniValueConverter? converter)
    {

        var localizedConvertes = iniSerializer.CustomConverters.FindAll(c => c.CanConvert(targetType));

        if(localizedConvertes.Count > 1)
        {
            throw new InvalidOperationException($"Multiple converters found for type '{targetType.FullName}'.");
        }

        if(localizedConvertes.Count == 1)
        {
            converter = localizedConvertes[0];
            return true;
        }

        converter = default;
        return false;
    }
}
