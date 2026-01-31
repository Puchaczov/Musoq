#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
        if (exception is IDiagnosticException diagnosticException)
        {
            diagnostic = diagnosticException.ToDiagnostic(sourceText);
            return true;
        }

        diagnostic = null;
        return false;
    }

    /// <summary>
    ///     Converts an exception to a diagnostic, falling back to a generic error if
    ///     the exception doesn't implement IDiagnosticException.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <param name="sourceText">Optional source text for line/column information.</param>
    /// <returns>A diagnostic representing the exception.</returns>
    public static Diagnostic ToDiagnosticOrGeneric(this Exception exception, SourceText? sourceText = null)
    {
        if (exception is IDiagnosticException diagnosticException) return diagnosticException.ToDiagnostic(sourceText);


        var message = MakeUserFriendlyMessage(exception);

        return Diagnostic.Error(
            DiagnosticCode.MQ9999_Unknown,
            message,
            TextSpan.Empty);
    }

    private static string MakeUserFriendlyMessage(Exception exception)
    {
        var originalMessage = exception.Message;


        switch (exception)
        {
            case KeyNotFoundException:


                if (originalMessage.Contains("was not present in the dictionary"))
                {
                    var keyMatch = Regex.Match(originalMessage, @"'([^']+)'");
                    if (keyMatch.Success)
                        return $"Internal error: Reference '{keyMatch.Groups[1].Value}' could not be resolved. " +
                               "This may indicate a malformed query structure. Please verify your query syntax.";
                }

                return $"Internal error: A required reference could not be found. {originalMessage}";

            case NullReferenceException:
                return "Internal error: A null reference was encountered during query processing. " +
                       "This may indicate an invalid query structure.";

            case ArgumentNullException argNull:
                return $"Internal error: Required value '{argNull.ParamName}' was not provided. " +
                       "Please verify your query is complete.";

            case ArgumentException arg when arg.ParamName != null:
                return $"Invalid argument '{arg.ParamName}': {originalMessage}";

            case InvalidOperationException:

                return $"Query processing error: {originalMessage}";

            case NotSupportedException:
                return $"Unsupported operation: {originalMessage}";

            case IndexOutOfRangeException:
                return "Internal error: An index was out of range during query processing. " +
                       "This may indicate a malformed query with mismatched elements.";

            default:

                return $"Unexpected error ({exception.GetType().Name}): {originalMessage}";
        }
    }
}
