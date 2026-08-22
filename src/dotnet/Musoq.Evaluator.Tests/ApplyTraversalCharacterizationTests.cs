using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ApplyTraversalCharacterizationTests
{
    private static readonly CompilationOptions CompilationOptions = new(usePrimitiveTypeValidation: false);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void CrossApply_LeftPredicate_PrunesRejectedParentBeforeChildEnumeration()
    {
        var fixture = ApplyTraversalFixture.Create();
        using var query = Compile(
            "select b.X from #counting.parents() a cross apply a.Children b where a.Name = 'keep'",
            fixture);

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.X", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1], [2]);
        Assert.AreEqual(0, fixture.Rejected.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Kept.Children.EnumerationCount);
        Assert.IsGreaterThan(0, fixture.Schema.SourcePlanningRequestCount);
    }

    [TestMethod]
    public void ChainedCrossApply_PrunesAtEachAvailableBoundary()
    {
        var fixture = ApplyTraversalFixture.Create();
        using var query = Compile(
            "select c.Value from #counting.parents() a cross apply a.Children b cross apply b.Other c where a.Name = 'keep' and b.X = 1",
            fixture);

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("c.Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [10], [11]);
        Assert.AreEqual(0, fixture.Rejected.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Kept.Children.EnumerationCount);
        Assert.AreEqual(0, fixture.Rejected.Children.Single().Other.EnumerationCount);
        Assert.AreEqual(1, fixture.Kept.Children[0].Other.EnumerationCount);
        Assert.AreEqual(0, fixture.Kept.Children[1].Other.EnumerationCount);
    }

    [TestMethod]
    public void CrossApply_EmptyChildCollection_ShouldProduceNoRows()
    {
        var fixture = ApplyTraversalFixture.Create();
        using var query = Compile(
            "select a.Name, b.X from #counting.parents() a cross apply a.Children b where a.Name = 'empty'",
            fixture);

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.X", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
    }

    [TestMethod]
    public void OuterApply_EmptyChildCollection_ShouldPreserveNullExtension()
    {
        var fixture = ApplyTraversalFixture.Create();
        using var query = Compile(
            "select a.Name, b.X from #counting.parents() a outer apply a.Children b where a.Name = 'empty'",
            fixture);

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.X", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["empty", null]);
        Assert.AreEqual(0, fixture.Rejected.Children.EnumerationCount);
        Assert.AreEqual(0, fixture.Kept.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Empty.Children.EnumerationCount);
        Assert.AreEqual(0, fixture.Nullable.Children.EnumerationCount);
    }

    [TestMethod]
    public void OuterApply_LeftPredicate_RejectsParentWithoutUnmatchedOutput()
    {
        var fixture = ApplyTraversalFixture.Create();
        using var query = Compile(
            "select a.Name, b.X from #counting.parents() a outer apply a.Children b where a.Name = 'keep'",
            fixture);

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["keep", 1], ["keep", 2]);
        Assert.AreEqual(0, fixture.Rejected.Children.EnumerationCount);
    }

    [TestMethod]
    public void OuterApply_RightOnlyPredicate_RemainsResidual()
    {
        var fixture = ApplyTraversalFixture.Create();
        using var query = Compile(
            "select a.Name, b.X from #counting.parents() a outer apply a.Children b where b.X = 1",
            fixture);

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("b.X", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["keep", 1]);
        Assert.AreEqual(1, fixture.Rejected.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Kept.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Empty.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Nullable.Children.EnumerationCount);
    }

    [TestMethod]
    public void OuterApply_MixedPredicate_GuardsLeftAndKeepsRightResidual()
    {
        var fixture = ApplyTraversalFixture.Create();
        using var query = Compile(
            "select a.Name, b.X from #counting.parents() a outer apply a.Children b where a.Name = 'keep' and b.X = 2",
            fixture);

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["keep", 2]);
        Assert.AreEqual(0, fixture.Rejected.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Kept.Children.EnumerationCount);
        Assert.AreEqual(0, fixture.Empty.Children.EnumerationCount);
        Assert.AreEqual(0, fixture.Nullable.Children.EnumerationCount);
    }

    [TestMethod]
    public void OuterApply_RightIsNullPredicate_PreservesUnmatchedNullExtension()
    {
        var fixture = ApplyTraversalFixture.Create();
        using var query = Compile(
            "select a.Name, b.X from #counting.parents() a outer apply a.Children b where b.X is null",
            fixture);

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["empty", null]);
        Assert.AreEqual(1, fixture.Rejected.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Kept.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Empty.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Nullable.Children.EnumerationCount);
    }

    [TestMethod]
    public void CrossThenOuterApply_GuardsEachAvailableLeftScope()
    {
        var fixture = ApplyTraversalFixture.Create();
        using var query = Compile(
            "select b.X, c.Value from #counting.parents() a cross apply a.Children b outer apply b.Other c where a.Name = 'keep' and b.X = 1",
            fixture);

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 10], [1, 11]);
        Assert.AreEqual(0, fixture.Rejected.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Kept.Children.EnumerationCount);
        Assert.AreEqual(1, fixture.Kept.Children[0].Other.EnumerationCount);
        Assert.AreEqual(0, fixture.Kept.Children[1].Other.EnumerationCount);
    }

    [TestMethod]
    public void OuterApply_LeftPredicate_ExecutionPlanGuardsBeforeRightSource()
    {
        var fixture = ApplyTraversalFixture.Create();
        var inspection = InstanceCreator.CompileForInspection(
            "select b.X from #counting.parents() a outer apply a.Children b where a.Name = 'keep'",
            Guid.NewGuid().ToString(),
            new GenericSchemaProvider(new Dictionary<string, ISchema>
            {
                ["#counting"] = fixture.Schema
            }),
            new TestsLoggerResolver());

        var planText = inspection.ExecutionPlanText;
        var guardIndex = planText.IndexOf("ContinueIf [NOT (a.Name = 'keep')]", StringComparison.Ordinal);
        var sourceIndex = planText.IndexOf("EnumerableSource [a.Children", StringComparison.Ordinal);

        Assert.IsTrue(guardIndex >= 0 && guardIndex < sourceIndex, planText);
        Assert.IsFalse(planText.Contains("If [(ab.a.Name = 'keep')]", StringComparison.Ordinal), planText);
    }

    [TestMethod]
    public void CrossApply_NullablePredicate_ShouldUseSqlThreeValuedSemantics()
    {
        var fixture = ApplyTraversalFixture.Create();
        using var query = Compile(
            "select b.X from #counting.parents() a cross apply a.Children b where a.Enabled = true",
            fixture);

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.X", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1], [2]);
    }

    [TestMethod]
    public void CrossApply_AccessMethodSource_ShouldPruneBeforeMethodEnumeration()
    {
        var fixture = ApplyTraversalFixture.Create();
        using var query = Compile(
            "select b.Value from #counting.parents() a cross apply a.JustReturnArrayOfString() b where a.Name = 'keep'",
            fixture);

        var table = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Value", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["1"], ["2"], ["3"]);
    }

    [TestMethod]
    public void CrossApply_LeftPredicate_CurrentExecutionPlanGuardsBeforeChildLoop()
    {
        var fixture = ApplyTraversalFixture.Create();
        var inspection = InstanceCreator.CompileForInspection(
            "select b.X from #counting.parents() a cross apply a.Children b where a.Name = 'keep'",
            Guid.NewGuid().ToString(),
            new GenericSchemaProvider(new Dictionary<string, ISchema>
            {
                ["#counting"] = fixture.Schema
            }),
            new TestsLoggerResolver());

        var planText = inspection.ExecutionPlanText;
        var childLoopIndex = planText.IndexOf("ChunkedForEach [b in", StringComparison.Ordinal);
        var predicateIndex = planText.IndexOf("ContinueIf [NOT (a.Name = 'keep')]", StringComparison.Ordinal);

        Assert.IsTrue(childLoopIndex >= 0, planText);
        Assert.IsTrue(predicateIndex >= 0 && predicateIndex < childLoopIndex, planText);
        Assert.IsFalse(planText.Contains("If [(ab.a.Name = 'keep')]", StringComparison.Ordinal), planText);
    }

    [TestMethod]
    public void ChainedCrossApply_ExecutionPlanKeepsScopedGuardsAndResiduals()
    {
        var fixture = ApplyTraversalFixture.Create();
        var inspection = InstanceCreator.CompileForInspection(
            "select c.Value from #counting.parents() a cross apply a.Children b cross apply b.Other c where a.Name = 'keep' and b.X = 1 and c.Value = 11",
            Guid.NewGuid().ToString(),
            new GenericSchemaProvider(new Dictionary<string, ISchema>
            {
                ["#counting"] = fixture.Schema
            }),
            new TestsLoggerResolver());

        var planText = inspection.ExecutionPlanText;
        var parentGuard = planText.IndexOf("ContinueIf [NOT (a.Name = 'keep')]", StringComparison.Ordinal);
        var childSource = planText.IndexOf("EnumerableSource [a.Children", StringComparison.Ordinal);
        var childGuard = planText.IndexOf("ContinueIf [NOT (ab.b.X = 1)]", StringComparison.Ordinal);
        var grandchildSource = planText.IndexOf("EnumerableSource [ab.b.Other", StringComparison.Ordinal);

        Assert.IsTrue(parentGuard >= 0 && parentGuard < childSource, planText);
        Assert.IsTrue(childGuard >= 0 && childGuard < grandchildSource, planText);
        Assert.Contains("If [(c_Value = 11)]", planText);
        Assert.IsFalse(planText.Contains("a.Name = 'keep')]\n        AppendShape", StringComparison.Ordinal), planText);
        Assert.IsFalse(planText.Contains("b.X = 1)]\n        AppendShape", StringComparison.Ordinal), planText);
    }

    [TestMethod]
    public void ChainedCrossApply_GroupedAggregatePrunesBeforeNestedSources()
    {
        var fixture = ApplyTraversalFixture.Create();
        var inspection = InstanceCreator.CompileForInspection(
            "select a.Name, Sum(c.Value) as ValueSum from #counting.parents() a cross apply a.Children b cross apply b.Other c where a.Name = 'keep' and b.X = 1 group by a.Name",
            Guid.NewGuid().ToString(),
            new GenericSchemaProvider(new Dictionary<string, ISchema>
            {
                ["#counting"] = fixture.Schema
            }),
            new TestsLoggerResolver(),
            CompilationOptions);

        Assert.Contains("ApplyPredicateMovementPlan", inspection.PlanningText);
        Assert.Contains("ContinueIf [NOT (a.Name = 'keep')]", inspection.ExecutionPlanText);
        Assert.Contains("ContinueIf [NOT (ab.b.X = 1)]", inspection.ExecutionPlanText);
        Assert.IsFalse(inspection.ExecutionPlanText.Contains("a_Name = 'keep'", StringComparison.Ordinal));
        Assert.IsFalse(inspection.ExecutionPlanText.Contains("abc.b.X = 1", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CrossApply_AccessMethodSource_ExecutionPlanGuardsBeforeMethodSource()
    {
        var fixture = ApplyTraversalFixture.Create();
        var inspection = InstanceCreator.CompileForInspection(
            "select b.Value from #counting.parents() a cross apply a.JustReturnArrayOfString() b where a.Name = 'keep'",
            Guid.NewGuid().ToString(),
            new GenericSchemaProvider(new Dictionary<string, ISchema>
            {
                ["#counting"] = fixture.Schema
            }),
            new TestsLoggerResolver());

        var planText = inspection.ExecutionPlanText;
        var guardIndex = planText.IndexOf("ContinueIf [NOT (a.Name = 'keep')]", StringComparison.Ordinal);
        var sourceIndex = planText.IndexOf("EnumerableSource [JustReturnArrayOfString()", StringComparison.Ordinal);

        Assert.IsTrue(guardIndex >= 0 && guardIndex < sourceIndex, planText);
        Assert.IsFalse(planText.Contains("If [(ab.a.Name = 'keep')]", StringComparison.Ordinal), planText);
    }

    private static CompiledQuery Compile(string query, ApplyTraversalFixture fixture)
    {
        var result = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new GenericSchemaProvider(new Dictionary<string, ISchema>
            {
                ["#counting"] = fixture.Schema
            }),
            new TestsLoggerResolver(),
            CompilationOptions);

        return result;
    }
}

internal sealed class ApplyTraversalFixture
{
    private ApplyTraversalFixture(
        CountingApplySchema schema,
        CountingParent rejected,
        CountingParent kept,
        CountingParent empty,
        CountingParent nullable)
    {
        Schema = schema;
        Rejected = rejected;
        Kept = kept;
        Empty = empty;
        Nullable = nullable;
    }

    public CountingApplySchema Schema { get; }
    public CountingParent Rejected { get; }
    public CountingParent Kept { get; }
    public CountingParent Empty { get; }
    public CountingParent Nullable { get; }

    public static ApplyTraversalFixture Create()
    {
        var rejected = new CountingParent(
            "reject",
            false,
            [new CountingChild(9, [new CountingGrandchild(90)])]);
        var kept = new CountingParent(
            "keep",
            true,
            [
                new CountingChild(1, [new CountingGrandchild(10), new CountingGrandchild(11)]),
                new CountingChild(2, [new CountingGrandchild(20)])
            ]);
        var empty = new CountingParent("empty", false, []);
        var nullable = new CountingParent(null, null, [new CountingChild(3, [new CountingGrandchild(30)])]);
        var rows = new[] { rejected, kept, empty, nullable };
        var schema = new CountingApplySchema(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["parents"] = (new GenericEntityTable<CountingParent>(), new CountingParentRowSource(rows))
        });

        return new ApplyTraversalFixture(schema, rejected, kept, empty, nullable);
    }
}

