using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void FinalOutputPlanning_FreshExclusiveSortBuffer_TransfersOwnership()
    {
        var plan = CreateOwnedSortPlan();

        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out var render));
        var owned = render.FinalSinkPlans.TypedOutput.OwnedBuffer;
        Assert.IsNotNull(owned);
        Assert.AreEqual("result", owned.SourceName);
        Assert.AreEqual("__musoqSequence", owned.SequenceField);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void FinalOutputPlanning_BufferHasAnotherConsumer_KeepsCopyingPath(bool afterSort)
    {
        var plan = CreateOwnedSortPlan();
        var nodes = plan.Body.Nodes.ToList();
        var source = new ExecutionVariable(afterSort ? "resultSorted" : "result", typeof(object));
        var position = afterSort ? nodes.Count - 1 : nodes.Count - 2;
        nodes.Insert(position, new ExecutionStoreTable(source, 0));
        plan = plan with { Body = new ExecutionBlock(nodes) };

        Assert.IsTrue(TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out var render));
        Assert.IsNull(render.FinalSinkPlans.TypedOutput.OwnedBuffer);
        Assert.IsNull(render.FinalSinkPlans.TableOutput.OwnedBuffer);
    }

    private static ExecutionPlan CreateOwnedSortPlan()
    {
        var plan = CreatePlan();
        var result = plan.FinalResult!;
        var target = new ExecutionVariable("resultSorted", typeof(object));
        var body = new ExecutionBlock(
        [
            ..plan.Body.Nodes.SkipLast(1),
            new ExecutionSortTable(result.Source, target, [new ExecutionOrderField("Name", 0, typeof(string), false)]),
            new ExecutionReturnTable(target)
        ]);
        return plan with { Body = body, FinalResult = result with { TableName = target.Name, Source = target } };
    }

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
