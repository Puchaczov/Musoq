namespace Musoq.Targets.Execution;

internal enum ExecutionTargetRequirementKind
{
    ClrTypeUsage,
    MethodInfoCall,
    SchemaProviderBinding,
    GeneratedClrRow,
    PluginInvocation,
    HostSourceAccess,
    QueryRowSourceAccess,
    NullTypeCoercion,
    ProfilingDiagnostics,
    Cancellation,
    ClrOnlyConstant
}
