using System.Collections.Generic;

namespace Musoq.Schema.Diagnostics;

/// <summary>Optional provider report content. The diagnostic snapshots and sanitizes these values.</summary>
public sealed record DataSourceDiagnosticOptions
{
    public DataSourceDiagnosticSeverity Severity { get; init; } = DataSourceDiagnosticSeverity.Error;
    public IReadOnlyList<string> SuggestedFixes { get; init; } = [];
    public DataSourceFailureKind? FailureKind { get; init; }
    public IReadOnlyDictionary<string, string> Details { get; init; } = new Dictionary<string, string>();
    public DataSourceInputLocation? InputLocation { get; init; }
    public string? ArgumentName { get; init; }
    public int? ArgumentIndex { get; init; }
    public bool CanRetry { get; init; }
    public TimeSpan? RetryAfter { get; init; }
}
