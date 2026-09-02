namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Supplies the default source domain for diagnostics whose originating
///     boundary is encoded by their stable code family.
/// </summary>
internal static class DiagnosticSourceKindMapping
{
    public static DiagnosticSourceKind FromCode(DiagnosticCode code)
    {
        var value = (int)code;

        return value switch
        {
            >= 4001 and <= 4016 => DiagnosticSourceKind.Schema,
            >= 7003 and <= 7009 => DiagnosticSourceKind.Runtime,
            >= 7010 and <= 7012 => DiagnosticSourceKind.DataSource,
            (int)DiagnosticCode.MQ8001_CodeGenerationFailed or
            (int)DiagnosticCode.MQ8002_CompiledArtifactIncompatible => DiagnosticSourceKind.GeneratedSource,
            (int)DiagnosticCode.MQ9001_InternalCompilerError or
            (int)DiagnosticCode.MQ9002_InternalExecutionError => DiagnosticSourceKind.Internal,
            _ => DiagnosticSourceKind.Query
        };
    }
}
