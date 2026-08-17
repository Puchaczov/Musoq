using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Schema.Exceptions;

/// <summary>
/// Identifies a failure at a data-source lifecycle boundary without exposing
/// provider-specific exception text as a query diagnostic.
/// </summary>
public sealed class DataSourceLifecycleException : Exception, IDiagnosticException
{
    public DataSourceLifecycleException(
        DiagnosticCode code,
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId,
        string operation,
        Exception innerException)
        : base(CreateSafeMessage(code, schemaName, sourceName, alias), innerException)
    {
        if (code is not DiagnosticCode.MQ7010_DataSourceOpenFailed and
            not DiagnosticCode.MQ7011_DataSourceReadFailed and
            not DiagnosticCode.MQ7012_DataSourceCleanupFailed)
            throw new ArgumentOutOfRangeException(nameof(code), code, "The code is not a data-source lifecycle diagnostic.");

        SchemaName = schemaName ?? string.Empty;
        SourceName = sourceName ?? string.Empty;
        Alias = alias ?? string.Empty;
        SourceContextId = sourceContextId ?? string.Empty;
        Operation = operation ?? string.Empty;
        Code = code;
    }

    public DiagnosticCode Code { get; }

    public string SchemaName { get; }

    public string SourceName { get; }

    public string Alias { get; }

    public string SourceContextId { get; }

    public string Operation { get; }

    public TextSpan? Span => null;

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        return new Diagnostic(
            Code,
            DiagnosticSeverity.Error,
            Message,
            SourceLocation.None,
            SourceLocation.None,
            phase: DiagnosticPhase.DataSource,
            sourceKind: DiagnosticSourceKind.DataSource,
            arguments:
            [
                new KeyValuePair<string, string>("schema", SchemaName),
                new KeyValuePair<string, string>("source", SourceName),
                new KeyValuePair<string, string>("alias", Alias),
                new KeyValuePair<string, string>("sourceContextId", SourceContextId),
                new KeyValuePair<string, string>("operation", Operation)
            ]);
    }

    public static DataSourceLifecycleException ForOpen(
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId,
        Exception innerException)
    {
        return new DataSourceLifecycleException(
            DiagnosticCode.MQ7010_DataSourceOpenFailed,
            schemaName,
            sourceName,
            alias,
            sourceContextId,
            "open",
            innerException);
    }

    public static DataSourceLifecycleException ForRead(
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId,
        Exception innerException)
    {
        return new DataSourceLifecycleException(
            DiagnosticCode.MQ7011_DataSourceReadFailed,
            schemaName,
            sourceName,
            alias,
            sourceContextId,
            "read",
            innerException);
    }

    public static DataSourceLifecycleException ForCleanup(
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId,
        Exception innerException)
    {
        return new DataSourceLifecycleException(
            DiagnosticCode.MQ7012_DataSourceCleanupFailed,
            schemaName,
            sourceName,
            alias,
            sourceContextId,
            "cleanup",
            innerException);
    }

    private static string CreateSafeMessage(
        DiagnosticCode code,
        string schemaName,
        string sourceName,
        string alias)
    {
        return code switch
        {
            DiagnosticCode.MQ7010_DataSourceOpenFailed =>
                ErrorCatalog.GetMessage(code, schemaName, sourceName, alias),
            DiagnosticCode.MQ7011_DataSourceReadFailed =>
                ErrorCatalog.GetMessage(code, schemaName, sourceName, alias),
            DiagnosticCode.MQ7012_DataSourceCleanupFailed =>
                ErrorCatalog.GetMessage(code, schemaName, sourceName, alias),
            _ => "The data source failed during query execution."
        };
    }
}
