
using System.Globalization;
using Oeeng.Ini.Serializer.Dsl.Contracts;

namespace Oeeng.Ini.Serializer.Dsl;

public sealed class ExpressionParser
{
    public IExpressionNode Parse(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        var state = new ParserState(
            new ExpressionLexer().Tokenize(expression));

        var result = state.ParseExpression();

        state.Expect(ExpressionTokenKind.End);

        return result;
    }

    private sealed class ParserState(IReadOnlyList<ExpressionToken> tokens)
    {
        private int position;

        private ExpressionToken Current => tokens[position];

        public void Expect(ExpressionTokenKind kind)
        {
            if (!Match(kind))
                throw new FormatException(
                    $"Expected {kind}, found '{Current.Text}' " +
                    $"at position {Current.Position}.");
        }

        private bool Match(ExpressionTokenKind kind)
        {
            if (Current.Kind != kind)
                return false;

            position++;
            return true;
        }

        private ExpressionToken Consume()
        {
            return tokens[position++];
        }

        public IExpressionNode ParseExpression(int minPrecedence = 0)
        {
            IExpressionNode left = ParsePrimary();

            while (TryGetOperator(
                Current.Kind,
                out var op,
                out int precedence,
                out bool rightAssociative)
                && precedence >= minPrecedence)
            {
                Consume();

                int nextPrecedence = rightAssociative
                    ? precedence
                    : precedence + 1;

                IExpressionNode right = ParseExpression(nextPrecedence);

                left = new BinaryExpressionNode(left, op, right);
            }

            return left;
        }

        private IExpressionNode ParsePrimary()
        {
            ExpressionToken token = Current;

            switch (token.Kind)
            {
                case ExpressionTokenKind.Minus:
                    Consume();
                    return new UnaryExpressionNode(
                        ExpressionUnaryOperator.Negate,
                        ParseExpression(4));

                case ExpressionTokenKind.Number:
                    Consume();
                    return new NumberExpressionNode(
                        double.Parse(
                            token.Text,
                            CultureInfo.InvariantCulture));

                case ExpressionTokenKind.Identifier:
                    Consume();

                    if (!Match(ExpressionTokenKind.LeftParenthesis))
                        return new VariableExpressionNode(token.Text);

                    var arguments = new List<IExpressionNode>();

                    if (Current.Kind != ExpressionTokenKind.RightParenthesis)
                    {
                        do
                        {
                            arguments.Add(ParseExpression());
                        }
                        while (Match(ExpressionTokenKind.Comma));
                    }

                    Expect(ExpressionTokenKind.RightParenthesis);

                    return new FunctionExpressionNode(
                        token.Text,
                        arguments);

                case ExpressionTokenKind.LeftParenthesis:
                    Consume();

                    IExpressionNode expression = ParseExpression();

                    Expect(ExpressionTokenKind.RightParenthesis);

                    return expression;

                default:
                    throw new FormatException(
                        $"Unexpected token '{token.Text}' " +
                        $"at position {token.Position}.");
            }
        }

        private static bool TryGetOperator(
            ExpressionTokenKind kind,
            out ExpressionBinaryOperator op,
            out int precedence,
            out bool rightAssociative)
        {
            (op, precedence, rightAssociative) = kind switch
            {
                ExpressionTokenKind.Plus =>
                    (ExpressionBinaryOperator.Add, 1, false),

                ExpressionTokenKind.Minus =>
                    (ExpressionBinaryOperator.Subtract, 1, false),

                ExpressionTokenKind.Star =>
                    (ExpressionBinaryOperator.Multiply, 2, false),

                ExpressionTokenKind.Slash =>
                    (ExpressionBinaryOperator.Divide, 2, false),

                ExpressionTokenKind.Percent =>
                    (ExpressionBinaryOperator.Modulo, 2, false),

                ExpressionTokenKind.Caret =>
                    (ExpressionBinaryOperator.Power, 3, true),

                _ => (default, 0, false)
            };

            return precedence != 0;
        }
    }
}