using Oeeng.Ini.Serializer.Contracts;
using Oeeng.Ini.Serializer.Expressions;
using Oeeng.Ini.Serializer.Services;

namespace Oeeng.Ini.Serializer.Converters;

/// <summary>
/// Converts expression strings to and from <see cref="IniExpression"/> values.
/// </summary>
public sealed class ExpressionConverter : IIniValueConverter
{
    /// <summary>
    /// Determines whether the converter supports the provided target type.
    /// </summary>
    /// <param name="targetType">The type to evaluate.</param>
    /// <returns><see langword="true"/> if the type is <see cref="IniExpression"/>.</returns>
    public bool CanConvert(Type targetType)
    {
        Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return type == typeof(IniExpression);
    }

    /// <summary>
    /// Serializes an <see cref="IniExpression"/> to its expression string.
    /// </summary>
    /// <param name="value">The expression value to serialize.</param>
    /// <param name="declaredType">The declared target type.</param>
    /// <param name="serializer">The active serializer instance.</param>
    /// <returns>The raw expression string.</returns>
    public string Serialize(object? value, Type declaredType, IniSerializer serializer)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return ((IniExpression)value).Expression;
    }

    /// <summary>
    /// Deserializes expression text into an <see cref="IniExpression"/> instance.
    /// </summary>
    /// <param name="rawValue">The raw expression text.</param>
    /// <param name="targetType">The requested target type.</param>
    /// <param name="serializer">The active serializer instance.</param>
    /// <returns>An <see cref="IniExpression"/> instance.</returns>
    public object? Deserialize(string rawValue, Type targetType, IniSerializer serializer)
    {
        return new IniExpression(rawValue);
    }
}
