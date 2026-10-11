using Musoq.Parser.Nodes.From;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator.Diagnostics;

internal static class DataSourceLifecycleDiagnosticAdapter
{
    public static DataSourceLifecycleException ForProviderOperation(
        SchemaProviderFailureException exception,
        SchemaFromNode source,
        string sourceContextId,
        string operation) =>
        DataSourceLifecycleException.ForProviderOperation(
            source.Schema,
            source.Method,
            source.Alias,
            sourceContextId,
            operation,
            exception);

    public static DataSourceLifecycleException ForProviderOperation(
        SchemaProviderFailureException exception,
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId,
        string operation) =>
        DataSourceLifecycleException.ForProviderOperation(
            schemaName,
            sourceName,
            alias,
            sourceContextId,
            operation,
            exception);
}
