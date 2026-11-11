using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Text.RegularExpressions;

namespace Ini.Serializer.Expressions;

public sealed class IniExpression
{
    private static readonly Regex IdentifierPattern = new(@"\b[A-Za-z_]\w*\b", RegexOptions.Compiled);

    private static readonly HashSet<string> ReservedTokens =
    [
        "true", "false", "null", "new", "Math"
    ];

    private static readonly ScriptOptions ScriptExecutionOptions = ScriptOptions.Default
        .AddImports("System")
        .AddReferences(typeof(Math).Assembly);

    public IniExpression(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        Expression = expression.Trim();
    }

    public string Expression { get; }

    public double Execute(IDictionary<string, double> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        string transformedExpression = TransformExpression(Expression);
        var globals = new ExpressionGlobals(variables);

        return CSharpScript.EvaluateAsync<double>(
            transformedExpression,
            ScriptExecutionOptions,
            globals).GetAwaiter().GetResult();
    }

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

    public override string ToString() => Expression;

    private static string TransformExpression(string expression)
    {
        return IdentifierPattern.Replace(expression, match =>
        {
            string token = match.Value;
            if (ReservedTokens.Contains(token))
            {
                return token;
            }

            int index = match.Index;
            if (index > 0 && expression[index - 1] == '.')
            {
                return token;
            }

            return $"Variables[\"{token}\"]";
        });
    }

    public sealed class ExpressionGlobals
    {
        public ExpressionGlobals(IDictionary<string, double> variables)
        {
            Variables = variables;
        }

        public IDictionary<string, double> Variables { get; }
    }
}
