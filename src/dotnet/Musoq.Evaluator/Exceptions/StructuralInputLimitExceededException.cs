using System.Collections.Generic;
using System.Globalization;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Exceptions;

/// <summary>Reports a structural input that exceeded a configured resource bound.</summary>
public sealed class StructuralInputLimitExceededException : InvalidOperationException, IDiagnosticException
{
    public StructuralInputLimitExceededException(
        string origin,
        string path,
        StructuralInputLimitKind limitKind,
        long configuredLimit,
        long observedValue,
        string? sourceContextId = null)
        : base(CreateMessage(origin, path, limitKind, configuredLimit, observedValue))
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (configuredLimit <= 0)
            throw new ArgumentOutOfRangeException(nameof(configuredLimit));
        if (observedValue < 0)
            throw new ArgumentOutOfRangeException(nameof(observedValue));

        Origin = origin;
        Path = path;
        LimitKind = limitKind;
        ConfiguredLimit = configuredLimit;
        ObservedValue = observedValue;
        SourceContextId = sourceContextId;
    }

    public DiagnosticCode Code => DiagnosticCode.MQ7013_StructuralInputLimitExceeded;

    public TextSpan? Span => null;

    public string Origin { get; }

    public string Path { get; }

    public StructuralInputLimitKind LimitKind { get; }

    public long ConfiguredLimit { get; }

    public long ObservedValue { get; }

    public string? SourceContextId { get; }

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var arguments = new List<KeyValuePair<string, string>>
        {
            new("origin", Origin),
            new("path", Path),
            new("limitKind", LimitKind.ToString()),
            new("configuredLimit", ConfiguredLimit.ToString(CultureInfo.InvariantCulture)),
            new("observedValue", ObservedValue.ToString(CultureInfo.InvariantCulture))
        };
        if (SourceContextId != null)
            arguments.Add(new("sourceContextId", SourceContextId));

        return new Diagnostic(
            Code,
            DiagnosticSeverity.Error,
            Message,
            SourceLocation.None,
            SourceLocation.None,
            phase: DiagnosticPhase.Runtime,
            sourceKind: DiagnosticSourceKind.Runtime,
            arguments: arguments);
    }

    private static string CreateMessage(
        string origin,
        string path,
        StructuralInputLimitKind limitKind,
        long configuredLimit,
        long observedValue) =>
        $"Structural input from '{origin}' exceeded its {limitKind} limit of {configuredLimit.ToString(CultureInfo.InvariantCulture)} " +
        $"(observed {observedValue.ToString(CultureInfo.InvariantCulture)}) at '{path}'.";
}
