using System.Globalization;

namespace Oeeng.Ini.Serializer.Dsl;

public sealed class ExpressionLexer
{
    public IReadOnlyList<ExpressionToken> Tokenize(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        var tokens = new List<ExpressionToken>();
        int index = 0;

        while (index < expression.Length)
        {
            char current = expression[index];

            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (char.IsDigit(current) || (current == '.' && index + 1 < expression.Length && char.IsDigit(expression[index + 1])))
            {
                int start = index;
                while (index < expression.Length && (char.IsDigit(expression[index]) || expression[index] == '.'))
                {
                    index++;
                }

                if (index < expression.Length && (expression[index] == 'e' || expression[index] == 'E'))
                {
                    index++;
                    if (index < expression.Length && (expression[index] == '+' || expression[index] == '-')) index++;
                    while (index < expression.Length && char.IsDigit(expression[index])) index++;
                }

                string numberText = expression[start..index];
                if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    throw new FormatException($"Invalid number '{numberText}' at position {start}.");
                }

                tokens.Add(new ExpressionToken(ExpressionTokenKind.Number, numberText, start));
                continue;
            }

            if (char.IsLetter(current) || current == '_')
            {
                int start = index++;
                while (index < expression.Length && (char.IsLetterOrDigit(expression[index]) || expression[index] == '_')) index++;
                tokens.Add(new ExpressionToken(ExpressionTokenKind.Identifier, expression[start..index], start));
                continue;
            }

            int position = index;
            index++;

            ExpressionTokenKind kind = current switch
            {
                '+' => ExpressionTokenKind.Plus,
                '-' => ExpressionTokenKind.Minus,
                '*' => ExpressionTokenKind.Star,
                '/' => ExpressionTokenKind.Slash,
                '%' => ExpressionTokenKind.Percent,
                '^' => ExpressionTokenKind.Caret,
                '(' => ExpressionTokenKind.LeftParenthesis,
                ')' => ExpressionTokenKind.RightParenthesis,
                ',' => ExpressionTokenKind.Comma,
                _ => throw new FormatException($"Unexpected character '{current}' at position {position}.")
            };

            string operatorText = expression[position..index];
            tokens.Add(new ExpressionToken(kind, operatorText, position));
        }

        tokens.Add(new ExpressionToken(ExpressionTokenKind.End, string.Empty, expression.Length));
        return tokens;
    }
}
