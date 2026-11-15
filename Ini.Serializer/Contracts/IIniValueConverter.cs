using Oeeng.Ini.Serializer.Services;

namespace Oeeng.Ini.Serializer.Contracts;

/// <summary>
/// Defines a custom value converter for INI serialization and deserialization.
/// </summary>
public interface IIniValueConverter
{
    /// <summary>
    /// Determines whether this converter can handle the specified target type.
    /// </summary>
    /// <param name="targetType">The type to evaluate.</param>
    /// <returns><see langword="true"/> if the type is supported; otherwise <see langword="false"/>.</returns>
    bool CanConvert(Type targetType);

    /// <summary>
    /// Serializes a value to its INI string representation.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="declaredType">The declared type of the value.</param>
    /// <param name="serializer">The active serializer instance.</param>
    /// <returns>The serialized INI string representation.</returns>
    string Serialize(object? value, Type declaredType, IniSerializer serializer);

    /// <summary>
    /// Deserializes an INI string value into the requested target type.
    /// </summary>
    /// <param name="rawValue">The raw INI value.</param>
    /// <param name="targetType">The requested target type.</param>
    /// <param name="serializer">The active serializer instance.</param>
    /// <returns>The deserialized object value.</returns>
    object? Deserialize(string rawValue, Type targetType, IniSerializer serializer);
}
