using Oeeng.Ini.Serializer.Services;

namespace Oeeng.Ini.Serializer;

/// <summary>
/// Provides convenience access to an <see cref="IniSerializer"/> instance.
/// </summary>
public static class IniFile
{
    /// <summary>
    /// Gets a new serializer instance.
    /// </summary>
    public static IniSerializer Instance => new();
}
