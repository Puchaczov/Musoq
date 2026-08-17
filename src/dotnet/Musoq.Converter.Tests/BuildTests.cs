using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Tests.Common;

namespace Musoq.Converter.Tests;

[TestClass]
public class BuildTests
{
    static BuildTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void CompileForStoreTest()
    {
        var query = "select 1 from #system.dual()";

        var (dllFile, pdbFile) = CreateForStore(query);

        Assert.IsNotNull(dllFile);
        Assert.IsNotNull(pdbFile);

        Assert.AreNotEqual(0, dllFile.Length);
        Assert.AreNotEqual(0, pdbFile.Length);
    }

    [TestMethod]
    public async Task CompileForStoreAsyncTest()
    {
        var query = "select 1 from #system.dual()";

        var arrays = await InstanceCreator.CompileForStoreAsync(query, Guid.NewGuid().ToString(),
            new SystemSchemaProvider(), new TestsLoggerResolver()).ConfigureAwait(false);

        Assert.IsNotNull(arrays.DllFile);
        Assert.IsNotNull(arrays.PdbFile);

        Assert.AreNotEqual(0, arrays.DllFile.Length);
        Assert.AreNotEqual(0, arrays.PdbFile.Length);
    }

    [TestMethod]
    public void CompileForTypedArtifact_WhenLoaded_ShouldRunTypedQuery()
    {
        var artifact = CompileDualArtifact();

        var row = LoadDualArtifact(artifact).Run().Single();

        Assert.AreEqual("single", row.Dummy);
    }

    [TestMethod]
    public void LoadTypedArtifact_WhenArtifactVersionIsUnsupported_ShouldReject()
    {
        var artifact = new VersionedArtifact(CompileDualArtifact(), 0);

        var exception = Assert.Throws<InvalidOperationException>(() => LoadDualArtifact(artifact));

        StringAssert.Contains(exception.Message, "Typed query artifact version '0' is not supported");
    }

    [TestMethod]
    public void CompiledTypedQueryArtifact_WhenConstructorInputsMutate_ShouldKeepLoadableSnapshot()
    {
        var sourceArtifact = CompileDualArtifact();
        var dllFile = sourceArtifact.DllFile;
        var pdbFile = sourceArtifact.PdbFile;
        var sourceRuntimeSettings = CopySourceRuntimeSettings(sourceArtifact);
        var sourceRuntimeSettingDescriptions = CopySourceRuntimeSettingDescriptions(sourceArtifact);
        var sourceExecutionPlans = sourceArtifact.SourceExecutionPlans.ToDictionary(
            static entry => entry.Key,
            static entry => entry.Value,
            StringComparer.Ordinal);
        var parameterDefinitions = sourceArtifact.ParameterDefinitions.ToList();
        var artifact = new CompiledTypedQueryArtifact(
            dllFile,
            pdbFile,
            sourceArtifact.RunnableTypeName,
            sourceArtifact.ResultMode,
            sourceArtifact.OutputType,
            sourceRuntimeSettings,
            sourceRuntimeSettingDescriptions,
            sourceExecutionPlans,
            parameterDefinitions);

        Array.Clear(dllFile);
        if (pdbFile != null)
            Array.Clear(pdbFile);
        sourceRuntimeSettings.Clear();
        sourceRuntimeSettingDescriptions.Clear();
        sourceExecutionPlans.Clear();
        parameterDefinitions.Clear();

        var row = LoadDualArtifact(artifact).Run().Single();

        Assert.AreEqual("single", row.Dummy);
    }

    [TestMethod]
    public void CompiledTypedQueryArtifact_WhenReturnedByteArraysMutate_ShouldKeepLoadableSnapshot()
    {
        var artifact = CompileDualArtifact();
        var dllFile = artifact.DllFile;
        var pdbFile = artifact.PdbFile;

        Array.Clear(dllFile);
        if (pdbFile != null)
            Array.Clear(pdbFile);

        var row = LoadDualArtifact(artifact).Run().Single();

        Assert.AreEqual("single", row.Dummy);
        Assert.AreNotEqual(0, artifact.DllFile[0]);
    }

    [TestMethod]
    public void CompiledTypedQueryArtifact_WhenSourcePlanInputsMutate_ShouldKeepMetadataSnapshot()
    {
        var artifact = CreateArtifactWithMutableSourcePlan(
            out var acceptedColumns,
            out var acceptedOrderBy,
            out var predicateValues,
            out var propertyTags,
            out var nestedProperties);

        acceptedColumns.Clear();
        acceptedOrderBy.Clear();
        predicateValues.Clear();
        propertyTags.Clear();
        nestedProperties.Clear();

        var plan = artifact.SourceExecutionPlans["mutable"];
        var predicate = (SourcePredicateIn)plan.AcceptedPredicate!;
        var nested = (IReadOnlyDictionary<string, object?>)plan.Properties["nested"]!;
        var tags = (IReadOnlyList<string>)nested["tags"]!;

        Assert.AreEqual(1, plan.AcceptedColumns.Count);
        Assert.AreEqual("Name", plan.AcceptedColumns[0].Name);
        Assert.AreEqual(1, plan.AcceptedOrderBy.Count);
        Assert.AreEqual(1, predicate.Values.Count);
        Assert.AreEqual(1, tags.Count);
        Assert.AreEqual("hot", tags[0]);
        Assert.AreEqual("single", ((SourcePredicateLiteral)predicate.Values[0]).Value);
        Assert.AreEqual("single", LoadDualArtifact(artifact).Run().Single().Dummy);
    }

