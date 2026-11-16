namespace Oeeng.Ini.Serializer.Services;

internal sealed class CommentMap
{
    public Dictionary<int, string> LineComments { get; } = [];

    public Dictionary<string, string> InlineComments { get; } = new(StringComparer.OrdinalIgnoreCase);
}
