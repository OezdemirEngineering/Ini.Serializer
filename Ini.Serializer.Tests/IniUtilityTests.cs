using System.Globalization;
using Shouldly;
using Ini.Serializer.Contracts;
using Ini.Serializer.Extensions;
using Ini.Serializer.Services;

namespace Ini.Serializer.Tests;

public class IniUtilityTests
{
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

    private readonly record struct Measurement(decimal Value, string Unit);

    private sealed class ConverterSettings
    {
        public Measurement Distance { get; set; }
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
}
