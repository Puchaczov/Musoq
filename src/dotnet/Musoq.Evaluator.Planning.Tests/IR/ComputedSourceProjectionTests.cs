using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning.SourcePlanning;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Computed;
using Musoq.Schema;
using SchemaColumn = Musoq.Schema.DataSources.SchemaColumn;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ComputedSourceProjectionTests
{
    private const string Query = "select r.Id, r.Optional::long * 2 as Value from #computedtest.rows() r";

    [TestMethod]
    [DataRow(ComputedSourceBehavior.Accept, 1)]
    [DataRow(ComputedSourceBehavior.Reject, 0)]
    [DataRow(ComputedSourceBehavior.NoCapability, 0)]
    [DataRow(ComputedSourceBehavior.DeclaredRows, 0)]
    [DataRow(ComputedSourceBehavior.VolatileColumn, 0)]
    public void AcceptedAndResidualPaths_ReturnTheSameIndependentValues(ComputedSourceBehavior behavior, int accepted)
    {
        var provider = new ComputedSourceProvider(behavior);
        using var query = Compile(Query, provider);
        using var table = query.Run();
        CollectionAssert.AreEqual(new long?[] { null, 8, -2 }, table.Select(row => row.GetValue<long?>(1)).ToArray());
        Assert.AreEqual(accepted * 3, provider.Source.ComputedReads);
        Assert.AreEqual(accepted, provider.Source.Shape?.Fields.Count(field => field.Origin == QueryRowFieldOrigin.ComputedProjection) ?? 0);
        if (accepted > 0)
        {
            CollectionAssert.AreEqual(new[] { "Id" }, provider.Source.Shape!.Fields
                .Where(field => field.Origin == QueryRowFieldOrigin.SourceColumn).Select(field => field.Name).ToArray());
            Assert.IsTrue(provider.Source.Request!.RequiredColumns.Any(column => column.Name == "Optional"));
        }
    }

    [TestMethod]
    public void PartialAcceptance_LeavesTheSecondExpressionInCore()
    {
        var provider = new ComputedSourceProvider(ComputedSourceBehavior.Partial);
        using var query = Compile("select r.Optional::long + 2 as First, -(r.Id::long) as Second from #computedtest.rows() r", provider);
        using var table = query.Run();
        CollectionAssert.AreEqual(new long?[] { null, 6, 1 }, table.Select(row => row.GetValue<long?>(0)).ToArray());
        CollectionAssert.AreEqual(new long?[] { 2, 0, -3 }, table.Select(row => row.GetValue<long?>(1)).ToArray());
        Assert.AreEqual(2, provider.Source.Request!.RequestedComputedProjections.Count);
        Assert.AreEqual(3, provider.Source.ComputedReads);
        CollectionAssert.AreEqual(new[] { "Id" }, provider.Source.Shape!.Fields
            .Where(field => field.Origin == QueryRowFieldOrigin.SourceColumn).Select(field => field.Name).ToArray());
    }

    [TestMethod]
    public void RepeatedAcceptedExpression_TransfersOneValueWithoutItsInputs()
    {
        var provider = new ComputedSourceProvider();
        using var query = Compile("select r.Optional::long + 2 as First, r.Optional::long + 2 as Second from #computedtest.rows() r", provider);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, null], [6L, 6L], [1L, 1L]);

        Assert.AreEqual(1, provider.Source.Request!.RequestedComputedProjections.Count);
        Assert.AreEqual(QueryRowFieldOrigin.ComputedProjection, provider.Source.Shape!.Fields.Single().Origin);
        Assert.AreEqual(3, provider.Source.ComputedReads);
    }

    [TestMethod]
    [DataRow("r.Optional::long + 2", null, 6L, 1L)]
    [DataRow("-(r.Id::long)", 2L, 0L, -3L)]
    public void PortableNumericFamilies_PreserveNullsAndSigns(string expression, long? first, long second, long third)
    {
        var provider = new ComputedSourceProvider();
        using var query = Compile($"select {expression} as Value from #computedtest.rows() r", provider);
        using var table = query.Run();
        long?[] expected = [first, second, third];
        CollectionAssert.AreEqual(expected, table.Select(row => row.GetValue<long?>(0)).ToArray());
        Assert.AreEqual(3, provider.Source.ComputedReads);
    }

    [TestMethod]
    public void PortableCoalesce_PreservesNullableFallback()
    {
        var provider = new ComputedSourceProvider();
        using var query = Compile("select (r.Optional::long ?? r.Id::long) as Value from #computedtest.rows() r", provider);
        using var table = query.Run();
        CollectionAssert.AreEqual(new long?[] { -2, 4, -1 }, table.Select(row => row.GetValue<long?>(0)).ToArray());
        Assert.AreEqual(3, provider.Source.ComputedReads);
    }

    [TestMethod]
    public void PortableNullCheck_IsMaterializedAsABoolean()
    {
        var provider = new ComputedSourceProvider();
        using var query = Compile("select r.Optional is null as Missing from #computedtest.rows() r", provider);
        using var table = query.Run();
        CollectionAssert.AreEqual(new[] { true, false, false }, table.Select(row => row.GetValue<bool>(0)).ToArray());
        Assert.AreEqual(3, provider.Source.ComputedReads);
    }

    [TestMethod]
    [DataRow(ComputedSourceBehavior.Accept, 3)]
    [DataRow(ComputedSourceBehavior.Reject, 0)]
    public void PortableComparisons_PreserveSqlUnknownAndBooleanComposition(ComputedSourceBehavior behavior, int reads)
    {
        var provider = new ComputedSourceProvider(behavior);
        using var query = Compile("select r.Optional > 0 or r.Id > 0 as Value from #computedtest.rows() r", provider);
        using var table = query.Run();
        CollectionAssert.AreEqual(new bool?[] { null, true, true }, table.Select(row => row.GetValue<bool?>(0)).ToArray());
        Assert.AreEqual(reads, provider.Source.ComputedReads);
    }

    [TestMethod]
    [DataRow("select r.Id::long * 2 from #computedtest.rows() r where r.Id > 0")]
    [DataRow("select r.Id::long * 2 from #computedtest.rows() r take 1")]
    [DataRow("select r.Id::long * 2 from #computedtest.rows() r skip 1")]
    [DataRow("select r.Id::long * 2 from #computedtest.rows() r order by r.Id")]
    [DataRow("select case when r.Id > 0 then r.Id::long * 2 else 0 end from #computedtest.rows() r")]
    [DataRow("select a.Id::long * 2 from #computedtest.rows() a inner join #computedtest.rows() b on a.Id = b.Id")]
    [DataRow("select r.Id::long * 2 from #computedtest.rows() r cross apply r.Split('a,b', ',') d")]
    [DataRow("select Sum(r.Id::long * 2) from #computedtest.rows() r")]
    [DataRow("select Sum(r.Id::long * 2) over (order by r.Id) from #computedtest.rows() r")]
    [DataRow("select r.Id::int from #computedtest.rows() r where r.Id < 0")]
    [DataRow("select 1 / r.Id from #computedtest.rows() r")]
    [DataRow("select r.Id::byte from #computedtest.rows() r")]
    [DataRow("select r.Id::long * r.Id::long * r.Id::long from #computedtest.rows() r")]
    [DataRow("select r.Id::long * 2, r.Abs(r.Id) from #computedtest.rows() r")]
    [DataRow("select r.Id::long * 2, case when r.Id > 0 then 1 else 0 end from #computedtest.rows() r")]
    [DataRow("select r.Id::long * 2, 1 / r.Id from #computedtest.rows() r")]
    public void UnsafeEvaluationRegionsAndThrowingOperations_AreNotOffered(string sql)
    {
        var provider = new ComputedSourceProvider();
        _ = InstanceCreator.CompileForInspection(sql, "computedregion", provider, new TestsLoggerResolver());
        Assert.AreEqual(0, provider.Source.Request!.RequestedComputedProjections.Count);
    }

    [TestMethod]
    public void MixedPortableAndPluginProjection_KeepsBothValuesInCore()
    {
        var provider = new ComputedSourceProvider();
        using var query = Compile("select r.Id::long * 2, r.Abs(r.Id) from #computedtest.rows() r", provider);
        using var table = query.Run();
        CollectionAssert.AreEqual(new long?[] { -4, 0, 6 }, table.Select(row => row.GetValue<long?>(0)).ToArray());
        CollectionAssert.AreEqual(new int?[] { 2, 0, 3 }, table.Select(row => row.GetValue<int?>(1)).ToArray());
        Assert.AreEqual(0, provider.Source.Request!.RequestedComputedProjections.Count);
        Assert.AreEqual(0, provider.Source.ComputedReads);
    }

    [TestMethod]
    public void UnresolvedSourceColumnMetadata_IsNotPortable()
    {
        var descriptor = new SourceDescriptor
        {
            Identity = SourceIdentity.Empty,
            Columns = [new SchemaColumn("Id", 0, typeof(int), intendedTypeName: "Missing.Provider.Type")]
        };
        var scan = new SchemaScanNode("computedtest", "rows", [], "r", new OutputSchema([]));
        Assert.IsNull(SourceScalarProjectionConverter.Convert(new ColumnRef("r", "Id", typeof(int)), scan, descriptor));
    }

    [TestMethod]
    [DataRow(ComputedSourceBehavior.Unknown)]
    [DataRow(ComputedSourceBehavior.Duplicate)]
    [DataRow(ComputedSourceBehavior.Modified)]
    [DataRow(ComputedSourceBehavior.Unstable)]
    [DataRow(ComputedSourceBehavior.WrongType)]
    [DataRow(ComputedSourceBehavior.MissingResidual)]
    [DataRow(ComputedSourceBehavior.MissingExecution)]
    [DataRow(ComputedSourceBehavior.ExtraResidual)]
    public void MalformedAcceptance_FailsBeforeExecution(ComputedSourceBehavior behavior)
    {
        var provider = new ComputedSourceProvider(behavior);
        var error = Assert.Throws<MusoqQueryException>(() => Compile(Query, provider));
        Assert.AreEqual(Musoq.Parser.Diagnostics.DiagnosticCode.MQ3071_SourceContractError, error.PrimaryEnvelope.Code);
        StringAssert.Contains(error.Message, "computed projection", StringComparison.OrdinalIgnoreCase);
        Assert.AreEqual(0, provider.Source.ComputedReads);
    }

    [TestMethod]
    public void MissingAcceptedValue_FailsWithoutResidualRecomputation()
    {
        var provider = new ComputedSourceProvider(ComputedSourceBehavior.MissingValue);
        using var query = Compile(Query, provider);
        var error = Assert.Throws<QueryExecutionException>(() =>
        {
            using var table = query.Run();
            _ = table.ToArray();
        });
        StringAssert.Contains(error.ToString(), "Missing accepted computed value");
        Assert.AreEqual(0, provider.Source.ComputedReads);
    }

    private static CompiledQuery Compile(string sql, ComputedSourceProvider provider)
        => InstanceCreator.CompileForExecution(sql, "computedtest", provider, new TestsLoggerResolver());

    [TestMethod]
    public void CompiledArtifactRoundTrip_PreservesAcceptedSourceExecutionMetadata()
    {
        var artifact = InstanceCreator.CompileArtifactWithDiagnostics(Query, "computedartifact",
            new ComputedSourceProvider(), new TestsLoggerResolver());
        Assert.IsTrue(artifact.Succeeded);
        var provider = new ComputedSourceProvider();
        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(Query, artifact.Artifact!, provider, new TestsLoggerResolver());
        Assert.IsTrue(loaded.Succeeded, string.Join("; ", loaded.Diagnostics.Select(item => item.Message)));
        using var query = loaded.CompiledQuery!;
        using var table = query.Run();
        CollectionAssert.AreEqual(new long?[] { null, 8, -2 }, table.Select(row => row.GetValue<long?>(1)).ToArray());
        Assert.AreEqual(3, provider.Source.ComputedReads);
    }

    [TestMethod]
    public void ArtifactAcceptanceChange_InvalidatesItsSemanticIdentity()
    {
        var artifact = InstanceCreator.CompileArtifactWithDiagnostics(Query, "computedartifact",
            new ComputedSourceProvider(), new TestsLoggerResolver());
        Assert.IsTrue(artifact.Succeeded);
        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(Query, artifact.Artifact!,
            new ComputedSourceProvider(ComputedSourceBehavior.Reject), new TestsLoggerResolver());
        Assert.IsFalse(loaded.Succeeded);
        Assert.IsTrue(loaded.Diagnostics.Any(item => item.Message.Contains("semantic", StringComparison.OrdinalIgnoreCase)));
    }
}
