using Ini.Serializer.Contracts;
using Ini.Serializer.Expressions;
using Ini.Serializer.Services;

namespace Ini.Serializer.Converters;

public sealed class RoslynExpressionConverter : IIniValueConverter
{
    public bool CanConvert(Type targetType)
    {
        Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return type == typeof(IniExpression);
    }

    public string Serialize(object? value, Type declaredType, IniSerializer serializer)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return ((IniExpression)value).Expression;
    }

    public object? Deserialize(string rawValue, Type targetType, IniSerializer serializer)
    {
        return new IniExpression(rawValue);
    }
}
