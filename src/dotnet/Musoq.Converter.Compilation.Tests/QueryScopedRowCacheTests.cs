using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Planning;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Converter.Tests;

[TestClass]
[DoNotParallelize]
public sealed class QueryScopedRowCacheTests
{
    private const int FreshProviderThreshold = -1_000_001;
    private const int MutatedMetadataThreshold = -1_000_002;
    private const int CapabilityThreshold = -1_000_003;
    private const int NarrowCarrierThreshold = -1_000_004;
    private const int WideCarrierThreshold = -1_000_005;

    private readonly TestsLoggerResolver _loggerResolver = new();
    private readonly ConditionalWeakTable<CompiledQuery, GeneratedRuntime> _generatedRuntimes = new();
    private readonly Dictionary<(string Query, SourceTransferMode Mode), GeneratedRuntime> _artifacts = new();

    [TestMethod]
    public void IdenticalSqlAndShape_ShouldReuseArtifactAndBindFreshProviderState()
    {
        var query = QueryWithDeterministicThreshold(
            "select r.Value from #queryrowcache.rows() r where r.Value >= {0}",
            FreshProviderThreshold
        );
        var firstProvider = new QueryRowCacheSchemaProvider(NarrowState(11));
        var first = Compile(query, "QueryRowCacheFreshFirst", firstProvider);
        Assert.IsFalse(first.BuildItems!.StopAfterPlanning);

        var firstRuntime = GetGeneratedRuntime(first.CompiledQuery!);
        using (var table = first.CompiledQuery!.Run())
            Assert.AreEqual(11, table[0].ReadValue(0));

        var secondProvider = new QueryRowCacheSchemaProvider(NarrowState(22));
        var second = Compile(query, "QueryRowCacheFreshSecond", secondProvider);
        Assert.IsTrue(
            second.BuildItems!.StopAfterPlanning,
            "The identical semantic shape did not hit the execution artifact cache."
        );
        var secondRuntime = GetGeneratedRuntime(second.CompiledQuery!);

        Assert.AreEqual(firstRuntime.Type.Module.ModuleVersionId, secondRuntime.Type.Module.ModuleVersionId);
        Assert.AreNotSame(firstRuntime.Context, secondRuntime.Context);
        using (var table = second.CompiledQuery!.Run())
            Assert.AreEqual(22, table[0].ReadValue(0));

        first.CompiledQuery.Dispose();
        second.CompiledQuery.Dispose();
    }

    [TestMethod]
    public void MutatedMetadataForSameSqlAndProvider_ShouldReplaceCollidingExactCacheAlias()
    {
        var query = QueryWithDeterministicThreshold(
            "select * from #queryrowcache.rows() r where {0} <= 0",
            MutatedMetadataThreshold
        );
        var state = NarrowState(7);
        var provider = new QueryRowCacheSchemaProvider(state);
        var first = Compile(query, $"QueryRowCacheShapeFirst_{Guid.NewGuid():N}", provider);
        var firstTransfer = Transfer(first.BuildItems!);
        var firstEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(first.BuildItems!, provider);
        using (var table = first.CompiledQuery!.Run())
            Assert.AreEqual(7, table[0].ReadValue(0));
        first.CompiledQuery.Dispose();

        state.Configure(
            [new QueryRowCacheColumn(nameof(QueryRowCacheEntity.Text), 0, typeof(string))],
            [
                ["changed"],
            ]
        );
        SemanticTemplateCache.Clear();

        var second = Compile(query, $"QueryRowCacheShapeSecond_{Guid.NewGuid():N}", provider);
        var secondTransfer = Transfer(second.BuildItems!);
        var secondEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(second.BuildItems!, provider);
        Assert.IsFalse(second.BuildItems!.StopAfterPlanning);
        Assert.AreNotEqual(firstTransfer.Shape!.Fingerprint, secondTransfer.Shape!.Fingerprint);
        Assert.AreNotEqual(firstEntry, secondEntry);
        using (var table = second.CompiledQuery!.Run())
            Assert.AreEqual("changed", table[0].ReadValue(0));
        second.CompiledQuery.Dispose();

        var third = Compile(query, $"QueryRowCacheShapeThird_{Guid.NewGuid():N}", provider);
        Assert.IsTrue(
            third.BuildItems!.StopAfterPlanning,
            "The updated semantic contract was not installed under the colliding exact key."
        );
        using (var table = third.CompiledQuery!.Run())
            Assert.AreEqual("changed", table[0].ReadValue(0));
        third.CompiledQuery.Dispose();
    }

