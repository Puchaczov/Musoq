namespace Musoq.Targets.Execution;

internal enum ExecutionTargetReadinessCategory
{
    ClrOnlyTypeUsage = 0,
    ReflectionMethodInfo = 1,
    SchemaProviderBinding = 3,
    GeneratedRowShape = 4,
    PluginInvocation = 5,
    HostSourceAccess = 6,
    NullTypeCoercion = 7,
    ProfilingDiagnostics = 8,
    Cancellation = 9
}
