using Ini.Serializer.Converters;
using Ini.Serializer.Services;

namespace Ini.Serializer.Extensions;

public static class RegisterExpressionConverterExtension
{
    public static IniSerializer RegisterExpressions(this IniSerializer iniSerializer)
    {
        ArgumentNullException.ThrowIfNull(iniSerializer);

        return iniSerializer.RegisterConverter(new RoslynExpressionConverter());
    }
}
