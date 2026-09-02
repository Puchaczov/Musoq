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

        if (ContainsSchemaProviderFailure(exception))
            return false;

        return exception is IDiagnosticException ||
               exception is ParseException ||
               exception is Schema.Interpreters.ParseException ||
               exception is SchemaArgumentException ||
               exception is TableNotFoundException ||
               exception is SourceNotFoundException;
    }

    public static bool IsExpectedSchemaLookupFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is SourceNotFoundException;
    }

    public static SchemaProviderFailureException? FindSchemaProviderFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var visited = new HashSet<Exception>();
        for (var current = exception; current != null && visited.Add(current); current = current.InnerException)
            if (current is SchemaProviderFailureException providerFailure)
                return providerFailure;

        return null;
    }

    private static bool ContainsSchemaProviderFailure(Exception exception)
    {
        return FindSchemaProviderFailure(exception) != null;
    }
}
