using Oeeng.Ini.Serializer.Services;

namespace Oeeng.Ini.Serializer;

/// <summary>
/// Provides convenience methods for serializing and deserializing INI text.
/// </summary>
public static class IniFile
{
    /// <summary>
    /// Serializes the provided data model into INI text.
    /// </summary>
    /// <param name="dataModel">The model instance to serialize.</param>
    /// <param name="configure">Optional serializer configuration callback (for registering converters).</param>
    /// <returns>The serialized INI text.</returns>
    public static string Serialize(object dataModel, Action<IniSerializer>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(dataModel);

        var serializer = new IniSerializer();
        configure?.Invoke(serializer);
        return serializer.Serialize(dataModel);
    }

    /// <summary>
    /// Deserializes INI text into a new instance of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target model type.</typeparam>
    /// <param name="iniText">The INI text to deserialize.</param>
    /// <param name="configure">Optional serializer configuration callback (for registering converters).</param>
    /// <returns>A populated instance of <typeparamref name="T"/>.</returns>
    public static T Deserialize<T>(string iniText, Action<IniSerializer>? configure = null)
        where T : new()
    {
        var serializer = new IniSerializer();
        configure?.Invoke(serializer);

        var model = new T();
        serializer.Deserialize(iniText, model);
        return model;
    }

    /// <summary>
    /// Deserializes INI text into the provided model instance.
    /// </summary>
    /// <param name="iniText">The INI text to deserialize.</param>
    /// <param name="dataModel">The target model instance to populate.</param>
    /// <param name="configure">Optional serializer configuration callback (for registering converters).</param>
    /// <returns>The same <paramref name="dataModel"/> instance after deserialization.</returns>
    public static object Deserialize(string iniText, object dataModel, Action<IniSerializer>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniText);
        ArgumentNullException.ThrowIfNull(dataModel);

        var serializer = new IniSerializer();
        configure?.Invoke(serializer);
        return serializer.Deserialize(iniText, dataModel);
    }
}
