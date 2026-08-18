using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Planning;
using Musoq.Schema;
using Musoq.Targets.CSharpClr;

namespace Musoq.Converter.Tests;

[TestClass]
[DoNotParallelize]
public sealed class QueryScopedRowCacheTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void IdenticalSqlAndShape_ShouldReuseArtifactAndBindFreshProviderState()
    {
        var query = UniqueQuery("select r.Value from #queryrowcache.rows() r where r.Value >= {0}");
        var firstProvider = new QueryRowCacheSchemaProvider(NarrowState(11));
        var first = Compile(query, "QueryRowCacheFreshFirst", firstProvider);
        Assert.IsFalse(first.BuildItems!.StopAfterPlanning);

        var firstRuntime = GetGeneratedRuntime(first.CompiledQuery!);
        using (var table = first.CompiledQuery!.Run())
            Assert.AreEqual(11, table[0][0]);

        var secondProvider = new QueryRowCacheSchemaProvider(NarrowState(22));
        var second = Compile(query, "QueryRowCacheFreshSecond", secondProvider);
        Assert.IsTrue(second.BuildItems!.StopAfterPlanning, "The identical semantic shape did not hit the execution artifact cache.");
        var secondRuntime = GetGeneratedRuntime(second.CompiledQuery!);

        Assert.AreEqual(firstRuntime.Type.Module.ModuleVersionId, secondRuntime.Type.Module.ModuleVersionId);
        Assert.AreNotSame(firstRuntime.Context, secondRuntime.Context);
        using (var table = second.CompiledQuery!.Run())
            Assert.AreEqual(22, table[0][0]);

        first.CompiledQuery.Dispose();
        second.CompiledQuery.Dispose();
    }

    [TestMethod]
    public void MutatedMetadataForSameSqlAndProvider_ShouldReplaceCollidingExactCacheAlias()
    {
        var query = UniqueQuery("select * from #queryrowcache.rows() r where {0} <= 0");
        var state = NarrowState(7);
        var provider = new QueryRowCacheSchemaProvider(state);
        var first = Compile(query, $"QueryRowCacheShapeFirst_{Guid.NewGuid():N}", provider);
        var firstTransfer = Transfer(first.BuildItems!);
        var firstEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(first.BuildItems!, provider);
        using (var table = first.CompiledQuery!.Run())
            Assert.AreEqual(7, table[0][0]);
        first.CompiledQuery.Dispose();

        state.Configure(
            [new QueryRowCacheColumn(nameof(QueryRowCacheEntity.Text), 0, typeof(string))],
            [["changed"]]);
        SemanticTemplateCache.Clear();

        var second = Compile(query, $"QueryRowCacheShapeSecond_{Guid.NewGuid():N}", provider);
        var secondTransfer = Transfer(second.BuildItems!);
        var secondEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(second.BuildItems!, provider);
        Assert.IsFalse(second.BuildItems!.StopAfterPlanning);
        Assert.AreNotEqual(firstTransfer.Shape!.Fingerprint, secondTransfer.Shape!.Fingerprint);
        Assert.AreNotEqual(firstEntry, secondEntry);
        using (var table = second.CompiledQuery!.Run())
            Assert.AreEqual("changed", table[0][0]);
        second.CompiledQuery.Dispose();

        var third = Compile(query, $"QueryRowCacheShapeThird_{Guid.NewGuid():N}", provider);
        Assert.IsTrue(third.BuildItems!.StopAfterPlanning, "The updated semantic contract was not installed under the colliding exact key.");
        using (var table = third.CompiledQuery!.Run())
            Assert.AreEqual("changed", table[0][0]);
        third.CompiledQuery.Dispose();
    }

    [TestMethod]
    public void ToggledCapability_ShouldNotReuseQueryScopedArtifactForLegacyTransfer()
    {
        var query = UniqueQuery("select r.Value from #queryrowcache.rows() r where r.Value >= {0}");
        var state = NarrowState(31);
        var provider = new QueryRowCacheSchemaProvider(state);
        var queryScoped = Compile(query, "QueryRowCacheCapabilityQueryScoped", provider);
        var queryScopedEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(
            queryScoped.BuildItems!,
            provider);
        Assert.AreEqual(SourceTransferMode.QueryScopedRows, Transfer(queryScoped.BuildItems!).Mode);
        queryScoped.CompiledQuery!.Dispose();

        state.Configure(
            state.Columns,
            [[31]],
            SourceTransferCapabilities.None);
        SemanticTemplateCache.Clear();

        var legacy = Compile(query, "QueryRowCacheCapabilityLegacy", provider);
        var legacyEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(legacy.BuildItems!, provider);
        Assert.IsFalse(legacy.BuildItems!.StopAfterPlanning);
        Assert.AreEqual(SourceTransferMode.DeclaredRows, Transfer(legacy.BuildItems!).Mode);
        Assert.AreNotEqual(queryScopedEntry, legacyEntry);
        using (var table = legacy.CompiledQuery!.Run())
            Assert.AreEqual(31, table[0][0]);
        legacy.CompiledQuery.Dispose();

        var repeatedLegacy = Compile(query, "QueryRowCacheCapabilityLegacyRepeated", provider);
        Assert.IsTrue(repeatedLegacy.BuildItems!.StopAfterPlanning);
        repeatedLegacy.CompiledQuery!.Dispose();
    }

    [TestMethod]
    public void DifferingCarrierChoices_ShouldRemainArtifactIsolated()
    {
        var narrowProvider = new QueryRowCacheSchemaProvider(NarrowState(3));
        var scanLocal = Compile(
            UniqueQuery("select r.Value from #queryrowcache.rows() r where r.Value >= {0}"),
            "QueryRowCacheStruct",
            narrowProvider);
        var scanLocalEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(
            scanLocal.BuildItems!,
            narrowProvider);

        var wideProvider = new QueryRowCacheSchemaProvider(
            new QueryRowCacheState(WideColumns(), [WideValues()]));
        var wide = Compile(
            UniqueQuery("select r.G0, r.G1, r.G2, r.G3, r.G4 from #queryrowcache.rows() r where {0} <= 0"),
            "QueryRowCacheClass",
            wideProvider);
        var scanLocalTransfer = Transfer(scanLocal.BuildItems!);
        var wideTransfer = Transfer(wide.BuildItems!);

        Assert.AreEqual(SourceQueryRowCarrier.ReadonlyStruct, scanLocalTransfer.Carrier);
        Assert.AreEqual(SourceQueryRowCarrier.SealedClass, wideTransfer.Carrier);
        Assert.AreNotEqual(
            scanLocalEntry,
            InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(wide.BuildItems!, wideProvider));

        scanLocal.CompiledQuery!.Dispose();
        wide.CompiledQuery!.Dispose();
    }

    [TestMethod]
    public void QueryScopedArtifactCache_ShouldNotRetainGeneratedAssemblyCarrierOrMaterializer()
    {
        var structReferences = CompileInspectAndDisposeQueryScopedArtifact(
            NarrowState(41),
            UniqueQuery("select r.Value from #queryrowcache.rows() r where r.Value >= {0}"),
            SourceQueryRowCarrier.ReadonlyStruct,
            "struct");
        var classReferences = CompileInspectAndDisposeQueryScopedArtifact(
            new QueryRowCacheState(WideColumns(), [WideValues()]),
            UniqueQuery("select r.G0, r.G1, r.G2, r.G3, r.G4 from #queryrowcache.rows() r where {0} <= 0"),
            SourceQueryRowCarrier.SealedClass,
            "class");
        var references = structReferences.All.Concat(classReferences.All).ToArray();

        ForceCollection(references);

        foreach (var reference in references)
            Assert.IsFalse(reference.Reference.IsAlive, $"The generated {reference.Name} remained strongly reachable.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private GeneratedWeakReferences CompileInspectAndDisposeQueryScopedArtifact(
        QueryRowCacheState state,
        string query,
        SourceQueryRowCarrier expectedCarrier,
        string label)
    {
        var provider = new QueryRowCacheSchemaProvider(state);
        var result = Compile(
            query,
            $"QueryRowCacheUnload_{label}_{Guid.NewGuid():N}",
            provider);
        Assert.AreEqual(expectedCarrier, Transfer(result.BuildItems!).Carrier);
        var runtime = GetGeneratedRuntime(result.CompiledQuery!);
        var assembly = runtime.Type.Assembly;
        var generatedTypes = assembly.GetTypes();
        var carrier = generatedTypes.Single(static type =>
            type.Name.StartsWith("QueryRow_", StringComparison.Ordinal));
        var materializer = generatedTypes.Single(static type =>
            type.Name.StartsWith("QueryRowMaterializer_", StringComparison.Ordinal));
        var cacheEntry = GetCachedExecutionEntry(result.BuildItems!, provider);

        Assert.IsInstanceOfType<ClrAssemblyExecutableArtifact>(GetCachedExecutableArtifact(cacheEntry));
        AssertCacheEntryHasNoGeneratedReferences(cacheEntry, assembly);

        using (var table = result.CompiledQuery!.Run())
            Assert.AreEqual(1, table.Count);
        result.CompiledQuery.Dispose();

        return new GeneratedWeakReferences(
        [
            new NamedWeakReference($"{label} assembly load context", runtime.Context),
            new NamedWeakReference($"{label} assembly", assembly),
            new NamedWeakReference($"{label} runnable type", runtime.Type),
            new NamedWeakReference($"{label} query-row carrier type", carrier),
            new NamedWeakReference($"{label} query-row materializer type", materializer)
        ]);
    }

    private BuildResult Compile(string query, string assemblyName, QueryRowCacheSchemaProvider provider)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            $"{assemblyName}_{Guid.NewGuid():N}",
            provider,
            _loggerResolver,
            new CompilationOptions());
        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Errors));
        Assert.IsNotNull(result.BuildItems);
        Assert.IsNotNull(result.CompiledQuery);
        return result;
    }

    private static QueryRowCacheState NarrowState(int value)
    {
        return new QueryRowCacheState(
            [new QueryRowCacheColumn(nameof(QueryRowCacheEntity.Value), 0, typeof(int))],
            [[value]]);
    }

    private static IReadOnlyList<QueryRowCacheColumn> WideColumns()
    {
        return Enumerable.Range(0, 5)
            .Select(index => new QueryRowCacheColumn($"G{index}", index, typeof(Guid)))
            .ToArray();
    }

    private static object?[] WideValues()
    {
        return Enumerable.Range(0, 5)
            .Select(index => (object?)Guid.Parse($"00000000-0000-0000-0000-{index + 1:000000000000}"))
            .ToArray();
    }

    private static string UniqueQuery(string format)
    {
        var threshold = -Random.Shared.Next(1, int.MaxValue);
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, format, threshold);
    }

    private static SourceTransferStrategyPlan Transfer(BuildItems items)
    {
        return items.PlanningResult?.ExecutionArtifacts.SourceTransferPlansBySourceId?
                   .Values
                   .Single() ??
               throw new AssertFailedException("The compilation did not expose a source-transfer plan.");
    }

    private static GeneratedRuntime GetGeneratedRuntime(CompiledQuery query)
    {
        var runnableField = typeof(CompiledQuery).GetField("_runnable", BindingFlags.Instance | BindingFlags.NonPublic);
        var current = runnableField?.GetValue(query) ??
                      throw new AssertFailedException("The compiled query did not retain a runnable.");
        while (FindProperty(current.GetType(), "Inner")?.GetValue(current) is { } inner)
            current = inner;

        var type = current.GetType();
        var context = AssemblyLoadContext.GetLoadContext(type.Assembly) ??
                      throw new AssertFailedException("The generated assembly has no load context.");
        return new GeneratedRuntime(type, context);
    }

    private static PropertyInfo? FindProperty(Type type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is { } property)
                return property;
        }

        return null;
    }

    private static object GetCachedExecutionEntry(BuildItems items, QueryRowCacheSchemaProvider provider)
    {
        var identity = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(items, provider);
        Assert.AreNotEqual(0, identity);
        var entriesField = typeof(InstanceCreator).GetField(
            "ExecutionCompilationEntries",
            BindingFlags.Static | BindingFlags.NonPublic);
        var entries = entriesField?.GetValue(null) as IEnumerable ??
                      throw new AssertFailedException("The execution compilation cache was not found.");
        return entries.Cast<object>().Single(entry => RuntimeHelpers.GetHashCode(entry) == identity);
    }

    private static object GetCachedExecutableArtifact(object cacheEntry)
    {
        var template = cacheEntry.GetType().GetProperty("Template")?.GetValue(cacheEntry) ??
                       throw new AssertFailedException("The cached execution entry has no template.");
        return template.GetType().GetProperty(
                   "ExecutableArtifact",
                   BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(template) ??
               throw new AssertFailedException("The cached execution template has no executable artifact.");
    }

    private static void AssertCacheEntryHasNoGeneratedReferences(object cacheEntry, Assembly generatedAssembly)
    {
        var violations = new List<string>();
        InspectRetainedObject(
            cacheEntry,
            "execution-cache-entry",
            generatedAssembly,
            new HashSet<object>(ReferenceEqualityComparer.Instance),
            violations,
            depth: 0);

        Assert.AreEqual(0, violations.Count, string.Join(Environment.NewLine, violations));
    }

    private static void InspectRetainedObject(
        object? value,
        string path,
        Assembly generatedAssembly,
        HashSet<object> visited,
        ICollection<string> violations,
        int depth)
    {
        if (value is null || value is string || value.GetType().IsPrimitive || depth > 8)
            return;
        if (!visited.Add(value))
            return;

        switch (value)
        {
            case Type type when ContainsGeneratedType(type, generatedAssembly):
                violations.Add($"{path} retains generated type '{type}'.");
                return;
            case MethodInfo method when method.DeclaringType is { } declaringType &&
                                        ContainsGeneratedType(declaringType, generatedAssembly):
                violations.Add($"{path} retains generated method '{method}'.");
                return;
            case Delegate callback when callback.Method.DeclaringType is { } declaringType &&
                                        ContainsGeneratedType(declaringType, generatedAssembly):
                violations.Add($"{path} retains generated delegate '{callback.Method}'.");
                return;
        }

        var valueType = value.GetType();
        if (ContainsGeneratedType(valueType, generatedAssembly))
        {
            violations.Add($"{path} retains generated instance '{valueType}'.");
            return;
        }

        if (value is byte[])
            return;
        if (value is IEnumerable sequence)
        {
            var index = 0;
            foreach (var item in sequence)
                InspectRetainedObject(item, $"{path}[{index++}]", generatedAssembly, visited, violations, depth + 1);
            return;
        }

        if (valueType.Namespace?.StartsWith("Musoq.", StringComparison.Ordinal) != true)
            return;

        foreach (var field in InstanceFields(valueType))
        {
            InspectRetainedObject(
                field.GetValue(value),
                $"{path}.{field.Name}",
                generatedAssembly,
                visited,
                violations,
                depth + 1);
        }
    }

    private static bool ContainsGeneratedType(Type type, Assembly generatedAssembly)
    {
        return type.Assembly == generatedAssembly ||
               type.IsGenericType && type.GetGenericArguments().Any(argument => ContainsGeneratedType(argument, generatedAssembly));
    }

    private static IEnumerable<FieldInfo> InstanceFields(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            foreach (var field in current.GetFields(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
            {
                yield return field;
            }
        }
    }

    private static void ForceCollection(IReadOnlyList<NamedWeakReference> references)
    {
        for (var attempt = 0; attempt < 20 && references.Any(static reference => reference.Reference.IsAlive); attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(20);
        }
    }

    private sealed record GeneratedRuntime(Type Type, AssemblyLoadContext Context);

    private sealed class NamedWeakReference
    {
        public NamedWeakReference(string name, object target)
        {
            Name = name;
            Reference = new WeakReference(target);
        }

        public string Name { get; }

        public WeakReference Reference { get; }
    }

    private sealed record GeneratedWeakReferences(IReadOnlyList<NamedWeakReference> All);
}
