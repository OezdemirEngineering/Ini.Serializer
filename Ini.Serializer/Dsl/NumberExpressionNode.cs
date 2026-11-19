namespace Oeeng.Ini.Serializer.Dsl;

using Oeeng.Ini.Serializer.Dsl.Contracts;

public sealed record NumberExpressionNode(double Value) : IExpressionNode
{
    public double Evaluate(IDictionary<string, double> variables) => Value;
}
