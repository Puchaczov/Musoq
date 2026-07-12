namespace Musoq.Targets.Execution;

internal enum ExecutionTargetRequirementKind
{
    ClrTypeUsage,
    MethodInfoCall,
    SchemaProviderBinding,
    GeneratedClrRow,
    PluginInvocation,
    HostSourceAccess,
    NullTypeCoercion,
    ProfilingDiagnostics,
    Cancellation,
    ClrOnlyConstant
}
