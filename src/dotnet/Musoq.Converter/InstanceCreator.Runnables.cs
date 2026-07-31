using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Schema.Optimization;
using Musoq.Targets.CSharpClr;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static ITableRunnable CreateRunnable(BuildItems items)
    {
        var executable = GetExecutableArtifact(
            items.ExecutableArtifact,
            items.DllFile,
            items.PdbFile,
            () => items.AccessToClassPath,
            () => CreateMissingRunnableDllMessage(items));
        var activator = ExecutionTargetCatalog.ResolveActivator(executable.TargetId);
        return activator.ActivateTable(
            executable,
            CreateRuntimeBinding(items));
    }

    private static ITableRunnable CreateRunnable(BuildItems items, Func<Assembly> createAssembly)
    {
        var executable = GetExecutableArtifact(
            items.ExecutableArtifact,
            items.DllFile,
            items.PdbFile,
            () => items.AccessToClassPath,
            () => CreateMissingRunnableDllMessage(items));
        var clrActivator = RequireClrActivator(executable.TargetId);
        return clrActivator.ActivateTable(
            executable,
            CreateRuntimeBinding(items),
            createAssembly);
    }

    private static ITableRunnable CreateRunnable(CachedExecutionCompilation cachedCompilation, BuildItems items)
    {
        var executable = cachedCompilation.Template.ExecutableArtifact;
        var activator = ExecutionTargetCatalog.ResolveActivator(executable.TargetId);
        return activator.ActivateTable(
            executable,
            CreateRuntimeBinding(items));
    }

    private static ITypedRunnable<TOut> CreateTypedRunnable<TOut>(BuildItems items)
    {
        var executable = GetExecutableArtifact(
            items.ExecutableArtifact,
            items.DllFile,
            items.PdbFile,
            () => items.AccessToClassPath,
            () => CreateMissingRunnableDllMessage(items));
        var activator = ExecutionTargetCatalog.ResolveActivator(executable.TargetId);
        return activator.ActivateTyped<TOut>(
            executable,
            CreateRuntimeBinding(items));
    }

    private static Type LoadRunnableType(BuildItems items)
    {
        var executable = GetExecutableArtifact(
            items.ExecutableArtifact,
            items.DllFile,
            items.PdbFile,
            () => items.AccessToClassPath,
            () => CreateMissingRunnableDllMessage(items));
        return RequireClrActivator(executable.TargetId).LoadRunnableType(executable);
    }

    private static string CreateMissingRunnableDllMessage(BuildItems items)
    {
        var diagnostics = items.DiagnosticContext.Diagnostics.ToArray();
        if (diagnostics.Length == 0)
            return "Cannot load runnable because the DLL file is missing.";

        var builder = new StringBuilder("Cannot load runnable because the DLL file is missing. Build diagnostics:");

        foreach (var diagnostic in diagnostics)
        {
            builder.AppendLine();
            builder.Append(diagnostic.ToDetailedString());
        }

        return builder.ToString();
    }

    private static Type LoadRunnableType(BuildItems items, Func<Assembly> createAssembly)
    {
        var executable = GetExecutableArtifact(
            items.ExecutableArtifact,
            items.DllFile,
            items.PdbFile,
            () => items.AccessToClassPath,
            () => CreateMissingRunnableDllMessage(items));
        return RequireClrActivator(executable.TargetId).LoadRunnableType(executable, createAssembly);
    }

    private static ITableRunnable CreateRunnable(Type runnableType, QueryRuntimeBinding binding)
    {
        return RequireClrActivator(ExecutionTargetIds.CSharpClr).ActivateTable(runnableType, binding);
    }

    private static QueryRuntimeBinding CreateRuntimeBinding(BuildItems items)
    {
        return new QueryRuntimeBinding(
            items.SchemaProvider,
            items.SourceRuntimeSettingsBySourceContextId,
            items.SourceRuntimeSettingDescriptionsBySourceContextId,
            CreateSourceExecutionPlans(items));
    }

    private static ExecutableQueryArtifact GetExecutableArtifact(
        ExecutableQueryArtifact? executableArtifact,
        byte[]? dllFile,
        byte[]? pdbFile,
        Func<string> accessToClassPath,
        Func<string> missingRunnableDllMessage)
    {
        if (executableArtifact is { } executable)
            return executable;

        if (dllFile is { Length: > 0 } runnableDllFile)
            return CSharpClrArtifactCompatibility.CreateAssemblyExecutable(
                runnableDllFile,
                pdbFile,
                accessToClassPath());

        throw new InvalidOperationException(missingRunnableDllMessage());
    }

    private static ClrAssemblyExecutableActivator RequireClrActivator(ExecutionTargetId TargetId)
    {
        return ExecutionTargetCatalog.ResolveActivator(TargetId) as ClrAssemblyExecutableActivator ??
               throw new InvalidOperationException(
                   $"Execution target '{TargetId}' does not expose a CLR assembly activator.");
    }

    private static IReadOnlyDictionary<string, SourceExecutionPlan> CreateSourceExecutionPlans(BuildItems items)
    {
        var requests = items.SourcePlanRequestsPerSchema;
        var plannedSources = items.PlanningResult?.Properties.SourcePlanResultsBySourceId;
        var result = new Dictionary<string, SourceExecutionPlan>(requests.Count, StringComparer.Ordinal);
        foreach (var requestEntry in requests)
        {
            result[requestEntry.Key.Id] =
                plannedSources != null &&
                plannedSources.TryGetValue(requestEntry.Key.Id, out var plannedSource)
                    ? plannedSource.ExecutionPlan
                    : SourceExecutionPlan.Empty(requestEntry.Value.Identity);
        }

        return result;
    }
}
