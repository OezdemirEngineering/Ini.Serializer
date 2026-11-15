using Oeeng.Ini.Serializer.Converters;
using Oeeng.Ini.Serializer.Services;

namespace Oeeng.Ini.Serializer.Extensions;

public static class RegisterExpressionConverterExtension
{
    public static IniSerializer RegisterExpressions(this IniSerializer iniSerializer)
    {
        ArgumentNullException.ThrowIfNull(iniSerializer);

        return iniSerializer.RegisterConverter(new RoslynExpressionConverter());
    }
}
