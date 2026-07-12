using System;

namespace Musoq.Targets.Abstractions;

internal sealed record TargetDiagnostic
{
    public TargetDiagnostic(
        string code,
        TargetDiagnosticSeverity severity,
        string message,
        TargetSourceRange? sourceRange = null,
        string? sourceName = null,
        string? sourceSnippet = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Severity = severity;
        Message = message;
        SourceRange = sourceRange;
        SourceName = sourceName;
        SourceSnippet = sourceSnippet;
    }

    public string Code { get; }

    public TargetDiagnosticSeverity Severity { get; }

    public string Message { get; }

    public TargetSourceRange? SourceRange { get; }

    public string? SourceName { get; }

    public string? SourceSnippet { get; }

    public static TargetDiagnostic Error(string code, string message) =>
        new(code, TargetDiagnosticSeverity.Error, message);
}
