using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using ExecutionCSharpRenderer = Musoq.Targets.CSharpClr.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void CanRender_IndexedLoopWithTypedDirectItem_ShouldReturnTrue()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateIndexedRowAccessPlan(typeof(string), null, ExecutionRowAccessMode.Direct);

        Assert.IsTrue(renderer.CanRender(plan), renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void CanRender_IndexedLoopWithGeneratedRowObjectDirectItem_ShouldReturnTrue()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateIndexedRowAccessPlan(
            typeof(object),
            "GeneratedItemRow",
            ExecutionRowAccessMode.Direct);

        Assert.IsTrue(renderer.CanRender(plan), renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void CanRender_IndexedLoopWithUntypedObjectDirectItem_ShouldReturnFalse()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateIndexedRowAccessPlan(typeof(object), null, ExecutionRowAccessMode.Direct);

        Assert.IsFalse(renderer.CanRender(plan));
        Assert.AreEqual(
            "Execution IR C# backend cannot render node ExecutionForEachIndexed.",
            renderer.GetUnsupportedReason(plan));
    }

    [TestMethod]
    public void CanRender_IndexedLoopWithUnsupportedAccessMode_ShouldReturnFalse()
    {
        var renderer = new ExecutionCSharpRenderer();
        var plan = CreateIndexedRowAccessPlan(typeof(object), null, (ExecutionRowAccessMode)99);

        Assert.IsFalse(renderer.CanRender(plan));
        Assert.AreEqual("Execution IR C# backend cannot render node ExecutionForEachIndexed.",
            renderer.GetUnsupportedReason(plan));
    }

    private static ExecutionPlan CreateIndexedRowAccessPlan(
        Type itemType,
        string? generatedRowTypeName,
        ExecutionRowAccessMode rowAccessMode)
    {
        var item = new ExecutionVariable("item", itemType, generatedRowTypeName);
        var index = new ExecutionVariable("rowIndex", typeof(int));
        var source = new ExecutionVariable("rows", typeof(object[]));
        var loop = new ExecutionForEachIndexed(
            item,
            index,
            source,
            rowAccessMode,
            ExecutionBlock.Empty);

        return new ExecutionPlan(
            "Q_IndexedRowAccessCapability",
            [],
            new ExecutionBlock([loop]));
    }
}
