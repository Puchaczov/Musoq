namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Interface for exceptions that can be converted to diagnostic information.
///     Implementing this interface allows exceptions to provide rich error information
///     suitable for IDE/LSP integration.
/// </summary>
public interface IDiagnosticException
{
    /// <summary>
    ///     Gets the diagnostic code for this exception type.
    /// </summary>
    DiagnosticCode Code { get; }

    /// <summary>
    ///     Gets the text span where this error occurred, if known.
    /// </summary>
    TextSpan? Span { get; }

    /// <summary>
    ///     Converts this exception to a Diagnostic instance.
    /// </summary>
    /// <param name="sourceText">Optional source text for computing line/column information.</param>
    /// <returns>A Diagnostic instance representing this error.</returns>
    Diagnostic ToDiagnostic(SourceText? sourceText = null);
}