    [TestMethod]
    public void CompiledTypedQueryArtifact_WhenReturnedSourcePlanCollectionsMutate_ShouldRejectAndRemainLoadable()
    {
        var artifact = CreateArtifactWithMutableSourcePlan(
            out _,
            out _,
            out _,
            out _,
            out _);
        var plan = artifact.SourceExecutionPlans["mutable"];
        var predicate = (SourcePredicateIn)plan.AcceptedPredicate!;
        var nested = (IReadOnlyDictionary<string, object?>)plan.Properties["nested"]!;
        var tags = (IReadOnlyList<string>)nested["tags"]!;

        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, SourceExecutionPlan>)artifact.SourceExecutionPlans).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<SourceColumnRef>)plan.AcceptedColumns).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<OrderByExpression>)plan.AcceptedOrderBy).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<SourcePredicateExpression>)predicate.Values).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, object?>)plan.Properties).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, object?>)nested).Clear());
        Assert.Throws<NotSupportedException>(() =>
            ((IList<string>)tags).Clear());

        Assert.AreEqual("single", LoadDualArtifact(artifact).Run().Single().Dummy);
    }

    [TestMethod]
    public void CompiledTypedQueryArtifact_WhenNestedKnownDictionariesMutate_ShouldKeepMetadataSnapshot()
    {
        var sourceArtifact = CompileDualArtifact();
        var readModifiers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["trim"] = "true"
        };
        var labels = new List<string> { "hot" };
        var nestedMap = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["labels"] = labels
        };
        var column = new SourceColumnRef("Name", readModifiers);
        var sourceExecutionPlans = sourceArtifact.SourceExecutionPlans.ToDictionary(
            static entry => entry.Key,
            static entry => entry.Value,
            StringComparer.Ordinal);
        sourceExecutionPlans["nested"] = new SourceExecutionPlan
        {
            Identity = new SourceIdentity("#test", "entities", "nested", "n"),
            AcceptedColumns = [column],
            AcceptedPredicate = new SourcePredicateLiteral(nestedMap),
            AcceptedOrderBy = [new OrderByExpression(column, OrderDirection.Descending)],
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["map"] = nestedMap,
                ["columns"] = new[] { column }
            }
        };
        var artifact = new CompiledTypedQueryArtifact(
            sourceArtifact.DllFile,
            sourceArtifact.PdbFile,
            sourceArtifact.RunnableTypeName,
            sourceArtifact.ResultMode,
            sourceArtifact.OutputType,
            sourceArtifact.SourceRuntimeSettingsBySourceContextId,
            sourceArtifact.SourceRuntimeSettingDescriptionsBySourceContextId,
            sourceExecutionPlans,
            sourceArtifact.ParameterDefinitions);

        readModifiers["trim"] = "false";
        readModifiers["pad"] = "left";
        labels.Add("cold");
        nestedMap["extra"] = "later";

        var plan = artifact.SourceExecutionPlans["nested"];
        var copiedColumn = plan.AcceptedColumns.Single();
        var copiedPredicateMap = (IReadOnlyDictionary<string, object?>)((SourcePredicateLiteral)plan.AcceptedPredicate!).Value!;
        var copiedPropertyMap = (IReadOnlyDictionary<string, object?>)plan.Properties["map"]!;
        var copiedLabels = (IReadOnlyList<string>)copiedPropertyMap["labels"]!;

        Assert.AreEqual("true", copiedColumn.ReadModifiers["trim"]);
        Assert.IsFalse(copiedColumn.ReadModifiers.ContainsKey("pad"));
        Assert.IsFalse(copiedPredicateMap.ContainsKey("extra"));
        Assert.IsFalse(copiedPropertyMap.ContainsKey("extra"));
        CollectionAssert.AreEqual(new[] { "hot" }, copiedLabels.ToArray());
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, string>)copiedColumn.ReadModifiers).Add("new", "value"));
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, object?>)copiedPropertyMap).Add("new", "value"));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<string>)copiedLabels).Add("new"));
    }

    private (byte[] DllFile, byte[] PdbFile) CreateForStore(string script)
    {
        return InstanceCreator.CompileForStore(script, Guid.NewGuid().ToString(), new SystemSchemaProvider(),
            new TestsLoggerResolver());
    }

    private static ICompiledTypedQueryArtifact CompileDualArtifact()
    {
        const string query = "select d.Dummy as Dummy from #system.dual() d";
        return InstanceCreator.CompileForTypedArtifact<DualDto>(
            query,
            Guid.NewGuid().ToString(),
            new SystemSchemaProvider(),
            new TestsLoggerResolver());
    }

    private static ICompiledTypedQueryArtifact CreateArtifactWithMutableSourcePlan(
        out List<SourceColumnRef> acceptedColumns,
        out List<OrderByExpression> acceptedOrderBy,
        out List<SourcePredicateExpression> predicateValues,
        out List<string> propertyTags,
        out Dictionary<string, object?> nestedProperties)
    {
        var sourceArtifact = CompileDualArtifact();
        acceptedColumns = [new SourceColumnRef("Name", new Dictionary<string, string> { ["trim"] = "true" })];
        acceptedOrderBy = [new OrderByExpression(acceptedColumns[0], OrderDirection.Ascending)];
        predicateValues = [new SourcePredicateLiteral("single")];
        propertyTags = ["hot"];
        nestedProperties = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["tags"] = propertyTags
        };
        var sourceExecutionPlans = sourceArtifact.SourceExecutionPlans.ToDictionary(
            static entry => entry.Key,
            static entry => entry.Value,
            StringComparer.Ordinal);
        sourceExecutionPlans["mutable"] = new SourceExecutionPlan
        {
            Identity = new SourceIdentity("#test", "entities", "mutable", "m"),
            AcceptedColumns = acceptedColumns,
            AcceptedPredicate = new SourcePredicateIn(
                new SourcePredicateColumn(acceptedColumns[0]),
                predicateValues),
            AcceptedOrderBy = acceptedOrderBy,
            AcceptedSkip = 1,
            AcceptedTake = 2,
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["nested"] = nestedProperties,
                ["columns"] = acceptedColumns,
                ["order"] = acceptedOrderBy
            }
        };

        return new CompiledTypedQueryArtifact(
            sourceArtifact.DllFile,
            sourceArtifact.PdbFile,
            sourceArtifact.RunnableTypeName,
            sourceArtifact.ResultMode,
            sourceArtifact.OutputType,
            sourceArtifact.SourceRuntimeSettingsBySourceContextId,
            sourceArtifact.SourceRuntimeSettingDescriptionsBySourceContextId,
            sourceExecutionPlans,
            sourceArtifact.ParameterDefinitions);
    }

    private static global::Musoq.Evaluator.CompiledTypedQuery<DualDto> LoadDualArtifact(
        ICompiledTypedQueryArtifact artifact)
    {
        return InstanceCreator.LoadTypedArtifact<DualDto>(
            artifact,
            new SystemSchemaProvider(),
            new TestsLoggerResolver());
    }

    private static Dictionary<string, IReadOnlyDictionary<string, string>> CopySourceRuntimeSettings(
        ICompiledTypedQueryArtifact artifact)
    {
        return artifact.SourceRuntimeSettingsBySourceContextId.ToDictionary(
            static entry => entry.Key,
            static entry => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(entry.Value, StringComparer.Ordinal),
            StringComparer.Ordinal);
    }

    private static Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> CopySourceRuntimeSettingDescriptions(
        ICompiledTypedQueryArtifact artifact)
    {
        return artifact.SourceRuntimeSettingDescriptionsBySourceContextId.ToDictionary(
            static entry => entry.Key,
            static entry => (IReadOnlyList<SourceRuntimeSettingDescription>)entry.Value.ToArray(),
            StringComparer.Ordinal);
    }

    public sealed record DualDto(string Dummy);

    private sealed class VersionedArtifact(
        ICompiledTypedQueryArtifact inner,
        int artifactVersion) : ICompiledTypedQueryArtifact
    {
        public int ArtifactVersion => artifactVersion;

        public string EngineVersion => inner.EngineVersion;

        public string RuntimeVersion => inner.RuntimeVersion;

        public string RuntimeContractSignature => inner.RuntimeContractSignature;

        public byte[] DllFile => inner.DllFile;

        public byte[]? PdbFile => inner.PdbFile;

        public string RunnableTypeName => inner.RunnableTypeName;

        public global::Musoq.Evaluator.IR.CodeGeneration.QueryResultMode ResultMode => inner.ResultMode;

        public Type OutputType => inner.OutputType;

        public string OutputTypeName => inner.OutputTypeName;

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId =>
            inner.SourceRuntimeSettingsBySourceContextId;

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId =>
            inner.SourceRuntimeSettingDescriptionsBySourceContextId;

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans => inner.SourceExecutionPlans;

        public IReadOnlyList<global::Musoq.Evaluator.ScriptParameterDefinition> ParameterDefinitions => inner.ParameterDefinitions;

        public IReadOnlyList<global::Musoq.Evaluator.ScriptParameterContract> ParameterContracts => inner.ParameterContracts;

        public IReadOnlyList<TypedArtifactSourceSlotIdentity> SourceSlotIdentities => inner.SourceSlotIdentities;
    }
}
