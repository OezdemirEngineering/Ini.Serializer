# Ini.Serializer

A lightweight .NET 10 library to serialize and deserialize objects to/from **INI text**, while keeping conversion fully extensible through **custom converter extensions**.

## Why this library?

INI is still useful when you want:

- human-readable config files
- simple key/value editing without heavy tooling
- easy diff/merge in Git
- low dependency footprint

At the same time, plain INI is often too limited for rich domain types (units, collections, value objects, etc.).

This library combines both worlds:

- INI format stays simple and readable
- serialization is model-based and reflection-driven
- any custom conversion strategy can be plugged in from outside

---

## Core idea

`IniSerializer` itself does not hardcode type-specific formatting rules.

Instead, it uses a converter pipeline (`IIniValueConverter`) you register with extensions.
This allows you to keep INI but still get flexible serialization behavior similar to modern config systems.

---

## Features

- Serialize object graph to INI string
- Deserialize INI string back into object instances
- Nested model sections via dot path (`[Parent.Child]`)
- Full converter extensibility (`IIniValueConverter`)
- Optional collection support via converter (`BracketCollectionConverter`)
- Optional Roslyn expression support via extension (`RegisterRoslynExpressions`)
- Utility entry point API (`Ini.Serialize`, `Ini.Deserialize`)

---

## Installation / target

- Target framework: **.NET 10**

---

## NuGet packaging & publish

This project is configured for NuGet packaging in `Ini.Serializer.csproj` and includes:

- package metadata (`PackageId`, `Version`, description, tags)
- symbols package (`.snupkg`)
- README inside package (`PackageReadmeFile`)

### Local pack

```bash
dotnet pack Ini.Serializer/Ini.Serializer.csproj -c Release -o ./artifacts
```

### GitHub publish workflow

Workflow: `.github/workflows/nuget-publish.yml`

- runs on tag push (`v*`) or manual dispatch
- packs and pushes `.nupkg` + `.snupkg` to NuGet.org

Publishing uses GitHub Actions OIDC. Configure a trusted publisher for this repository on NuGet.org and set the GitHub Actions repository variable `NUGET_USER` to the NuGet username associated with that trusted publisher. No `NUGET_API_KEY` repository secret is required.

---

## Quick start

```csharp
using Ini.Serializer;

var model = new AppSettings
{
	Name = "Demo",
	Port = 8080
};

string iniText = Ini.Serialize(model);

var deserialized = Ini.Deserialize<AppSettings>(iniText);
```

```csharp
public sealed class AppSettings
{
	public string Name { get; set; } = string.Empty;
	public int Port { get; set; }
}
```

Output:

```ini
[AppSettings]
Name=Demo
Port=8080
```

---

## API overview

### Utility API (recommended entry)

```csharp
string ini = Ini.Serialize(object dataModel, Action<IniSerializer>? configure = null);
T model = Ini.Deserialize<T>(string iniText, Action<IniSerializer>? configure = null) where T : new();
object model = Ini.Deserialize(string iniText, object dataModel, Action<IniSerializer>? configure = null);
```

### Service API

```csharp
var serializer = new Ini.Serializer.Services.IniSerializer();
string ini = serializer.Serialize(model);
serializer.Deserialize(ini, model);
```

---

## Nested model serialization

Nested classes become nested sections:

```csharp
public sealed class Parent
{
	public string Name { get; set; } = string.Empty;
	public Child Child { get; set; } = new();
}

public sealed class Child
{
	public bool Enabled { get; set; }
}
```

Output:

```ini
[Parent]
Name=Root
[Parent.Child]
Enabled=True
```

---

## Converter extension model

### Contract

```csharp
public interface IIniValueConverter
{
	bool CanConvert(Type targetType);
	string Serialize(object? value, Type declaredType, IniSerializer serializer);
	object? Deserialize(string rawValue, Type targetType, IniSerializer serializer);
}
```

### Registration

