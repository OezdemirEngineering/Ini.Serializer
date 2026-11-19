using Oeeng.Ini.Serializer.Attributes;

namespace Oeeng.Ini.Serializer.Tests;

internal sealed class NestedTemplateLevel1Settings
{
    [Template]
    public string Level1Template { get; set; } = string.Empty;

    public NestedTemplateLevel2Settings Level2 { get; set; } = new();
}