internal sealed class CountingApplySchema(
    IReadOnlyDictionary<string, (ISchemaTable SchemaTable, object RowSource)> tables)
    : GenericSchema<GenericLibrary>(tables)
{
    public int SourcePlanningRequestCount { get; private set; }

    public override SourcePlanResult TryPlanSource(
        string name,
        SourcePlanRequest request,
        params object?[] parameters)
    {
        SourcePlanningRequestCount++;
        return SourcePlanResult.RejectAll(request);
    }
}

internal sealed class CountingParentRowSource(IReadOnlyList<CountingParent> rows) : RowSourceBase<CountingParent>
{
    protected override void CollectChunks(IChunkWriter<CountingParent> writer) => writer.Write(rows);
}

public sealed class CountingEnumerable<T>(IReadOnlyList<T> values) : IEnumerable<T>
{
    private readonly IReadOnlyList<T> _values = values;

    public int EnumerationCount { get; private set; }

    public IEnumerator<T> GetEnumerator()
    {
        EnumerationCount++;
        return new CountingEnumerator<T>(_values.GetEnumerator());
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public T this[int index] => _values[index];

    public T Single() => _values.Single();
}

public sealed class CountingEnumerator<T>(IEnumerator<T> inner) : IEnumerator<T>
{
    public T Current => inner.Current;

    object IEnumerator.Current => Current!;

    public bool MoveNext() => inner.MoveNext();

    public void Reset() => inner.Reset();

    public void Dispose() => inner.Dispose();
}

public sealed class CountingParent(string? name, bool? enabled, IReadOnlyList<CountingChild> children)
{
    public string? Name { get; } = name;
    public bool? Enabled { get; } = enabled;
    public CountingEnumerable<CountingChild> Children { get; } = new(children);
}

public sealed class CountingChild(int x, IReadOnlyList<CountingGrandchild> other)
{
    public int X { get; } = x;
    public CountingEnumerable<CountingGrandchild> Other { get; } = new(other);
}

public sealed class CountingGrandchild(int value)
{
    public int Value { get; } = value;
}
