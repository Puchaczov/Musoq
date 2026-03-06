namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Maps diagnostic codes to their originating compilation phase.
/// </summary>
public static class DiagnosticPhaseMapping
{
    /// <summary>
    ///     Determines the compilation phase for a given diagnostic code.
    /// </summary>
    public static DiagnosticPhase FromCode(DiagnosticCode code)
    {
        var value = (int)code;

        return value switch
        {
            >= 1000 and < 3000 => DiagnosticPhase.Parse,
            >= 3000 and < 4000 => DiagnosticPhase.Bind,
            >= 4000 and < 5000 => DiagnosticPhase.DataSource,
            >= 5000 and < 6000 => DiagnosticPhase.Bind,
            >= 6000 and < 7000 => DiagnosticPhase.FeatureGate,
            >= 7000 and < 8000 => DiagnosticPhase.Runtime,
            >= 8000 and < 9000 => DiagnosticPhase.Runtime,
            _ => DiagnosticPhase.Runtime
        };
    }

    /// <summary>
    ///     Returns the phase name as a lowercase string for display.
    /// </summary>
    public static string ToDisplayString(DiagnosticPhase phase)
    {
        return phase switch
        {
            DiagnosticPhase.Parse => "parse",
            DiagnosticPhase.Bind => "bind",
            DiagnosticPhase.Runtime => "runtime",
            DiagnosticPhase.DataSource => "datasource",
            DiagnosticPhase.FeatureGate => "feature-gate",
            _ => "unknown"
        };
    }
}
