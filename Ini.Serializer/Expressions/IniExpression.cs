using Oeeng.Ini.Serializer.Dsl;
using Oeeng.Ini.Serializer.Dsl.Contracts;

namespace Oeeng.Ini.Serializer.Expressions;

/// <summary>
/// Represents a runtime-evaluable arithmetic expression loaded from INI text.
/// </summary>
public sealed class IniExpression
{
    private readonly IExpressionNode syntaxTree;

    /// <summary>
    /// Initializes a new expression instance from expression text.
    /// </summary>
    /// <param name="expression">The expression text (for example: <c>x * x + 2</c>).</param>
    public IniExpression(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        Expression = expression.Trim();
        syntaxTree = new ExpressionParser().Parse(Expression);
    }

    /// <summary>
    /// Gets the parsed abstract syntax tree for this expression.
    /// </summary>
    public IExpressionNode SyntaxTree => syntaxTree;

    /// <summary>
    /// Gets the original expression text.
    /// </summary>
    public string Expression { get; }

    /// <summary>
    /// Executes the expression with the provided variable map.
    /// </summary>
    /// <param name="variables">Variables used by the expression, keyed by variable name.</param>
    /// <returns>The evaluated numeric result.</returns>
    public double Execute(IDictionary<string, double> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        return syntaxTree.Evaluate(variables);
    }

    /// <summary>
    /// Executes the expression with positional parameter tuples.
    /// </summary>
    /// <param name="parameters">Name/value tuples mapped to expression variables.</param>
    /// <returns>The evaluated numeric result.</returns>
    public double Execute(params (string Name, double Value)[] parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach ((string Name, double Value) parameter in parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Name))
            {
                continue;
            }

            variables[parameter.Name] = parameter.Value;
        }

        return Execute(variables);
    }

    /// <summary>
    /// Returns the original expression text.
    /// </summary>
    /// <returns>The raw expression string.</returns>
    public override string ToString() => Expression;

}
