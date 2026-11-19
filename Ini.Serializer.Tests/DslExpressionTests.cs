using Shouldly;
using Oeeng.Ini.Serializer.Dsl;
using Oeeng.Ini.Serializer.Dsl.Contracts;

namespace Oeeng.Ini.Serializer.Tests;

public class DslExpressionTests
{
    [Fact]
    public void Parser_BuildsAst_ForArithmeticExpression()
    {
        // Arrange
        var parser = new ExpressionParser();

        // Act
        IExpressionNode root = parser.Parse("(x + 2) * 3");

        // Assert
        root.ShouldBeOfType<BinaryExpressionNode>();
        root.Evaluate(new Dictionary<string, double> { ["x"] = 4d }).ShouldBe(18d);
    }

    [Fact]
    public void Evaluate_SupportsArithmeticOperators()
    {
        // Arrange
        var parser = new ExpressionParser();

        // Act
        double result = parser.Parse("10 + 4 * 2 - 8 / 2 % 3 ^ 2")
            .Evaluate(new Dictionary<string, double>());

        // Assert
        result.ShouldBe(14d);
    }

    [Fact]
    public void Evaluate_SupportsFunctions()
    {
        // Arrange
        var parser = new ExpressionParser();

        // Act
        double result = parser.Parse("max(sqrt(x), abs(y))").Evaluate(
            new Dictionary<string, double>
            {
                ["x"] = 16d,
                ["y"] = -3d
            });

        // Assert
        result.ShouldBe(4d);
    }
}
