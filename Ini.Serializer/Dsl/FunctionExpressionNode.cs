namespace Oeeng.Ini.Serializer.Dsl;

using Oeeng.Ini.Serializer.Dsl.Contracts;

public sealed record FunctionExpressionNode(string Name, IReadOnlyList<IExpressionNode> Arguments) : IExpressionNode
{
    public double Evaluate(IDictionary<string, double> variables)
    {
        double[] values = Arguments.Select(argument => argument.Evaluate(variables)).ToArray();

        return Name.ToLowerInvariant() switch
        {
            "abs" => values.Length == 1 ? Math.Abs(values[0]) : throw InvalidArgumentCount(Name, 1),
            "sqrt" => values.Length == 1 ? Math.Sqrt(values[0]) : throw InvalidArgumentCount(Name, 1),
            "pow" => values.Length == 2 ? Math.Pow(values[0], values[1]) : throw InvalidArgumentCount(Name, 2),
            "min" => values.Length == 2 ? Math.Min(values[0], values[1]) : throw InvalidArgumentCount(Name, 2),
            "max" => values.Length == 2 ? Math.Max(values[0], values[1]) : throw InvalidArgumentCount(Name, 2),
            "sin" => values.Length == 1 ? Math.Sin(values[0]) : throw InvalidArgumentCount(Name, 1),
            "cos" => values.Length == 1 ? Math.Cos(values[0]) : throw InvalidArgumentCount(Name, 1),
            "tan" => values.Length == 1 ? Math.Tan(values[0]) : throw InvalidArgumentCount(Name, 1),
            "round" => values.Length == 1 ? Math.Round(values[0]) : throw InvalidArgumentCount(Name, 1),
            "floor" => values.Length == 1 ? Math.Floor(values[0]) : throw InvalidArgumentCount(Name, 1),
            "ceil" => values.Length == 1 ? Math.Ceiling(values[0]) : throw InvalidArgumentCount(Name, 1),
            "log" => values.Length == 1 ? Math.Log(values[0]) : throw InvalidArgumentCount(Name, 1),
            "exp" => values.Length == 1 ? Math.Exp(values[0]) : throw InvalidArgumentCount(Name, 1),
            _ => throw new NotSupportedException($"Expression function '{Name}' is not supported.")
        };
    }

    private static ArgumentException InvalidArgumentCount(string name, int count)
    {
        return new ArgumentException($"Function '{name}' requires exactly {count} argument(s).");
    }
}
