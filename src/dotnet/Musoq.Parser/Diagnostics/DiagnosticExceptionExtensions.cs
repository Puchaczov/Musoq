using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
    ///     Converts an exception to a diagnostic, falling back to a generic error if
    ///     the exception doesn't implement IDiagnosticException.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <param name="sourceText">Optional source text for line/column information.</param>
    /// <returns>A diagnostic representing the exception.</returns>
    public static Diagnostic ToDiagnosticOrGeneric(this Exception exception, SourceText? sourceText = null)
    {
        if (TryGetDiagnosticException(exception, out var diagnosticException))
            return diagnosticException.ToDiagnostic(sourceText);


        var message = MakeUserFriendlyMessage(exception);
        var code = GetFallbackDiagnosticCode(exception);

        return Diagnostic.Error(
            code,
            message,
            TextSpan.Empty);
    }

    private static DiagnosticCode GetFallbackDiagnosticCode(Exception exception)
    {
        if (exception.Message.Contains("Unterminated string literal", StringComparison.OrdinalIgnoreCase))
            return DiagnosticCode.MQ1002_UnterminatedString;

        if (exception is NullReferenceException or ArgumentNullException or IndexOutOfRangeException)
            return DiagnosticCode.MQ2030_UnsupportedSyntax;

        if (exception is NotSupportedException)
            return DiagnosticCode.MQ2030_UnsupportedSyntax;

        if (exception is KeyNotFoundException)
            return DiagnosticCode.MQ3003_UnknownTable;

        if (exception is ArgumentException argumentException &&
            argumentException.Message.Contains("same key has already been added", StringComparison.OrdinalIgnoreCase))
            return DiagnosticCode.MQ3021_DuplicateAlias;

        if (exception is ArgumentException methodArgumentException &&
            string.Equals(methodArgumentException.ParamName, "methodName", StringComparison.Ordinal) &&
            methodArgumentException.Message.Contains("is not recognized", StringComparison.OrdinalIgnoreCase))
            return DiagnosticCode.MQ3003_UnknownTable;

        if (exception.Message.Contains("schemaName=", StringComparison.OrdinalIgnoreCase) &&
            exception.Message.Contains("registry=null", StringComparison.OrdinalIgnoreCase))
            return DiagnosticCode.MQ3010_UnknownSchema;

        if (exception is InvalidOperationException invalidOperationException &&
            invalidOperationException.Message.Contains("Stack empty", StringComparison.OrdinalIgnoreCase))
            return DiagnosticCode.MQ2030_UnsupportedSyntax;

        if (exception is InvalidOperationException)
            return DiagnosticCode.MQ2030_UnsupportedSyntax;

        return DiagnosticCode.MQ9999_Unknown;
    }

    private static bool TryGetDiagnosticException(Exception exception, out IDiagnosticException diagnosticException)
    {
        if (exception is null)
            throw new ArgumentNullException(nameof(exception));

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
