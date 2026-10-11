using System.Collections.Generic;
using System.IO;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Diagnostics;

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
        : this(
            code,
            new DataSourceLifecycleContext(schemaName, sourceName, alias, sourceContextId, operation),
            innerException,
            null)
    {
    }

    private DataSourceLifecycleException(
        DiagnosticCode code,
        DataSourceLifecycleContext context,
        Exception innerException,
        MalformedInputFailure? inputFailure)
        : base(CreateSafeMessage(code, context, inputFailure?.Reason), innerException)
    {
        if (code is not DiagnosticCode.MQ7010_DataSourceOpenFailed and
            not DiagnosticCode.MQ7011_DataSourceReadFailed and
            not DiagnosticCode.MQ7012_DataSourceCleanupFailed and
            not DiagnosticCode.MQ7014_MalformedRuntimeInput)
            throw new ArgumentOutOfRangeException(nameof(code), code, "The code is not a data-source lifecycle diagnostic.");

        SchemaName = context.SchemaName ?? string.Empty;
        SourceName = context.SourceName ?? string.Empty;
        Alias = context.Alias ?? string.Empty;
        SourceContextId = context.SourceContextId ?? string.Empty;
        Operation = context.Operation ?? string.Empty;
        Code = code;
        InputReason = inputFailure?.Reason;
        InputLocation = inputFailure?.InputLocation;
    }

    public DiagnosticCode Code { get; }

    public string SchemaName { get; }

    public string SourceName { get; }

    public string Alias { get; }

    public string SourceContextId { get; }

    public string Operation { get; }

    public RuntimeInputFailureReason? InputReason { get; }

    public DiagnosticInputLocation? InputLocation { get; }

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

        if (InputReason.HasValue)
        {
            arguments.Add(new KeyValuePair<string, string>(
                "inputReason",
                RuntimeInputDiagnosticException.GetReasonCode(InputReason.Value)));
        }

        return new Diagnostic(
            Code,
            DiagnosticSeverity.Error,
            Message,
            SourceLocation.None,
            SourceLocation.None,
            phase: Code == DiagnosticCode.MQ7014_MalformedRuntimeInput
                ? DiagnosticPhase.Runtime
                : DiagnosticPhase.DataSource,
            sourceKind: DiagnosticSourceKind.DataSource,
            arguments: arguments).WithInputLocation(InputLocation);
    }

    public static DataSourceLifecycleException ForProviderOperation(
        string schemaName,
        string sourceName,
        string alias,
        string sourceContextId,
        string operation,
        Exception innerException)
    {
        if (FindInnerException<DataSourceInputException>(innerException) is { } inputFailure)
        {
            return CreateMalformedInputFailure(
                new DataSourceLifecycleContext(schemaName, sourceName, alias, sourceContextId, operation),
                innerException,
                inputFailure.ToParserDiagnosticException());
        }

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
        if (FindInnerException<DataSourceInputException>(innerException) is { } inputFailure)
        {
            return CreateMalformedInputFailure(
                new DataSourceLifecycleContext(schemaName, sourceName, alias, sourceContextId, "open"),
                innerException,
                inputFailure.ToParserDiagnosticException());
        }

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
        if (FindInnerException<DataSourceInputException>(innerException) is { } dataSourceInputFailure)
        {
            return CreateMalformedInputFailure(
                new DataSourceLifecycleContext(schemaName, sourceName, alias, sourceContextId, "read"),
                innerException,
                dataSourceInputFailure.ToParserDiagnosticException());
        }

        var inputFailure = FindInnerException<RuntimeInputDiagnosticException>(innerException);
        if (inputFailure != null)
        {
            return CreateMalformedInputFailure(
                new DataSourceLifecycleContext(schemaName, sourceName, alias, sourceContextId, "read"),
                innerException,
                new MalformedInputFailure(inputFailure.Reason, inputFailure.InputLocation));
        }

        if (FindInnerException<InvalidDataException>(innerException) != null)
        {
            return CreateMalformedInputFailure(
                new DataSourceLifecycleContext(schemaName, sourceName, alias, sourceContextId, "read"),
                innerException,
                new MalformedInputFailure(RuntimeInputFailureReason.MalformedInput, null));
        }

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
        DataSourceLifecycleContext context,
        RuntimeInputFailureReason? inputReason)
    {
        return code switch
        {
            DiagnosticCode.MQ7014_MalformedRuntimeInput =>
                RuntimeInputDiagnosticException.GetSafeMessage(inputReason ?? RuntimeInputFailureReason.MalformedInput),
            DiagnosticCode.MQ7010_DataSourceOpenFailed when string.Equals(context.Operation, "open", StringComparison.Ordinal) =>
                ErrorCatalog.GetMessage(code, context.SchemaName, context.SourceName, context.Alias),
            DiagnosticCode.MQ7011_DataSourceReadFailed when string.Equals(context.Operation, "read", StringComparison.Ordinal) =>
                ErrorCatalog.GetMessage(code, context.SchemaName, context.SourceName, context.Alias),
            DiagnosticCode.MQ7012_DataSourceCleanupFailed when string.Equals(context.Operation, "cleanup", StringComparison.Ordinal) =>
                ErrorCatalog.GetMessage(code, context.SchemaName, context.SourceName, context.Alias),
            DiagnosticCode.MQ7010_DataSourceOpenFailed =>
                $"The data source provider failed during '{context.Operation}' for schema '{context.SchemaName}', source '{context.SourceName}', alias '{context.Alias}'.",
            _ => "The data source failed during query execution."
        };
    }

    private static DataSourceLifecycleException CreateMalformedInputFailure(
        DataSourceLifecycleContext context,
        Exception innerException,
        RuntimeInputDiagnosticException inputFailure)
    {
        return CreateMalformedInputFailure(
            context,
            innerException,
            new MalformedInputFailure(inputFailure.Reason, inputFailure.InputLocation));
    }

    private static DataSourceLifecycleException CreateMalformedInputFailure(
        DataSourceLifecycleContext context,
        Exception innerException,
        MalformedInputFailure inputFailure)
    {
        return new DataSourceLifecycleException(
            DiagnosticCode.MQ7014_MalformedRuntimeInput,
            context,
            innerException,
            inputFailure);
    }

    private static TException? FindInnerException<TException>(Exception exception)
        where TException : Exception
    {
        var current = exception;
        while (current != null)
        {
            if (current is TException match)
                return match;

            current = current.InnerException;
        }

        return null;
    }

    private sealed record DataSourceLifecycleContext(
        string SchemaName,
        string SourceName,
        string Alias,
        string SourceContextId,
        string Operation);

    private sealed record MalformedInputFailure(
        RuntimeInputFailureReason Reason,
        DiagnosticInputLocation? InputLocation);
}
