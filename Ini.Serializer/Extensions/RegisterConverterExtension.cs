using Oeeng.Ini.Serializer.Contracts;
using Oeeng.Ini.Serializer.Services;

namespace Oeeng.Ini.Serializer.Extensions;

/// <summary>
/// Provides converter registration helpers for <see cref="IniSerializer"/>.
/// </summary>
public static class RegisterConverterExtension
{
    /// <summary>
    /// Registers a value converter used during serialization and deserialization.
    /// </summary>
    /// <param name="iniSerializer">The serializer instance to configure.</param>
    /// <param name="converter">The converter implementation to register.</param>
    /// <returns>The same <paramref name="iniSerializer"/> instance for fluent chaining.</returns>
    public static IniSerializer RegisterConverter(this IniSerializer iniSerializer, IIniValueConverter converter)
    {
        ArgumentNullException.ThrowIfNull(iniSerializer);
        ArgumentNullException.ThrowIfNull(converter);

        iniSerializer.CustomConverters.Add(converter);

        return iniSerializer;
    }

    /// <summary>
    /// Attempts to resolve a single converter for the given target type.
    /// </summary>
    /// <param name="iniSerializer">The serializer that owns the converter collection.</param>
    /// <param name="targetType">The type to resolve.</param>
    /// <param name="converter">When this method returns, contains the converter if found.</param>
    /// <returns><see langword="true"/> when exactly one converter is resolved.</returns>
    internal static bool TryGetConverter(
        this IniSerializer iniSerializer,
        Type targetType,
        out IIniValueConverter? converter)
    {

        var localizedConvertes = iniSerializer.CustomConverters.FindAll(c => c.CanConvert(targetType));

        if (localizedConvertes.Count > 1)
        {
            throw new InvalidOperationException($"Multiple converters found for type '{targetType.FullName}'.");
        }

        if (localizedConvertes.Count == 1)
        {
            converter = localizedConvertes[0];
            return true;
        }

        converter = default;
        return false;
    }
}
