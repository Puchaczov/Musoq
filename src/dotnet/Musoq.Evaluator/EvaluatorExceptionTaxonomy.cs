using System;
using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator;

internal static class EvaluatorExceptionTaxonomy
{
    public static bool IsExpectedQueryFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException or SchemaProviderFailureException)
            return false;

        return exception is IDiagnosticException ||
               exception is ParseException ||
               exception is SchemaArgumentException ||
               exception is TableNotFoundException ||
               exception is KeyNotFoundException ||
               exception is NotSupportedException ||
               IsKnownDuplicateKeyFailure(exception) ||
               IsExpectedSchemaLookupFailure(exception) ||
               IsKnownQueryInvalidOperation(exception);
    }

    public static bool IsExpectedSchemaLookupFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is SourceNotFoundException ||
            string.Equals(exception.GetType().Name, "SchemaNotFoundException", StringComparison.Ordinal))
            return true;

        if (exception is not InvalidOperationException)
            return false;

        var message = exception.Message;
        return message.Contains("schema", StringComparison.OrdinalIgnoreCase) &&
               (message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("not declared", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("not expose", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsKnownDuplicateKeyFailure(Exception exception)
    {
        return exception is ArgumentException argumentException &&
               argumentException.Message.Contains(
                   "same key has already been added",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKnownQueryInvalidOperation(Exception exception)
    {
        if (exception is not InvalidOperationException)
            return false;

        var message = exception.Message;
        return message.Contains("Cannot create interpret table", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("no longer supported", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("does not have a resolved factory method", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("Specified method is not supported", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("Stack empty", StringComparison.OrdinalIgnoreCase);
    }
}
