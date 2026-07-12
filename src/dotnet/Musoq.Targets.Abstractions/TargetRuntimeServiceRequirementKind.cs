namespace Musoq.Targets.Abstractions;

internal enum TargetRuntimeServiceRequirementKind
{
    SourceAccess = 0,
    PluginInvocation = 1,
    RowTableShape = 2,
    NullSemantics = 3,
    Cancellation = 4,
    Diagnostics = 5,
    Profiling = 6
}