    [TestMethod]
    public void ToggledCapability_ShouldNotReuseQueryScopedArtifactForLegacyTransfer()
    {
        var query = QueryWithDeterministicThreshold(
            "select r.Value from #queryrowcache.rows() r where r.Value >= {0}",
            CapabilityThreshold
        );
        var state = NarrowState(31);
        var provider = new QueryRowCacheSchemaProvider(state);
        var queryScoped = Compile(query, "QueryRowCacheCapabilityQueryScoped", provider);
        var queryScopedEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(
            queryScoped.BuildItems!,
            provider
        );
        Assert.AreEqual(SourceTransferMode.QueryScopedRows, Transfer(queryScoped.BuildItems!).Mode);
        queryScoped.CompiledQuery!.Dispose();

        state.Configure(
            state.Columns,
            [
                [31],
            ],
            SourceTransferCapabilities.None
        );
        SemanticTemplateCache.Clear();

        var legacy = Compile(query, "QueryRowCacheCapabilityLegacy", provider);
        var legacyEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(legacy.BuildItems!, provider);
        Assert.IsFalse(legacy.BuildItems!.StopAfterPlanning);
        Assert.AreEqual(SourceTransferMode.DeclaredRows, Transfer(legacy.BuildItems!).Mode);
        Assert.AreNotEqual(queryScopedEntry, legacyEntry);
        using (var table = legacy.CompiledQuery!.Run())
            Assert.AreEqual(31, table[0].ReadValue(0));
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
            QueryWithDeterministicThreshold(
                "select r.Value from #queryrowcache.rows() r where r.Value >= {0}",
                NarrowCarrierThreshold
            ),
            "QueryRowCacheStruct",
            narrowProvider
        );
        var scanLocalEntry = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(
            scanLocal.BuildItems!,
            narrowProvider
        );

        var wideProvider = new QueryRowCacheSchemaProvider(new QueryRowCacheState(WideColumns(), [WideValues()]));
        var wide = Compile(
            QueryWithDeterministicThreshold(
                "select r.G0, r.G1, r.G2, r.G3, r.G4 from #queryrowcache.rows() r where {0} <= 0",
                WideCarrierThreshold
            ),
            "QueryRowCacheClass",
            wideProvider
        );
        var scanLocalTransfer = Transfer(scanLocal.BuildItems!);
        var wideTransfer = Transfer(wide.BuildItems!);

        Assert.AreEqual(SourceQueryRowCarrier.ReadonlyStruct, scanLocalTransfer.Carrier);
        Assert.AreEqual(SourceQueryRowCarrier.SealedClass, wideTransfer.Carrier);
        Assert.AreNotEqual(
            scanLocalEntry,
            InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(wide.BuildItems!, wideProvider)
        );

        scanLocal.CompiledQuery!.Dispose();
        wide.CompiledQuery!.Dispose();
    }

    private BuildResult Compile(string query, string assemblyName, QueryRowCacheSchemaProvider provider)
    {
        using var capture = new GeneratedAssemblyCapture();
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            $"{assemblyName}_{Guid.NewGuid():N}",
            provider,
            _loggerResolver,
            new CompilationOptions()
        );
        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Errors));
        Assert.IsNotNull(result.BuildItems);
        Assert.IsNotNull(result.CompiledQuery);
        var entry = (query, Transfer(result.BuildItems).Mode);
        var type = ResolveGeneratedType(result.BuildItems, entry, capture);
        var context =
            AssemblyLoadContext.GetLoadContext(type.Assembly)
            ?? throw new AssertFailedException("The generated assembly has no load context.");
        var runtime = new GeneratedRuntime(type, context);
        _generatedRuntimes.Add(result.CompiledQuery, runtime);
        _artifacts[entry] = runtime;
        return result;
    }

    private Type ResolveGeneratedType(
        BuildItems items,
        (string Query, SourceTransferMode Mode) entry,
        GeneratedAssemblyCapture capture
    )
    {
        if (items.ExecutableArtifact is ClrLoadedExecutableArtifact loaded)
            return loaded.RunnableType;
        if (items.ContainsKey(BuildItemKeys.AccessToClassPath))
            return capture.RequireType(items.AccessToClassPath);
        if (items.ExecutableArtifact is ClrAssemblyExecutableArtifact assembly)
            return capture.RequireType(assembly.RunnableTypeName);

        var previous = _artifacts[entry].Type;
        if (Transfer(items).Mode == SourceTransferMode.QueryScopedRows)
            return capture.RequireType(previous.FullName!);
        return previous;
    }

    private static QueryRowCacheState NarrowState(int value)
    {
        return new QueryRowCacheState(
            [new QueryRowCacheColumn(nameof(QueryRowCacheEntity.Value), 0, typeof(int))],
            [
                [value],
            ]
        );
    }

    private static IReadOnlyList<QueryRowCacheColumn> WideColumns()
    {
        return Enumerable
            .Range(0, 5)
            .Select(index => new QueryRowCacheColumn($"G{index}", index, typeof(Guid)))
            .ToArray();
    }

    private static object?[] WideValues()
    {
        return Enumerable
            .Range(0, 5)
            .Select(index => (object?)Guid.Parse($"00000000-0000-0000-0000-{index + 1:000000000000}"))
            .ToArray();
    }

    private static string QueryWithDeterministicThreshold(string format, int threshold)
    {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, format, threshold);
    }

    private static SourceTransferStrategyPlan Transfer(BuildItems items)
    {
        return items.PlanningResult?.ExecutionArtifacts.SourceTransferPlansBySourceId?.Values.Single()
            ?? throw new AssertFailedException("The compilation did not expose a source-transfer plan.");
    }

    private GeneratedRuntime GetGeneratedRuntime(CompiledQuery query) =>
        _generatedRuntimes.GetValue(
            query,
            static _ => throw new AssertFailedException("No assembly-load receipt for this compiled query.")
        );

    private sealed record GeneratedRuntime(Type Type, AssemblyLoadContext Context);
}
