using Oeeng.Ini.Serializer.Converters;
using Oeeng.Ini.Serializer.Services;

namespace Oeeng.Ini.Serializer.Extensions;

/// <summary>
/// Provides extension methods to register expression conversion support.
/// </summary>
public static class RegisterExpressionConverterExtension
{
    /// <summary>
    /// Registers Roslyn-based expression support for <see cref="Expressions.IniExpression"/> values.
    /// </summary>
    /// <param name="iniSerializer">The serializer instance to configure.</param>
    /// <returns>The same <paramref name="iniSerializer"/> instance for fluent chaining.</returns>
    public static IniSerializer RegisterExpressions(this IniSerializer iniSerializer)
    {
        ArgumentNullException.ThrowIfNull(iniSerializer);

        return iniSerializer.RegisterConverter(new RoslynExpressionConverter());
    }
}
