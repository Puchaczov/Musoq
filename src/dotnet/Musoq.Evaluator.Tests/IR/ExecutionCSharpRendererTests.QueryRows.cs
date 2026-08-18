using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Planning;
using ExecutionCSharpRenderer = Musoq.Targets.CSharpClr.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void RenderClassMembers_WhenPlanSelectsQueryRowCarrier_ShouldHonorImmutablePlanChoice()
    {
        var renderer = new ExecutionCSharpRenderer();

        var structCode = RenderClassMembersCode(
            renderer,
            CreateQueryRowRendererPlan(
                ExecutionQueryRowCarrier.ReadonlyStruct,
                ExecutionQueryRowLifetime.ScanLocal));
        var classCode = RenderClassMembersCode(
            renderer,
            CreateQueryRowRendererPlan(
                ExecutionQueryRowCarrier.SealedClass,
                ExecutionQueryRowLifetime.EscapesScan));

        StringAssert.Contains(structCode, "private readonly struct QueryRow_AAAAAAAAAAAA_S");
        StringAssert.Contains(structCode, "QueryRowMaterializer_AAAAAAAAAAAA_S");
        StringAssert.Contains(
            structCode,
            "private static readonly QueryRowShape __queryRowShape_AAAAAAAAAAAA = new QueryRowShape");
        Assert.AreEqual(1, CountOccurrences(structCode, "new QueryRowShape"));
        Assert.IsFalse(structCode.Contains("sealed class QueryRow_AAAAAAAAAAAA", StringComparison.Ordinal));

        StringAssert.Contains(classCode, "private sealed class QueryRow_AAAAAAAAAAAA_C");
        StringAssert.Contains(classCode, "QueryRowMaterializer_AAAAAAAAAAAA_C");
        StringAssert.Contains(
            classCode,
            "private static readonly QueryRowShape __queryRowShape_AAAAAAAAAAAA = new QueryRowShape");
        Assert.IsFalse(classCode.Contains("readonly struct QueryRow_AAAAAAAAAAAA", StringComparison.Ordinal));
    }

    private static ExecutionPlan CreateQueryRowRendererPlan(
        ExecutionQueryRowCarrier carrier,
        ExecutionQueryRowLifetime lifetime)
    {
        var field = new FieldBinding(
            "Id",
            "source.Id",
            0,
            typeof(int),
            FieldNullability.NotNullable,
            new GeneratedFieldAccess(QueryRowSourceNaming.CreateFieldName(0)));
        var transfer = new ExecutionQueryRowSourceTransfer(
            carrier,
            lifetime,
            new string('A', 64),
            [new ExecutionQueryRowField(
                0,
                0,
                "Id",
                ExecutionClrBindingFactory.FromClr(typeof(int)),
                false)]);
        var binding = new ExecutionSourceBinding(
            "test",
            "rows",
            "source:0",
            0,
            [],
            [field],
            SourceType: ExecutionClrBindingFactory.FromClr(typeof(object)),
            QueryRowSourceTransfer: transfer);
        var shape = new GeneratedRowShape(
            QueryRowSourceNaming.CreateCarrierTypeName(transfer.ShapeFingerprint, carrier),
            [field],
            [],
            supportsGeneratedFieldAccess: true,
            requiresRowBase: false)
        {
            EmitAsValueType = carrier == ExecutionQueryRowCarrier.ReadonlyStruct,
            IsQueryScopedRow = true,
            SourceAlias = "source"
        };

        return new ExecutionPlan(
            "query-row-renderer",
            [shape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(
                    new ExecutionVariable("source", typeof(object)),
                    new ExecutionVariable("rows", typeof(object)),
                    binding)
            ]));
    }
}
