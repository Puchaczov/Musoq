using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void FinalOutputPlanning_ReusesOneResultForTheSameImmutablePlan()
    {
        var plan = CreatePlan();

        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out var first));
        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out var second));

        Assert.AreSame(first, second);
        Assert.IsTrue(first.FinalSinkPlans.TableOutput.IsSupported);
        Assert.IsFalse(first.FinalSinkPlans.TableOutput.BufferFinalRows);
        Assert.IsFalse(first.FinalSinkPlans.TableOutput.ReuseStoredRows);
    }

    [TestMethod]
    public void FinalOutputPlanning_RecordCopyWithDifferentBodyHasIndependentDecisions()
    {
        var original = CreatePlan();
        var copy = original with { Body = new ExecutionBlock([]) };

        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolveRenderPlan(original, out var first));
        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolveRenderPlan(copy, out var second));

        Assert.AreNotSame(first, second);
        Assert.IsTrue(first.FinalSinkPlans.TableOutput.IsSupported);
        Assert.IsFalse(second.FinalSinkPlans.TableOutput.IsSupported);
    }

    [TestMethod]
    public void FinalOutputPlanning_PostOperationsShareImmutableSourceBufferMappings()
    {
        var plan = CreateProjectionPostOperationPlan();
        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out var renderPlan));
        var output = renderPlan.FinalSinkPlans.TableOutput;

        CollectionAssert.AreEquivalent(
            new[] { "result", "resultDistinct", "resultSorted", "resultSkipped" },
            new List<string>(output.SourceBuffers.Keys));
        Assert.IsTrue(output.BufferFinalRows);
        Assert.IsFalse(output.ReuseStoredRows);
        Assert.AreEqual(FinalOutputRowRepresentation.SelectShape, output.RowRepresentation);
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<string, string>)output.SourceBuffers).Add("unexpected", "ResultShape0"));
    }

    [TestMethod]
    public void FinalOutputPlanning_SerialTableRowsKeepTypedAdaptationAndOwnershipIndependent()
    {
        var plan = CreatePlan();
        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out var renderPlan));

        Assert.AreEqual(FinalOutputRowRepresentation.GeneratedRow, renderPlan.FinalSinkPlans.TableOutput.RowRepresentation);
        Assert.AreEqual(FinalOutputRowRepresentation.SelectShape, renderPlan.FinalSinkPlans.TypedOutput.RowRepresentation);
        Assert.IsTrue(renderPlan.FinalSinkPlans.TableOutput.PreserveEnumerationBoundary);
        Assert.IsFalse(renderPlan.FinalSinkPlans.TableOutput.ReuseStoredRows);
        Assert.IsFalse(renderPlan.FinalSinkPlans.TableOutput.BufferFinalRows);
    }

    [TestMethod]
    public void FinalOutputPlanning_MismatchedRowLayoutKeepsShapeAdaptation()
    {
        var plan = CreatePlan();
        plan = plan with { FinalResult = plan.FinalResult! with { Shape = new GeneratedRowShape("OtherRow", plan.FinalResult.Shape.Fields) } };

        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out var renderPlan));

        Assert.AreEqual(FinalOutputRowRepresentation.SelectShape, renderPlan.FinalSinkPlans.TableOutput.RowRepresentation);
    }

    [TestMethod]
    public void FinalOutputPlanning_ExistingRowAppendKeepsShapeAdaptation()
    {
        var plan = CreatePlan();
        var result = plan.FinalResult!;
        var sharedRow = new ExecutionVariable("sharedRow", typeof(object));
        plan = plan with
        {
            Body = new ExecutionBlock(
            [
                new ExecutionCreateTable(result.Source, result.Shape),
                new ExecutionCreateGeneratedRow(sharedRow, result.Shape,
                    [new ExecutionRowValue("Name", new ExecutionLiteral("shared", typeof(string)))], []),
                new ExecutionAppendExistingRow(result.Source, sharedRow),
                new ExecutionAppendExistingRow(result.Source, sharedRow),
                new ExecutionReturnTable(result.Source)
            ])
        };

        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out var renderPlan));

        Assert.AreEqual(FinalOutputRowRepresentation.SelectShape, renderPlan.FinalSinkPlans.TableOutput.RowRepresentation);
        Assert.IsFalse(renderPlan.FinalSinkPlans.TableOutput.ReuseStoredRows);
    }
}
