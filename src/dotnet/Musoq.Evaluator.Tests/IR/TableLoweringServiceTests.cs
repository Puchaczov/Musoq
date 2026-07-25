using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class TableLoweringServiceTests
{
    [TestMethod]
    public void PostOperationProjectionPlanner_WhenOrderKeyIsNotProjected_ShouldMaterializeHiddenSortField()
    {
        var sourceShape = new SourceEntityShape(
            "p",
            typeof(Person),
            [
                Field("Name", "p.Name", 0, typeof(string)),
                Field("Age", "p.Age", 1, typeof(int))
            ]);
        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(sourceShape);
        var publicFields = new[]
        {
            new ProjectedField("Name", new ColumnRef("p", "Name", typeof(string)), 0)
        };
        var planner = new PostOperationProjectionPlanner(CreateGeneratedShape, (_, _) => null);
        IReadOnlyList<PostOperation> operations =
        [
            new SortOperation([new OrderField(new ColumnRef("p", "Age", typeof(int)), Descending: false)])
        ];

        var result = planner.Create("result", "ResultRow", publicFields, operations, sourceLookup);

        Assert.IsTrue(result.IsBuilt, result.UnsupportedReason);
        Assert.HasCount(2, result.Value.MaterializedFields);
        Assert.AreEqual("Name", result.Value.MaterializedFields[0].OutputName);
        Assert.AreEqual("__sortKey0", result.Value.MaterializedFields[1].OutputName);
        Assert.AreEqual("resultWithSortKeys", result.Value.WorkingTable.Name);
        Assert.AreEqual("ResultRowWithSortKeys", result.Value.WorkingShape.TypeName);
        Assert.IsNotNull(result.Value.FinalProjection);
        var sort = Assert.IsInstanceOfType<SortOperation>(result.Value.PostOperations[0]);
        Assert.AreSame(result.Value.MaterializedFields, sort.ProjectedFields);
    }

    [TestMethod]
    public void TableCompletionPlanner_WhenTakeOperationIsApplied_ShouldAppendPostOperationNodeAndReturnTarget()
    {
        var planner = new TableCompletionPlanner(new PostOperationPlanner());
        var rowShape = CreateGeneratedShape("ResultRow", ProjectedFields(), RowShapeLookup.EmptySourceShapeLookup());
        var table = new ExecutionVariable("rows", typeof(object));
        var nodes = new List<ExecutionNode>();

        var result = planner.Complete(new TableCompletionRequest(
            [rowShape],
            nodes,
            table,
            rowShape,
            [new TakeOperation(2)]));

        Assert.IsTrue(result.IsBuilt, result.UnsupportedReason);
        Assert.HasCount(1, nodes);
        var take = Assert.IsInstanceOfType<ExecutionTakeTable>(nodes[0]);
        Assert.AreEqual("rowsTaken", take.Target.Name);
        Assert.AreEqual("rowsTaken", result.Table.Name);
        Assert.AreSame(rowShape, result.RowShape);
    }

    [TestMethod]
    public void TableCompletionPlanner_WhenDistinctCombinesWithFinalProjection_ShouldReturnUnsupportedReason()
    {
        var planner = new TableCompletionPlanner(new PostOperationPlanner());
        var rowShape = CreateGeneratedShape("ResultRow", ProjectedFields(), RowShapeLookup.EmptySourceShapeLookup());
        var table = new ExecutionVariable("rows", typeof(object));
        var finalProjection = new TableProjection(new ExecutionVariable("result", typeof(object)), rowShape, [0]);

        var result = planner.Complete(new TableCompletionRequest(
            [rowShape],
            [],
            table,
            rowShape,
            [],
            IsDistinct: true,
            FinalProjection: finalProjection));

        Assert.IsFalse(result.IsBuilt);
        StringAssert.Contains(result.UnsupportedReason, "distinct lowering");
        StringAssert.Contains(result.UnsupportedReason, "hidden sort fields");
    }

    private static ProjectedField[] ProjectedFields() =>
    [
        new("Name", new ColumnRef("p", "Name", typeof(string)), 0)
    ];

    private static GeneratedRowShape CreateGeneratedShape(
        string typeName,
        ProjectedField[] fields,
        IReadOnlyDictionary<string, RowShape> _)
    {
        return new GeneratedRowShape(
            typeName,
            fields
                .Select(field => Field(field.OutputName, field.OutputName, field.OutputIndex, field.Expression.ReturnType))
                .ToArray());
    }

    private static FieldBinding Field(string name, string qualifiedName, int index, Type type) =>
        new(name, qualifiedName, index, type, FieldNullability.Unknown, new GeneratedFieldAccess(name));

    private sealed class Person
    {
        public string Name { get; init; } = string.Empty;

        public int Age { get; init; }
    }
}
