using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Targets.Execution;

internal sealed record ExecutionTargetReadinessProfile(
    ExecutionTargetRuntimeFamily RuntimeFamily,
    IReadOnlySet<ExecutionTargetReadinessCategory> SupportedCategories,
    IReadOnlySet<ExecutionPortableSymbolPortability> SupportedTypeSymbolPortabilities,
    IReadOnlySet<ExecutionPortableSymbolPortability> SupportedCallableSymbolPortabilities)
{
    private static readonly ExecutionPortableSymbolPortability[] FutureTargetSymbolPortabilities =
    [
        ExecutionPortableSymbolPortability.Portable,
        ExecutionPortableSymbolPortability.HostImport
    ];

    private static readonly ExecutionPortableSymbolPortability[] CSharpClrSymbolPortabilities =
    [
        ExecutionPortableSymbolPortability.Portable,
        ExecutionPortableSymbolPortability.HostImport,
        ExecutionPortableSymbolPortability.ClrOnly
    ];

    public static ExecutionTargetReadinessProfile CSharpClr { get; } = Create(
        ExecutionTargetRuntimeFamily.CSharpClr,
        [
            ExecutionTargetReadinessCategory.ClrOnlyTypeUsage,
            ExecutionTargetReadinessCategory.ReflectionMethodInfo,
            ExecutionTargetReadinessCategory.SchemaProviderBinding,
            ExecutionTargetReadinessCategory.GeneratedRowShape,
            ExecutionTargetReadinessCategory.PluginInvocation,
            ExecutionTargetReadinessCategory.HostSourceAccess,
            ExecutionTargetReadinessCategory.NullTypeCoercion,
            ExecutionTargetReadinessCategory.ProfilingDiagnostics,
            ExecutionTargetReadinessCategory.Cancellation
        ],
        CSharpClrSymbolPortabilities,
        CSharpClrSymbolPortabilities);

    public static ExecutionTargetReadinessProfile BrowserLikeSource { get; } = Create(
        ExecutionTargetRuntimeFamily.BrowserSource,
        ExecutionTargetReadinessCategory.HostSourceAccess,
        ExecutionTargetReadinessCategory.NullTypeCoercion,
        ExecutionTargetReadinessCategory.ProfilingDiagnostics);

    public static ExecutionTargetReadinessProfile BytecodeVmLike { get; } = Create(
        ExecutionTargetRuntimeFamily.BytecodeVm,
        ExecutionTargetReadinessCategory.GeneratedRowShape,
        ExecutionTargetReadinessCategory.NullTypeCoercion,
        ExecutionTargetReadinessCategory.ProfilingDiagnostics);

    public static ExecutionTargetReadinessProfile InterpreterLike { get; } = Create(
        ExecutionTargetRuntimeFamily.Interpreter,
        ExecutionTargetReadinessCategory.HostSourceAccess,
        ExecutionTargetReadinessCategory.NullTypeCoercion,
        ExecutionTargetReadinessCategory.ProfilingDiagnostics);

    public static IReadOnlyList<ExecutionTargetReadinessProfile> FutureTargetProfiles { get; } =
    [
        BrowserLikeSource,
        BytecodeVmLike,
        InterpreterLike
    ];

    public static ExecutionTargetReadinessProfile Create(
        ExecutionTargetRuntimeFamily runtimeFamily,
        params ExecutionTargetReadinessCategory[] supportedCategories)
    {
        return Create(runtimeFamily, (IEnumerable<ExecutionTargetReadinessCategory>)supportedCategories);
    }

    public static ExecutionTargetReadinessProfile Create(
        ExecutionTargetRuntimeFamily runtimeFamily,
        IEnumerable<ExecutionTargetReadinessCategory> supportedCategories)
    {
        return Create(
            runtimeFamily,
            supportedCategories,
            FutureTargetSymbolPortabilities,
            FutureTargetSymbolPortabilities);
    }

    public static ExecutionTargetReadinessProfile Create(
        ExecutionTargetRuntimeFamily runtimeFamily,
        IEnumerable<ExecutionTargetReadinessCategory> supportedCategories,
        IEnumerable<ExecutionPortableSymbolPortability> supportedTypeSymbolPortabilities,
        IEnumerable<ExecutionPortableSymbolPortability> supportedCallableSymbolPortabilities)
    {
        ArgumentNullException.ThrowIfNull(supportedCategories);
        ArgumentNullException.ThrowIfNull(supportedTypeSymbolPortabilities);
        ArgumentNullException.ThrowIfNull(supportedCallableSymbolPortabilities);

        return new ExecutionTargetReadinessProfile(
            runtimeFamily,
            supportedCategories.ToFrozenSet(),
            supportedTypeSymbolPortabilities.ToFrozenSet(),
            supportedCallableSymbolPortabilities.ToFrozenSet());
    }

    public bool Supports(ExecutionTargetReadinessCategory category)
    {
        return SupportedCategories.Contains(category);
    }

    public bool SupportsTypeSymbolPortability(ExecutionPortableSymbolPortability portability)
    {
        return SupportedTypeSymbolPortabilities.Contains(portability);
    }

    public bool SupportsCallableSymbolPortability(ExecutionPortableSymbolPortability portability)
    {
        return SupportedCallableSymbolPortabilities.Contains(portability);
    }

}
