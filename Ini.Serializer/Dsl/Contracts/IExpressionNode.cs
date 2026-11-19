namespace Oeeng.Ini.Serializer.Dsl.Contracts;

using Oeeng.Ini.Serializer.Dsl.Contracts;

public interface IExpressionNode
{
    double Evaluate(IDictionary<string, double> variables);
}
