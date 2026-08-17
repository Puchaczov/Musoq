using System.Collections.Generic;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Extension methods for IDiagnosticException.
/// </summary>
public static class DiagnosticExceptionExtensions
{
    /// <summary>
    ///     Tries to convert an exception to a diagnostic if it implements IDiagnosticException.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <param name="sourceText">Optional source text for line/column information.</param>
    /// <param name="diagnostic">The resulting diagnostic, if successful.</param>
    /// <returns>True if the exception was converted to a diagnostic; otherwise false.</returns>
    public static bool TryToDiagnostic(this Exception exception, SourceText? sourceText, out Diagnostic? diagnostic)
    {
        if (TryGetDiagnosticException(exception, out var diagnosticException))
        {
            diagnostic = diagnosticException.ToDiagnostic(sourceText);
            return true;
        }

        diagnostic = null;
        return false;
    }

    /// <summary>
    ///     Converts an exception to a diagnostic, wrapping untyped failures as internal compiler failures.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <param name="sourceText">Optional source text for line/column information.</param>
    /// <returns>A diagnostic representing the exception.</returns>
    public static Diagnostic ToDiagnosticOrGeneric(this Exception exception, SourceText? sourceText = null)
    {
        if (TryGetDiagnosticException(exception, out var diagnosticException))
            return diagnosticException.ToDiagnostic(sourceText);

        return InternalDiagnosticException.ForCompiler(exception).ToDiagnostic(sourceText);
    }

    /// <summary>
    ///     Converts an exception to a diagnostic and records it as an error in the bag.
    /// </summary>
    /// <param name="bag">The diagnostic bag to add to.</param>
    /// <param name="exception">The exception to convert and record.</param>
    /// <param name="sourceText">Optional source text for line/column information.</param>
    /// <returns>True if the error was added; otherwise false.</returns>
    public static bool AddError(this DiagnosticBag bag, Exception exception, SourceText? sourceText)
    {
        var diagnostic = exception.ToDiagnosticOrGeneric(sourceText);
        return bag.Add(diagnostic);
    }

    private static bool TryGetDiagnosticException(Exception exception, out IDiagnosticException diagnosticException)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var current = exception;
        var visited = new HashSet<Exception>();

        while (current != null && visited.Add(current))
        {
            if (current is IDiagnosticException directDiagnosticException)
            {
                diagnosticException = directDiagnosticException;
                return true;
            }

            current = current.InnerException;
        }

        diagnosticException = default!;
        return false;
    }
}
