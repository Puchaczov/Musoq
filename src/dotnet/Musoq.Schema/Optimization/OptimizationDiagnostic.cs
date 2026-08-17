namespace Musoq.Schema.Optimization;

public sealed record OptimizationDiagnostic(
    OptimizationDiagnosticSeverity Severity,
    string Message)
{
    public string? Origin { get; init; }

    public string? Optimization { get; init; }

    public string? Target { get; init; }

    public string? Reason { get; init; }

    [Obsolete("Runtime-v2 planning should report source-contract or concrete planning diagnostics instead of optimization fallback warnings.")]
    public string? Fallback { get; init; }

    public static OptimizationDiagnostic Info(string message)
    {
        return new OptimizationDiagnostic(OptimizationDiagnosticSeverity.Info, message);
    }

    public static OptimizationDiagnostic Warning(string message)
    {
        return new OptimizationDiagnostic(OptimizationDiagnosticSeverity.Warning, message);
    }

    [Obsolete("Runtime-v2 planning should report source-contract or concrete planning diagnostics instead of optimization fallback warnings.")]
    public static OptimizationDiagnostic FallbackWarning(
        string optimization,
        string target,
        string reason,
        string fallback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(optimization);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);

        return new OptimizationDiagnostic(OptimizationDiagnosticSeverity.Warning, reason)
        {
            Optimization = optimization,
            Target = target,
            Reason = reason,
            Fallback = fallback
        };
    }
}
