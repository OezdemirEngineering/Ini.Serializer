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
- template placeholders help keep secrets out of plain-text config files

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
- Optional Roslyn expression support via extension (`RegisterExpressions`)
- Utility entry point API via `IniFile.Instance`
- Template placeholders via `[Template]` for simple and nested properties
- Comment-aware round-trip after `Deserialize` (preserves full-line and inline comments)

---

## Installation / target

- Target framework: **.NET 10**

---

## NuGet packaging & publish

This project is configured for NuGet packaging in `Ini.Serializer.csproj` and includes:

- package metadata (`PackageId`, `Version`, description, tags)
- symbols package (`.snupkg`)
- README inside package (`PackageReadmeFile`)

Local package build:

```bash
dotnet pack Ini.Serializer/Ini.Serializer.csproj -c Release -o ./artifacts
```

---

## Quick start

```csharp
using Oeeng.Ini.Serializer;

var model = new AppSettings
{
	Name = "Demo",
	Port = 8080
};

string iniText = IniFile.Instance.Serialize(model);

var deserialized = new AppSettings();
IniFile.Instance.Deserialize(iniText, deserialized);
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
string ini = IniFile.Instance.Serialize(object dataModel);
object model = IniFile.Instance.Deserialize(string iniText, object dataModel, Dictionary<string, object>? replaceMappingList = null);

// optional only when needed:
string iniWithCustomConverter = IniFile.Instance
	.RegisterConverter(IIniValueConverter converter)
	.Serialize(object dataModel);
```

### Service API (additional usage)

```csharp
var serializer = new Oeeng.Ini.Serializer.Services.IniSerializer();
string ini = serializer.Serialize(model);
serializer.Deserialize(ini, model);
```

### Comment round-trip behavior

After `Deserialize`, the serializer remembers comment positions from the source INI.
On the next `Serialize` with the same model type, comments are applied back:

- full-line comments are inserted at their mapped line positions
- inline comments are appended to the corresponding `section|key` line

If you serialize a different model type, the comment map is discarded.
After applying comments once, the map is cleared.

Example:

```csharp
string ini = """
[AppSettings]
; global comment
Name=Demo ; keep this note
Port=8080
""";

var model = new AppSettings();
IniFile.Instance.Deserialize(ini, model);
model.Port = 9090;

string updated = IniFile.Instance.Serialize(model);
```

Result keeps comments and updates values.

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
using Oeeng.Ini.Serializer;
using Oeeng.Ini.Serializer.Extensions;

var serializer = IniFile.Instance
	.RegisterConverter(new MyCustomConverter());
```

> If multiple converters match the same target type, an `InvalidOperationException` is thrown.

---

## Example: custom value converter (Units-like type)

```csharp
using Oeeng.Ini.Serializer.Contracts;
using Oeeng.Ini.Serializer.Services;
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

string ini = IniFile.Instance
	.RegisterConverter(new MeasurementConverter())
	.Serialize(config);
```

Output:

```ini
[DeviceConfig]
Distance=12.5 m
```

---

## Example: template placeholders (`[Template]`)

You can mark properties with `[Template]` so serialization writes placeholders and deserialization can resolve them from a mapping dictionary.

A practical reason is security: plain-text config files increasingly become an attack surface when secrets are committed or shared by mistake. Template placeholders help avoid storing raw secrets in INI files by keeping only tokens in config and injecting real values at runtime.

Security intention: keep credentials, API keys, and environment-specific secrets out of repository-tracked INI files and resolve them during startup/deployment from secure sources.

Simple template property:

```csharp
public sealed class TemplatedSettings
{
	public string Name { get; set; } = string.Empty;

	[Template]
	public string TemplatedProperty { get; set; } = string.Empty;
}
```

Serialized output:

```ini
[TemplatedSettings]
Name=John
TemplatedProperty=%TemplatedProperty%
```

Nested templates are supported as well:

```csharp
public sealed class NestedTemplateRootSettings
{
	[Template]
	public string RootTemplate { get; set; } = string.Empty;

	public NestedTemplateLevel1Settings Level1 { get; set; } = new();
}

public sealed class NestedTemplateLevel1Settings
{
	[Template]
	public string Level1Template { get; set; } = string.Empty;

	public NestedTemplateLevel2Settings Level2 { get; set; } = new();
}

public sealed class NestedTemplateLevel2Settings
{
	[Template]
	public string Level2Template { get; set; } = string.Empty;
}
```

Deserialization with placeholder mapping:

```csharp
var model = new NestedTemplateRootSettings();

IniFile.Instance.Deserialize(iniText, model, new Dictionary<string, object>
{
	["RootToken"] = "root-value",
	["Level1Token"] = "level1-value",
	["Level2Token"] = "level2-value"
});
```

---

## Example: collection converter

Collections are not hardcoded in the serializer. Add support by registering `BracketCollectionConverter`:

```csharp
using Oeeng.Ini.Serializer.Converters;

string ini = IniFile.Instance
	.RegisterConverter(new BracketCollectionConverter())
	.RegisterConverter(new MeasurementConverter())
	.Serialize(model);
```

This enables syntax like:

```ini
Distances=[1.2 m,3.4 km]
```

---

## Example: runtime expressions (DSL, opt-in)

Expressions are parsed into an abstract syntax tree and evaluated by a custom traversal. No Roslyn runtime is required.

You can store expressions in INI and execute them at runtime after deserialization.

INI example:

```ini
[ExpressionSettings]
ExpressionY=x * x + 2
ExpressionZ=x * y + z
```

Model:

```csharp
using Oeeng.Ini.Serializer.Expressions;

public sealed class ExpressionSettings
{
	public IniExpression? ExpressionY { get; set; }
	public IniExpression? ExpressionZ { get; set; }
}
```

Usage:

```csharp
using Oeeng.Ini.Serializer;
using Oeeng.Ini.Serializer.Converters;

string iniText = """
[ExpressionSettings]
ExpressionY=x * x + 2
ExpressionZ=x * y + z
""";

var settings = new ExpressionSettings();
IniFile.Instance
	.RegisterConverter(new ExpressionConverter())
	.Deserialize(iniText, settings);

double y = settings.ExpressionY!.Execute(("x", 5));             // 27
double z = settings.ExpressionZ!.Execute(("x", 2), ("y", 4), ("z", 3)); // 11
```

### Why this is useful

- Keep INI human-readable while allowing dynamic formulas.
- Define calculation rules in config without recompiling the app.
- Supports flexible parameter counts (1..n variables).
- Uses a custom lexer, parser, AST, and visitor traversal instead of Roslyn.

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
