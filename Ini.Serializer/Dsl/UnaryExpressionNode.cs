namespace Oeeng.Ini.Serializer.Dsl;

using Oeeng.Ini.Serializer.Dsl.Contracts;

public sealed record UnaryExpressionNode(ExpressionUnaryOperator Operator, IExpressionNode Operand) : IExpressionNode
{
    public double Evaluate(IDictionary<string, double> variables)
    {
        double value = Operand.Evaluate(variables);
        return Operator switch
        {
            ExpressionUnaryOperator.Negate => -value,
            _ => throw new InvalidOperationException($"Unknown unary operator '{Operator}'.")
        };
    }
}
