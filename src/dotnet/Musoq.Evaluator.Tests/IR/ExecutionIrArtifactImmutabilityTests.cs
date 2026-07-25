using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionIrArtifactImmutabilityTests
{
    [TestMethod]
    public void ExecutionBlock_ShouldSnapshotTheSourceNodeList()
    {
        var nodes = new List<ExecutionNode>();
        var block = new ExecutionBlock(nodes);

        nodes.Add(new ExecutionReturnDesc("schema", "method", DescType.Query, null, [], "ctx", 0));

        Assert.AreEqual(0, block.Nodes.Count);

        var rewrittenNodes = new List<ExecutionNode>
        {
            new ExecutionReturnDesc("schema", "method", DescType.Query, null, [], "ctx", 0)
        };
        var rewritten = block with { Nodes = rewrittenNodes };
        rewrittenNodes.Clear();

        Assert.AreEqual(1, rewritten.Nodes.Count);
    }

    [TestMethod]
    public void ExecutionPlan_ShouldSnapshotShapesAndNestedShapeFields()
    {
        var fields = new List<FieldBinding>();
        var shape = new GeneratedRowShape("Row", fields);
        var shapes = new List<RowShape> { shape };
        var plan = new ExecutionPlan("query", shapes, ExecutionBlock.Empty);

        fields.Add(new FieldBinding(
            "Name",
            "Row.Name",
            0,
            ExecutionClrBindingFactory.FromClr(typeof(string)),
            FieldNullability.NotNullable,
            new GeneratedFieldAccess("Name")));
        shapes.Clear();

        Assert.AreEqual(1, plan.Shapes.Count);
        Assert.AreEqual(0, plan.Shapes[0].Fields.Count);

        var rewrittenFields = new List<FieldBinding>();
        var rewrittenShape = (GeneratedRowShape)shape with { Fields = rewrittenFields };
        rewrittenFields.Add(shape.Fields.Count == 0
            ? new FieldBinding(
                "Id",
                "Row.Id",
                0,
                ExecutionClrBindingFactory.FromClr(typeof(int)),
                FieldNullability.NotNullable,
                new GeneratedFieldAccess("Id"))
            : shape.Fields[0]);

        Assert.AreEqual(0, rewrittenShape.Fields.Count);
    }

    [TestMethod]
    public void CollectionExpressions_ShouldSnapshotTheirParts()
    {
        var parts = new List<ExecutionExpression>
        {
            new ExecutionCompositeKey([])
        };
        var expression = new ExecutionCompositeKey(parts);

        parts.Clear();

        Assert.AreEqual(1, expression.Parts.Count);
    }
}
