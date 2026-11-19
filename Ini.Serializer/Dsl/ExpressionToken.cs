namespace Oeeng.Ini.Serializer.Dsl;

public readonly record struct ExpressionToken(ExpressionTokenKind Kind, string Text, int Position)
{
}
