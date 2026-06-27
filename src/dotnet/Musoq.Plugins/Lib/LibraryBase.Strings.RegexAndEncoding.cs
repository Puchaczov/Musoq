using System.Text.RegularExpressions;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    [GeneratedRegex(@"\\u([0-9A-Fa-f]{4})")]
    private static partial Regex UnicodeEscapeRegex();
}
