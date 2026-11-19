namespace Oeeng.Ini.Serializer.Services;

/// <summary>
/// Stores mapped comment positions collected from INI text.
/// </summary>
internal sealed class CommentMap
{
    /// <summary>
    /// Gets full-line comments keyed by their original line index.
    /// </summary>
    public Dictionary<int, string> LineComments { get; } = [];

    /// <summary>
    /// Gets inline comments keyed by <c>section|key</c>.
    /// </summary>
    public Dictionary<string, string> InlineComments { get; } = new(StringComparer.OrdinalIgnoreCase);
}
