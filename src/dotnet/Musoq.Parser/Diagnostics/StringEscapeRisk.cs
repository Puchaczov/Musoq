namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Describes the first value-changing escape found in an ordinary string literal.
/// </summary>
public readonly record struct StringEscapeRisk(
    string EscapeText,
    TextSpan Span,
    bool IsRootedPath,
    bool HasNonEscapeContent);

/// <summary>
///     Detects ordinary-string escapes that can change the authored value.
/// </summary>
public static class StringEscapeRiskDetector
{
    /// <summary>
    ///     Finds the first value-changing escape in <paramref name="innerText" />.
    /// </summary>
    /// <param name="innerText">The string contents without its quote delimiters.</param>
    /// <param name="sourceStart">The source offset of the first character in the contents.</param>
    /// <returns>The first risk, or <see langword="null" /> when the contents are safe.</returns>
    public static StringEscapeRisk? Find(ReadOnlySpan<char> innerText, int sourceStart)
    {
        return OrdinaryStringEscapeRiskDetector.Find(innerText, sourceStart) is not { } risk
            ? null
            : new StringEscapeRisk(
                risk.EscapeText,
                risk.Span,
                risk.IsRootedPath,
                risk.HasNonEscapeContent);
    }
}
