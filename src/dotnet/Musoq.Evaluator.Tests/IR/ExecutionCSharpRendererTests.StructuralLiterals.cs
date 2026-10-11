using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Schema.StructuralInputs;
using Musoq.Targets.CSharpClr;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void PublicRecordLiteral_ShouldEmitPresentFieldsAndExplicitNull()
    {
        var record = new ExecutionStructuralRecord(
            ExecutionClrBindingFactory.FromClr(typeof(StructuralValue)),
            [
                new ExecutionStructuralField("Id", new ExecutionLiteral(3, typeof(int))),
                new ExecutionStructuralField("Absent", new ExecutionLiteral(7, typeof(int)), IsPresent: false),
                new ExecutionStructuralField("Mode", new ExecutionLiteral(null, typeof(string)))
            ]) { GeneratedTypeName = "__musoqStructuralCarrier_0_root" };
        var plan = CreateProjectionPlan("Q_PublicRecord", "Value", typeof(StructuralValue), record);
        var renderer = new ExecutionCSharpRenderer();

        Assert.IsTrue(renderer.CanRender(plan), renderer.GetUnsupportedReason(plan));
        var code = renderer.RenderMethod(plan, "ExecutePlan").NormalizeWhitespace().ToFullString();

        Assert.Contains("new __musoqStructuralCarrier_0_root(3, 7, null, 5UL)", code);
        Assert.DoesNotContain("StructuralValue.FromRecord", code);
        Assert.DoesNotContain("StructuralValue.FromScalar", code);
    }

    [TestMethod]
    public void ReceiverRecordWithoutConstructionPlan_ShouldRemainUnsupported()
    {
        var record = new ExecutionStructuralRecord(
            ExecutionClrBindingFactory.FromClr(typeof(Uri)),
            [new ExecutionStructuralField("Value", new ExecutionLiteral("todo", typeof(string)))]);
        var plan = CreateProjectionPlan("Q_UnboundReceiverRecord", "Value", typeof(Uri), record);

        Assert.IsFalse(new ExecutionCSharpRenderer().CanRender(plan));
    }
}
