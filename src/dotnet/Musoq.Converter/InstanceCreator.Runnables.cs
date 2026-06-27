using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Musoq.Converter.Build;
using Musoq.Evaluator;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Converter;

public static partial class InstanceCreator
{
    private static ITableRunnable CreateRunnable(BuildItems items)
    {
        var type = LoadRunnableType(items);

        return CreateRunnable(
            type,
            items.SchemaProvider,
            items.SourceRuntimeSettingsBySourceContextId,
            items.SourceRuntimeSettingDescriptionsBySourceContextId,
            CreateSourceExecutionPlans(items));
    }

    private static ITableRunnable CreateRunnable(BuildItems items, Func<Assembly> createAssembly)
    {
        var type = LoadRunnableType(items, createAssembly);

        return CreateRunnable(
            type,
            items.SchemaProvider,
            items.SourceRuntimeSettingsBySourceContextId,
            items.SourceRuntimeSettingDescriptionsBySourceContextId,
            CreateSourceExecutionPlans(items));
    }

    private static ITableRunnable CreateRunnable(CachedExecutionCompilation cachedCompilation, BuildItems items)
    {
        return CreateRunnable(
            cachedCompilation.RunnableType,
            items.SchemaProvider,
            items.SourceRuntimeSettingsBySourceContextId,
            items.SourceRuntimeSettingDescriptionsBySourceContextId,
            CreateSourceExecutionPlans(items));
    }

    private static ITypedRunnable<TOut> CreateTypedRunnable<TOut>(BuildItems items)
    {
        var type = LoadRunnableType(items);

        return CreateTypedRunnable<TOut>(
            type,
            items.SchemaProvider,
            items.SourceRuntimeSettingsBySourceContextId,
            items.SourceRuntimeSettingDescriptionsBySourceContextId,
            CreateSourceExecutionPlans(items));
    }

    private static Type LoadRunnableType(BuildItems items)
    {
        return LoadRunnableType(items, () =>
        {
            var dllFile = items.DllFile ??
                          throw new InvalidOperationException(CreateMissingRunnableDllMessage(items));

            return items.PdbFile is { Length: > 0 } pdbFile
                ? Assembly.Load(dllFile, pdbFile)
                : Assembly.Load(dllFile);
        });
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
        var assembly = createAssembly();

        var type = assembly.GetType(items.AccessToClassPath);

        if (type is null)
            throw new InvalidOperationException(
                $"Type {items.AccessToClassPath} was not found in assembly {assembly.FullName}.");

        return type;
    }

    private static ITableRunnable CreateRunnable(
        Type type,
        ISchemaProvider schemaProvider,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId,
        IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> sourceRuntimeSettingDescriptionsBySourceContextId,
        IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans)
    {
        var runnable = Activator.CreateInstance(type) as ITableRunnable;

        if (runnable is null)
            throw new InvalidOperationException($"Could not create instance of type {type.FullName}.");

        runnable.Provider = schemaProvider;
        runnable.SourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId;
        runnable.SourceRuntimeSettingDescriptionsBySourceContextId = sourceRuntimeSettingDescriptionsBySourceContextId;
        runnable.SourceExecutionPlans = sourceExecutionPlans;

        return runnable;
    }

    private static ITypedRunnable<TOut> CreateTypedRunnable<TOut>(
        Type type,
        ISchemaProvider schemaProvider,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> sourceRuntimeSettingsBySourceContextId,
        IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> sourceRuntimeSettingDescriptionsBySourceContextId,
        IReadOnlyDictionary<string, SourceExecutionPlan> sourceExecutionPlans)
    {
        var runnable = Activator.CreateInstance(type) as ITypedRunnable<TOut>;

        if (runnable is null)
            throw new InvalidOperationException($"Could not create typed instance of type {type.FullName}.");

        runnable.Provider = schemaProvider;
        runnable.SourceRuntimeSettingsBySourceContextId = sourceRuntimeSettingsBySourceContextId;
        runnable.SourceRuntimeSettingDescriptionsBySourceContextId = sourceRuntimeSettingDescriptionsBySourceContextId;
        runnable.SourceExecutionPlans = sourceExecutionPlans;

        return runnable;
    }

    private static Dictionary<string, SourceExecutionPlan> CreateSourceExecutionPlans(BuildItems items)
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
