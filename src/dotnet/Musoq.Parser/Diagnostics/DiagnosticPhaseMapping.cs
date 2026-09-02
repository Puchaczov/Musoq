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
            (int)DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape => DiagnosticPhase.Parse,
            >= 4001 and <= 4016 => DiagnosticPhase.Schema,
            (int)DiagnosticCode.MQ8001_CodeGenerationFailed => DiagnosticPhase.CodeGeneration,
            (int)DiagnosticCode.MQ7010_DataSourceOpenFailed or
            (int)DiagnosticCode.MQ7011_DataSourceReadFailed or
            (int)DiagnosticCode.MQ7012_DataSourceCleanupFailed => DiagnosticPhase.DataSource,
            >= 1000 and < 3000 => DiagnosticPhase.Parse,
            >= 3000 and < 4000 => DiagnosticPhase.Bind,
            >= 4000 and < 5000 => DiagnosticPhase.DataSource,
            >= 5000 and < 6000 => DiagnosticPhase.Bind,
            >= 6000 and < 7000 => DiagnosticPhase.FeatureGate,
            >= 7000 and < 8000 => DiagnosticPhase.Runtime,
            (int)DiagnosticCode.MQ9001_InternalCompilerError or (int)DiagnosticCode.MQ9002_InternalExecutionError => DiagnosticPhase.Internal,
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
            DiagnosticPhase.CodeGeneration => "code-generation",
            DiagnosticPhase.Schema => "schema",
            DiagnosticPhase.Internal => "internal",
            _ => "unknown"
        };
    }
}
