using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;

namespace Musoq.Formatter;

/// <summary>A complete formatted script or original-input diagnostics without partial output.</summary>
public sealed class FormattingResult
{
    internal FormattingResult(string? formattedText, IEnumerable<Diagnostic> diagnostics)
    {
        FormattedText = formattedText;
        Diagnostics = Array.AsReadOnly(diagnostics.OrderBy(d => d.Location.Offset).ToArray());
    }

    /// <summary>True when a complete formatted script is available.</summary>
    public bool IsSuccess => FormattedText != null;
    /// <summary>Complete output on success, including an empty string for blank input; null on failure.</summary>
    public string? FormattedText { get; }
    /// <summary>Immutable diagnostics ordered by original input location.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}