```csharp
using Ini.Serializer.Extensions;

var serializer = new Ini.Serializer.Services.IniSerializer()
	.RegisterConverter(new MyCustomConverter());
```

> If multiple converters match the same target type, an `InvalidOperationException` is thrown.

---

## Example: custom value converter (Units-like type)

```csharp
using Ini.Serializer.Contracts;
using Ini.Serializer.Services;
using System.Globalization;

public readonly record struct Measurement(decimal Value, string Unit);

public sealed class MeasurementConverter : IIniValueConverter
{
	public bool CanConvert(Type targetType)
	{
		var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
		return type == typeof(Measurement);
	}

	public string Serialize(object? value, Type declaredType, IniSerializer serializer)
	{
		if (value is null) return string.Empty;
		var m = (Measurement)value;
		return $"{m.Value.ToString(CultureInfo.InvariantCulture)} {m.Unit}";
	}

	public object? Deserialize(string rawValue, Type targetType, IniSerializer serializer)
	{
		int i = rawValue.IndexOf(' ');
		string number = i >= 0 ? rawValue[..i] : rawValue;
		string unit = i >= 0 ? rawValue[(i + 1)..] : string.Empty;
		return new Measurement(decimal.Parse(number, CultureInfo.InvariantCulture), unit);
	}
}
```

Use it:

```csharp
var config = new DeviceConfig { Distance = new Measurement(12.5m, "m") };

string ini = Ini.Serialize(config, s => s.RegisterConverter(new MeasurementConverter()));
```

---

## Example: collection converter

Collections are not hardcoded in the serializer. Add support by registering `BracketCollectionConverter`:

```csharp
using Ini.Serializer.Converters;

string ini = Ini.Serialize(model, s =>
{
	s.RegisterConverter(new BracketCollectionConverter());
	s.RegisterConverter(new MeasurementConverter());
});
```

This enables syntax like:

```ini
Distances=[1.2 m,3.4 km]
```

---

## Example: runtime expressions (Roslyn, opt-in)

You can store expressions in INI and execute them at runtime after deserialization.

INI example:

```ini
[ExpressionSettings]
ExpressionY=x * x + 2
ExpressionZ=x * y + z
```

Model:

```csharp
using Ini.Serializer.Expressions;

public sealed class ExpressionSettings
{
	public IniExpression? ExpressionY { get; set; }
	public IniExpression? ExpressionZ { get; set; }
}
```

Usage:

```csharp
using Ini.Serializer;
using Ini.Serializer.Extensions;

string iniText = """
[ExpressionSettings]
ExpressionY=x * x + 2
ExpressionZ=x * y + z
""";

ExpressionSettings settings = Ini.Deserialize<ExpressionSettings>(
	iniText,
	s => s.RegisterRoslynExpressions());

double y = settings.ExpressionY!.Execute(("x", 5));             // 27
double z = settings.ExpressionZ!.Execute(("x", 2), ("y", 4), ("z", 3)); // 11
```

### Why this is useful

- Keep INI human-readable while allowing dynamic formulas.
- Define calculation rules in config without recompiling the app.
- Supports flexible parameter counts (1..n variables).

> Expression execution is intentionally opt-in via extension registration.

---

## Why keep INI but still use converter-based serialization?

If you want to stay with INI (operations, legacy systems, manual editing), you usually lose flexibility for rich data types.

This library addresses that by giving you:

- **INI readability** (simple sections and key/value lines)
- **Serialization consistency** (reflection + type pipeline)
- **YAML-like extensibility mindset** (pluggable conversion rules per type)
- **Domain independence** (external converters for units, identifiers, custom value objects)

So you can keep the operational simplicity of INI while still modeling complex application types cleanly.

---

## Notes

- Converters are resolved by `CanConvert(Type)`.
- Converter registration order matters only if multiple converters can match; avoid overlap.
- For unsupported types without converter, serialization/deserialization throws a clear exception.

---

## License

Use according to your project license policy.
