namespace Oeeng.Ini.Serializer.Dsl;

using Oeeng.Ini.Serializer.Dsl.Contracts;

public sealed record VariableExpressionNode(string Name) : IExpressionNode
{
    public double Evaluate(IDictionary<string, double> variables)
    {
        if (!variables.TryGetValue(Name, out double value))
        {
            throw new KeyNotFoundException($"Expression variable '{Name}' was not provided.");
        }

        return value;
    }
}
