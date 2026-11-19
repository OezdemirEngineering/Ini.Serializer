using Oeeng.Ini.Serializer.Attributes;

namespace Oeeng.Ini.Serializer.Tests;

internal sealed class NestedTemplateLevel2Settings
{
    [Template]
    public string Level2Template { get; set; } = string.Empty;
}
