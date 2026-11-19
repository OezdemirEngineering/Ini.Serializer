using Oeeng.Ini.Serializer.Attributes;

namespace Oeeng.Ini.Serializer.Tests;

internal sealed class NestedTemplateRootSettings
{
    [Template]
    public string RootTemplate { get; set; } = string.Empty;

    public NestedTemplateLevel1Settings Level1 { get; set; } = new();
}
