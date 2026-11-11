using System.Globalization;
using Shouldly;

using Ini.Serializer.Converters;
using Ini.Serializer.Extensions;
using Ini.Serializer.Contracts;
using Ini.Serializer.Services;

namespace Ini.Serializer.Tests;

public class SerializerTests
{
    [Fact]
    public void Serialize_ReturnsSectionAndKeysFromModelProperties()
    {
        var serializer = new IniSerializer();
        var model = new TestSettings
        {
            Name = "Max",
            Age = 30,
            IsEnabled = true
        };

        string iniText = serializer.Serialize(model);

        string[] lines = iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines[0].ShouldBe("[TestSettings]");
        lines.ShouldContain("Name=Max");
        lines.ShouldContain("Age=30");
        lines.ShouldContain("IsEnabled=True");
    }

    [Fact]
    public void Deserialize_SetsModelPropertiesFromIniText()
    {
        var serializer = new IniSerializer();
        string iniText = "[TestSettings]\nName=Anna\nAge=44\nIsEnabled=False\n";

        var model = new TestSettings();

        serializer.Deserialize(iniText, model);

        model.Name.ShouldBe("Anna");
        model.Age.ShouldBe(44);
        model.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Serialize_FormatsCollectionsAsBracketedList()
    {
        var serializer = new IniSerializer().RegisterConverter(new BracketCollectionConverter());
        var model = new CollectionSettings
        {
            Tags = new[] { "a1", "a2", "a3" },
            Numbers = new List<int> { 1, 2, 3 }
        };

        string iniText = serializer.Serialize(model);

        string[] lines = iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        lines.ShouldContain("Tags=[a1,a2,a3]");
        lines.ShouldContain("Numbers=[1,2,3]");
    }

    [Fact]
    public void Deserialize_ParsesCollectionsFromBracketedList()
    {
        var serializer = new IniSerializer().RegisterConverter(new BracketCollectionConverter());
        string iniText = "[CollectionSettings]\nTags=[x1,x2,x3]\nNumbers=[7,8,9]\n";

        var model = new CollectionSettings();

        serializer.Deserialize(iniText, model);

        model.Tags.ShouldBe(["x1", "x2", "x3"]);
        model.Numbers.ShouldBe([7, 8, 9]);
    }

    [Fact]
    public void Serialize_UsesNestedSectionsForNestedModels()
    {
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

        string iniText = serializer.Serialize(model);
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
        var serializer = new IniSerializer().RegisterConverter(new BracketCollectionConverter());
        string iniText = "[ParentSettings]\nName=Configured\n[ParentSettings.ChildSettings]\nFlag=False\nCodes=[1,2,3]\n";

        var model = new ParentSettings();

        serializer.Deserialize(iniText, model);

        model.Name.ShouldBe("Configured");
        model.Child.ShouldNotBeNull();
        model.Child.Flag.ShouldBeFalse();
        model.Child.Codes.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void Serialize_UsesRegisteredConvertersForCollectionOfClassElements()
    {
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

        string iniText = serializer.Serialize(model);
        string[] lines = iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        lines.ShouldContain("[ParentWithChildrenSettings]");
        lines.ShouldContain("Children=[True 10,False 20:30]");
    }

    [Fact]
    public void Deserialize_UsesRegisteredConvertersForCollectionOfClassElements()
    {
        var serializer = new IniSerializer()
            .RegisterConverter(new BracketCollectionConverter())
            .RegisterConverter(new ChildSettingsConverter());
        string iniText = "[ParentWithChildrenSettings]\nName=Root\nChildren=[True 10,False 20:30]\n";

        var model = new ParentWithChildrenSettings();

        serializer.Deserialize(iniText, model);

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
        var serializer = new IniSerializer()
            .RegisterConverter(new MeasurementConverter());

        var model = new ConverterSettings
        {
            Distance = new Measurement(12.5m, "m")
        };

        string iniText = serializer.Serialize(model);

        iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ShouldContain("Distance=12.5 m");
    }

    [Fact]
    public void Deserialize_UsesCustomConverterForUnsupportedType()
    {
        var serializer = new IniSerializer()
            .RegisterConverter(new MeasurementConverter());

        string iniText = "[ConverterSettings]\nDistance=99.9 km\n";
        var model = new ConverterSettings();

        serializer.Deserialize(iniText, model);

        model.Distance.Value.ShouldBe(99.9m);
        model.Distance.Unit.ShouldBe("km");
    }

    [Fact]
    public void IniUtility_DeserializeObject_UsesConfiguredSerializer()
    {
        string iniText = "[ConverterSettings]\nDistance=77.7 mm\n";
        var model = new ConverterSettings();

        object result = Ini.Deserialize(iniText, model, serializer =>
            serializer.RegisterConverter(new MeasurementConverter()));

        result.ShouldBeSameAs(model);
        model.Distance.Value.ShouldBe(77.7m);
        model.Distance.Unit.ShouldBe("mm");
    }

    [Fact]
    public void Serialize_UsesCustomConverterForCollectionElements()
    {
        var serializer = new IniSerializer()
            .RegisterConverter(new BracketCollectionConverter())
            .RegisterConverter(new MeasurementConverter());

        var model = new ConverterCollectionSettings
        {
            Distances = [new Measurement(1.2m, "m"), new Measurement(3.4m, "km")]
        };

        string iniText = serializer.Serialize(model);

        iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ShouldContain("Distances=[1.2 m,3.4 km]");
    }

    [Fact]
    public void Deserialize_UsesCustomConverterForCollectionElements()
    {
        var serializer = new IniSerializer()
            .RegisterConverter(new BracketCollectionConverter())
            .RegisterConverter(new MeasurementConverter());

        string iniText = "[ConverterCollectionSettings]\nDistances=[5.6 m,7.8 km]\n";
        var model = new ConverterCollectionSettings();

        serializer.Deserialize(iniText, model);

        model.Distances.Count.ShouldBe(2);
        model.Distances[0].Value.ShouldBe(5.6m);
        model.Distances[0].Unit.ShouldBe("m");
        model.Distances[1].Value.ShouldBe(7.8m);
        model.Distances[1].Unit.ShouldBe("km");
    }

    [Fact]
    public void Deserialize_UsesCustomConverterForNullableType()
    {
        var serializer = new IniSerializer()
            .RegisterConverter(new MeasurementConverter());

        string iniText = "[NullableConverterSettings]\nDistance=42 cm\n";
        var model = new NullableConverterSettings();

        serializer.Deserialize(iniText, model);

        model.Distance.HasValue.ShouldBeTrue();
        model.Distance.Value.Value.ShouldBe(42m);
        model.Distance.Value.Unit.ShouldBe("cm");
    }

    [Fact]
    public void IniUtility_Serialize_UsesConfiguredSerializer()
    {
        var model = new ConverterSettings
        {
            Distance = new Measurement(12.5m, "m")
        };

        string iniText = Ini.Serialize(model, serializer =>
            serializer.RegisterConverter(new MeasurementConverter()));

        iniText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ShouldContain("Distance=12.5 m");
    }

    [Fact]
    public void IniUtility_DeserializeGeneric_UsesConfiguredSerializer()
    {
        string iniText = "[ConverterSettings]\nDistance=99.9 km\n";

        var model = Ini.Deserialize<ConverterSettings>(iniText, serializer =>
            serializer.RegisterConverter(new MeasurementConverter()));

        model.Distance.Value.ShouldBe(99.9m);
        model.Distance.Unit.ShouldBe("km");
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

    private sealed class MeasurementConverter : IIniValueConverter
    {
        public bool CanConvert(Type targetType)
        {
            Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
            return type == typeof(Measurement);
        }

        public string Serialize(object? value, Type declaredType, global::Ini.Serializer.Services.IniSerializer serializer)
        {
            if (value is null)
            {
                return string.Empty;
            }

            Measurement measurement = (Measurement)value;
            return $"{measurement.Value.ToString(CultureInfo.InvariantCulture)} {measurement.Unit}";
        }

        public object? Deserialize(string rawValue, Type targetType, global::Ini.Serializer.Services.IniSerializer serializer)
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

        public string Serialize(object? value, Type declaredType, global::Ini.Serializer.Services.IniSerializer serializer)
        {
            if (value is not ChildSettings child)
            {
                return string.Empty;
            }

            return $"{child.Flag} {string.Join(':', child.Codes)}";
        }

        public object? Deserialize(string rawValue, Type targetType, global::Ini.Serializer.Services.IniSerializer serializer)
        {
            int separatorIndex = rawValue.IndexOf(' ');
            string flagPart = separatorIndex >= 0 ? rawValue[..separatorIndex] : rawValue;
            string codesPart = separatorIndex >= 0 ? rawValue[(separatorIndex + 1)..] : string.Empty;

            int[] codes = string.IsNullOrWhiteSpace(codesPart)
                ? []
                : codesPart.Split(':').Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();

            return new ChildSettings
            {
                Flag = bool.Parse(flagPart),
                Codes = codes
            };
        }
    }
}
