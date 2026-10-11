using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionIrArtifactImmutabilityTests
{
    [TestMethod]
    public void GeneratedScalarBindings_ShouldSnapshotTypeNamesAndDistinguishSpecializations()
    {
        var method = typeof(Musoq.Plugins.LibraryBase).GetMethod(nameof(Musoq.Plugins.LibraryBase.ToJson))!
            .MakeGenericMethod(typeof(object));
        var names = new Dictionary<int, string> { [0] = "Packet" };
        var call = new ExecutionMethodCall(method, [], null, typeof(string)) { GeneratedGenericArguments = names };
        names[0] = "Changed";
        Assert.AreEqual("Packet", call.GeneratedGenericArguments[0]);
        var changed = call with { GeneratedGenericArguments = names };
        names.Clear();
        Assert.AreEqual("Changed", changed.GeneratedGenericArguments[0]);
        Assert.AreNotEqual(ExecutionExpressionFingerprint.ForHoist(call), ExecutionExpressionFingerprint.ForHoist(changed));
    }

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
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(2, typeof(int))
        };
        var expression = new ExecutionValueTupleKey(parts, typeof((int, int)));

        parts.Clear();

        Assert.AreEqual(2, expression.Parts.Count);
    }
}
