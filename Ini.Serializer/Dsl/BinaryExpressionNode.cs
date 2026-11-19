namespace Oeeng.Ini.Serializer.Dsl;

using Oeeng.Ini.Serializer.Dsl.Contracts;

public sealed record BinaryExpressionNode(IExpressionNode Left, ExpressionBinaryOperator Operator, IExpressionNode Right) : IExpressionNode
{
    public double Evaluate(IDictionary<string, double> variables)
    {
        double left = Left.Evaluate(variables);
        double right = Right.Evaluate(variables);

        return Operator switch
        {
            ExpressionBinaryOperator.Add => left + right,
            ExpressionBinaryOperator.Subtract => left - right,
            ExpressionBinaryOperator.Multiply => left * right,
            ExpressionBinaryOperator.Divide => left / right,
            ExpressionBinaryOperator.Modulo => left % right,
            ExpressionBinaryOperator.Power => Math.Pow(left, right),
            _ => throw new InvalidOperationException($"Unknown binary operator '{Operator}'.")
        };
    }
}
