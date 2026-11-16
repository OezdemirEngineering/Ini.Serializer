using System.Globalization;
using Shouldly;

using Oeeng.Ini.Serializer.Converters;
using Oeeng.Ini.Serializer.Contracts;
using Oeeng.Ini.Serializer.Expressions;
using Oeeng.Ini.Serializer.Extensions;
using Oeeng.Ini.Serializer.Services;

namespace Oeeng.Ini.Serializer.Tests;

public class SerializerTests
{
    [Fact]
    public void Serialize_ReturnsSectionAndKeysFromModelProperties()
    {
        // Arrange
        var serializer = new IniSerializer();
        var model = new TestSettings
        {
            Name = "Max",
            Age = 30,
            IsEnabled = true
        };

        // Act
        string iniText = serializer.Serialize(model);

        // Assert
        string[] lines = iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines[0].ShouldBe("[TestSettings]");
        lines.ShouldContain("Name=Max");
        lines.ShouldContain("Age=30");
        lines.ShouldContain("IsEnabled=True");
    }

    [Fact]
    public void Deserialize_SetsModelPropertiesFromIniText()
    {
        // Arrange
        var serializer = new IniSerializer();
        string iniText = "[TestSettings]\nName=Anna\nAge=44\nIsEnabled=False\n";
        var model = new TestSettings();

        // Act
        serializer.Deserialize(iniText, model);

        // Assert
        model.Name.ShouldBe("Anna");
        model.Age.ShouldBe(44);
        model.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Serialize_AddsMappedCommentsAfterDeserialize()
    {
        // Arrange
        var serializer = new IniSerializer();
        string iniText = """
[TestSettings]
; remember me
Name=Anna ; inline comment
Age=44
IsEnabled=False
""";
        var model = new TestSettings();
        serializer.Deserialize(iniText, model);
        model.Age = 45;

        // Act
        string result = serializer.Serialize(model);

        // Assert
        result.ShouldContain("; remember me");
        result.ShouldContain("Name=Anna ; inline comment");
        result.ShouldContain("Age=45");
    }

    [Fact]
    public void Serialize_DropsMappedComments_WhenModelTypeMismatches()
    {
        // Arrange
        var serializer = new IniSerializer();
        string iniText = """
[TestSettings]
; remember me
Name=Anna
Age=44
IsEnabled=False
""";
        serializer.Deserialize(iniText, new TestSettings());
        var otherModel = new CollectionSettings
        {
            Tags = ["x1", "x2"],
            Numbers = [1, 2]
        };

        // Act
        string result = serializer
            .RegisterConverter(new BracketCollectionConverter())
            .Serialize(otherModel);

        // Assert
        result.ShouldNotContain("; remember me");
        result.ShouldContain("[CollectionSettings]");
    }

    [Fact]
    public void Serialize_FormatsCollectionsAsBracketedList()
    {
        // Arrange
        var serializer = new IniSerializer().RegisterConverter(new BracketCollectionConverter());
        var model = new CollectionSettings
        {
            Tags = ["a1", "a2", "a3"],
            Numbers = [1, 2, 3]
        };

        // Act
        string iniText = serializer.Serialize(model);

        // Assert
        string[] lines = iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.ShouldContain("Tags=[a1,a2,a3]");
        lines.ShouldContain("Numbers=[1,2,3]");
    }

    [Fact]
    public void Deserialize_ParsesCollectionsFromBracketedList()
    {
        // Arrange
        var serializer = new IniSerializer().RegisterConverter(new BracketCollectionConverter());
        string iniText = "[CollectionSettings]\nTags=[x1,x2,x3]\nNumbers=[7,8,9]\n";
        var model = new CollectionSettings();

        // Act
        serializer.Deserialize(iniText, model);

        // Assert
        model.Tags.ShouldBe(["x1", "x2", "x3"]);
        model.Numbers.ShouldBe([7, 8, 9]);
    }

    [Fact]
    public void Serialize_UsesNestedSectionsForNestedModels()
    {
        // Arrange
        var serializer = new IniSerializer().RegisterConverter(new BracketCollectionConverter());
        var model = new ParentSettings
        {
            Name = "Root",
            Child = new ChildSettings
            {
                Flag = true,
                Codes = [10, 20]
            }
        };

        // Act
        string iniText = serializer.Serialize(model);

        // Assert
        string[] lines = iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.ShouldContain("[ParentSettings]");
        lines.ShouldContain("Name=Root");
        lines.ShouldContain("[ParentSettings.ChildSettings]");
        lines.ShouldContain("Flag=True");
        lines.ShouldContain("Codes=[10,20]");
    }

    [Fact]
    public void Deserialize_ReadsNestedSectionIntoNestedModel()
    {
        // Arrange
        var serializer = new IniSerializer().RegisterConverter(new BracketCollectionConverter());
        string iniText = "[ParentSettings]\nName=Configured\n[ParentSettings.ChildSettings]\nFlag=False\nCodes=[1,2,3]\n";
        var model = new ParentSettings();

        // Act
        serializer.Deserialize(iniText, model);

        // Assert
        model.Name.ShouldBe("Configured");
        model.Child.ShouldNotBeNull();
        model.Child.Flag.ShouldBeFalse();
        model.Child.Codes.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void Serialize_UsesRegisteredConvertersForCollectionOfClassElements()
    {
        // Arrange
        var serializer = new IniSerializer()
            .RegisterConverter(new BracketCollectionConverter())
            .RegisterConverter(new ChildSettingsConverter());

        var model = new ParentWithChildrenSettings
        {
            Name = "Root",
            Children =
            [
                new ChildSettings { Flag = true, Codes = [10] },
                new ChildSettings { Flag = false, Codes = [20, 30] }
            ]
        };

        // Act
        string iniText = serializer.Serialize(model);

        // Assert
        string[] lines = iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.ShouldContain("[ParentWithChildrenSettings]");
        lines.ShouldContain("Children=[True 10,False 20:30]");
    }

    [Fact]
    public void Deserialize_UsesRegisteredConvertersForCollectionOfClassElements()
    {
        // Arrange
        var serializer = new IniSerializer()
            .RegisterConverter(new BracketCollectionConverter())
            .RegisterConverter(new ChildSettingsConverter());
        string iniText = "[ParentWithChildrenSettings]\nName=Root\nChildren=[True 10,False 20:30]\n";
        var model = new ParentWithChildrenSettings();

        // Act
        serializer.Deserialize(iniText, model);

        // Assert
        model.Name.ShouldBe("Root");
        model.Children.Count.ShouldBe(2);
        model.Children[0].Flag.ShouldBeTrue();
        model.Children[0].Codes.ShouldBe([10]);
        model.Children[1].Flag.ShouldBeFalse();
        model.Children[1].Codes.ShouldBe([20, 30]);
    }

    [Fact]
    public void Serialize_UsesCustomConverterForUnsupportedType()
    {
        // Arrange
        var serializer = new IniSerializer()
            .RegisterConverter(new MeasurementConverter());

        var model = new ConverterSettings
        {
            Distance = new Measurement(12.5m, "m")
        };

        // Act
        string iniText = serializer.Serialize(model);

        // Assert
        iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ShouldContain("Distance=12.5 m");
    }

    [Fact]
    public void Deserialize_UsesCustomConverterForUnsupportedType()
    {
        // Arrange
        var serializer = new IniSerializer()
            .RegisterConverter(new MeasurementConverter());

        string iniText = "[ConverterSettings]\nDistance=99.9 km\n";
        var model = new ConverterSettings();

        // Act
        serializer.Deserialize(iniText, model);

        // Assert
        model.Distance.Value.ShouldBe(99.9m);
        model.Distance.Unit.ShouldBe("km");
    }

    [Fact]
    public void Serialize_UsesCustomConverterForCollectionElements()
    {
        // Arrange
        var serializer = new IniSerializer()
            .RegisterConverter(new BracketCollectionConverter())
            .RegisterConverter(new MeasurementConverter());

        var model = new ConverterCollectionSettings
        {
            Distances = [new Measurement(1.2m, "m"), new Measurement(3.4m, "km")]
        };

        // Act
        string iniText = serializer.Serialize(model);

        // Assert
        iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ShouldContain("Distances=[1.2 m,3.4 km]");
    }

    [Fact]
    public void Deserialize_UsesCustomConverterForCollectionElements()
    {
        // Arrange
        var serializer = new IniSerializer()
            .RegisterConverter(new BracketCollectionConverter())
            .RegisterConverter(new MeasurementConverter());

        string iniText = "[ConverterCollectionSettings]\nDistances=[5.6 m,7.8 km]\n";
        var model = new ConverterCollectionSettings();

        // Act
        serializer.Deserialize(iniText, model);

        // Assert
        model.Distances.Count.ShouldBe(2);
        model.Distances[0].Value.ShouldBe(5.6m);
        model.Distances[0].Unit.ShouldBe("m");
        model.Distances[1].Value.ShouldBe(7.8m);
        model.Distances[1].Unit.ShouldBe("km");
    }

    [Fact]
    public void Deserialize_UsesCustomConverterForNullableType()
    {
        // Arrange
        var serializer = new IniSerializer()
            .RegisterConverter(new MeasurementConverter());

        string iniText = "[NullableConverterSettings]\nDistance=42 cm\n";
        var model = new NullableConverterSettings();

        // Act
        serializer.Deserialize(iniText, model);

        // Assert
        model.Distance.HasValue.ShouldBeTrue();
        model.Distance.Value.Value.ShouldBe(42m);
        model.Distance.Value.Unit.ShouldBe("cm");
    }

    [Fact]
    public void Deserialize_StoresRoslynExpression_WithSingleParameter()
    {
        // Arrange
        var serializer = new IniSerializer()
            .RegisterExpressions();

        string iniText = "[ExpressionSettings]\nExpressionY=x * x + 2\n";
        var model = new ExpressionSettings();

        // Act
        serializer.Deserialize(iniText, model);

        // Assert
        model.ExpressionY.ShouldNotBeNull();
        model.ExpressionY!.Execute(("x", 5)).ShouldBe(27d);
    }

    [Fact]
    public void Deserialize_StoresRoslynExpression_WithMultipleParameters()
    {
        // Arrange
        var serializer = new IniSerializer()
            .RegisterExpressions();

        string iniText = "[ExpressionSettings]\nExpressionY=x * y + z\n";
        var model = new ExpressionSettings();

        // Act
        serializer.Deserialize(iniText, model);

        // Assert
        model.ExpressionY.ShouldNotBeNull();
        model.ExpressionY!.Execute(("x", 2), ("y", 4), ("z", 3)).ShouldBe(11d);
    }

    private sealed class TestSettings
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsEnabled { get; set; }
    }

    private sealed class CollectionSettings
    {
        public string[] Tags { get; set; } = [];
        public List<int> Numbers { get; set; } = [];
    }

    private sealed class ParentSettings
    {
        public string Name { get; set; } = string.Empty;
        public ChildSettings? Child { get; set; }
    }

    private sealed class ChildSettings
    {
        public bool Flag { get; set; }
        public int[] Codes { get; set; } = [];
    }

    private sealed class ParentWithChildrenSettings
    {
        public string Name { get; set; } = string.Empty;
        public List<ChildSettings> Children { get; set; } = [];
    }

    private readonly record struct Measurement(decimal Value, string Unit);

    private sealed class ConverterSettings
    {
        public Measurement Distance { get; set; }
    }

    private sealed class ConverterCollectionSettings
    {
        public List<Measurement> Distances { get; set; } = [];
    }

    private sealed class NullableConverterSettings
    {
        public Measurement? Distance { get; set; }
    }

    private sealed class ExpressionSettings
    {
        public IniExpression? ExpressionY { get; set; }
    }

    private sealed class MeasurementConverter : IIniValueConverter
    {
        public bool CanConvert(Type targetType)
        {
            Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return type == typeof(Measurement);
        }

        public string Serialize(object? value, Type declaredType, IniSerializer serializer)
        {
            if (value is null)
            {
                return string.Empty;
            }

            Measurement measurement = (Measurement)value;
            return $"{measurement.Value.ToString(CultureInfo.InvariantCulture)} {measurement.Unit}";
        }

        public object? Deserialize(string rawValue, Type targetType, IniSerializer serializer)
        {
            int separatorIndex = rawValue.IndexOf(' ');
            string valuePart = separatorIndex >= 0 ? rawValue[..separatorIndex] : rawValue;
            string unitPart = separatorIndex >= 0 ? rawValue[(separatorIndex + 1)..] : string.Empty;
            return new Measurement(decimal.Parse(valuePart, CultureInfo.InvariantCulture), unitPart);
        }
    }

    private sealed class ChildSettingsConverter : IIniValueConverter
    {
        public bool CanConvert(Type targetType)
        {
            Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return type == typeof(ChildSettings);
        }

        public string Serialize(object? value, Type declaredType, IniSerializer serializer)
        {
            if (value is not ChildSettings child)
            {
                return string.Empty;
            }

            return $"{child.Flag} {string.Join(':', child.Codes)}";
        }

        public object? Deserialize(string rawValue, Type targetType, IniSerializer serializer)
        {
            int separatorIndex = rawValue.IndexOf(' ');
            string flagPart = separatorIndex >= 0 ? rawValue[..separatorIndex] : rawValue;
            string codesPart = separatorIndex >= 0 ? rawValue[(separatorIndex + 1)..] : string.Empty;

            int[] codes = string.IsNullOrWhiteSpace(codesPart)
                ? []
                : [.. codesPart.Split(':').Select(x => int.Parse(x, CultureInfo.InvariantCulture))];

            return new ChildSettings
            {
                Flag = bool.Parse(flagPart),
                Codes = codes
            };
        }
    }
}
