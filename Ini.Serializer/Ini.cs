using Ini.Serializer.Services;
using System;

namespace Ini.Serializer;

public static class Ini
{
    public static string Serialize(object dataModel, Action<IniSerializer>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(dataModel);

        var serializer = new IniSerializer();
        configure?.Invoke(serializer);
        return serializer.Serialize(dataModel);
    }

    public static T Deserialize<T>(string iniText, Action<IniSerializer>? configure = null)
        where T : new()
    {
        var serializer = new IniSerializer();
        configure?.Invoke(serializer);

        var model = new T();
        serializer.Deserialize(iniText, model);
        return model;
    }

    public static object Deserialize(string iniText, object dataModel, Action<IniSerializer>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iniText);
        ArgumentNullException.ThrowIfNull(dataModel);

        var serializer = new IniSerializer();
        configure?.Invoke(serializer);
        return serializer.Deserialize(iniText, dataModel);
    }
}
