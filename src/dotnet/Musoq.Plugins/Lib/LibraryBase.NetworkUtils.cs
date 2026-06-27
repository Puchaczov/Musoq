using System.Text.RegularExpressions;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    [GeneratedRegex(@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b")]
    private static partial Regex ExtractIpsRegex();

    [GeneratedRegex("-+")]
    private static partial Regex RemoveConsecutiveDashesRegex();

    [GeneratedRegex("[^0-9A-Fa-f]")]
    private static partial Regex FormatMacRegex();

    [GeneratedRegex(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase)]
    private static partial Regex ExtractUrlsRegex();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex ExtractEmailsRegex();
}
