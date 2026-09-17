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
    private Exception? _relatedException;

    public DataSourceLifecycleException(
        DiagnosticCode code,
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId,
        string operation,
        Exception innerException)
        : base(CreateSafeMessage(code, schemaName, sourceName, alias, operation), innerException)
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

    internal void AttachRelatedFailure(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _relatedException ??= exception;
    }

    public TextSpan? Span => null;

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var arguments = new List<KeyValuePair<string, string>>
        {
            new("schema", SchemaName),
            new("source", SourceName),
            new("alias", Alias),
            new("sourceContextId", SourceContextId),
            new("operation", Operation),
            new(
                "causeType",
                InnerException?.GetType().FullName ?? string.Empty)
        };

        if (_relatedException != null)
        {
            arguments.Add(new KeyValuePair<string, string>(
                "relatedCauseType",
                _relatedException.GetType().FullName ?? string.Empty));
        }

        return new Diagnostic(
            Code,
            DiagnosticSeverity.Error,
            Message,
            SourceLocation.None,
            SourceLocation.None,
            phase: DiagnosticPhase.DataSource,
            sourceKind: DiagnosticSourceKind.DataSource,
            arguments: arguments);
    }

    public static DataSourceLifecycleException ForProviderOperation(
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId,
        string operation,
        Exception innerException)
    {
        return new DataSourceLifecycleException(
            DiagnosticCode.MQ7010_DataSourceOpenFailed,
            schemaName,
            sourceName,
            alias,
            sourceContextId,
            operation,
            innerException);
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
        string alias,
        string operation)
    {
        return code switch
        {
            DiagnosticCode.MQ7010_DataSourceOpenFailed when string.Equals(operation, "open", StringComparison.Ordinal) =>
                ErrorCatalog.GetMessage(code, schemaName, sourceName, alias),
            DiagnosticCode.MQ7011_DataSourceReadFailed when string.Equals(operation, "read", StringComparison.Ordinal) =>
                ErrorCatalog.GetMessage(code, schemaName, sourceName, alias),
            DiagnosticCode.MQ7012_DataSourceCleanupFailed when string.Equals(operation, "cleanup", StringComparison.Ordinal) =>
                ErrorCatalog.GetMessage(code, schemaName, sourceName, alias),
            DiagnosticCode.MQ7010_DataSourceOpenFailed =>
                $"The data source provider failed during '{operation}' for schema '{schemaName}', source '{sourceName}', alias '{alias}'.",
            _ => "The data source failed during query execution."
        };
    }
}
