using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionBindingInvariantValidatorTests
{
    [TestMethod]
    public void Validate_WhenKnownFieldHasNoAccessStrategy_ShouldRejectBeforeRendering()
    {
        var source = CreatePositionalSource();
        var plan = CreatePlan(
            source,
            new ExecutionFieldRead("entity", "Value", typeof(string)));

        var exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => ExecutionBindingInvariantValidator.Validate(plan));

        StringAssert.Contains(exception.Message, "has no access strategy");
    }

    [TestMethod]
    public void Validate_WhenFieldStrategyDoesNotMatchCarrierBinding_ShouldRejectBeforeRendering()
    {
        var source = CreatePositionalSource();
        var plan = CreatePlan(
            source,
            new ExecutionFieldRead("entity", "Key", typeof(string), new ClrPropertyAccess("Key")));

        var exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => ExecutionBindingInvariantValidator.Validate(plan));

        StringAssert.Contains(exception.Message, "declared binding");
    }

    private static SourceEntityShape CreatePositionalSource()
    {
        return new SourceEntityShape(
            "entity",
            typeof(object[]),
            [new FieldBinding(
                "Key",
                "entity.Key",
                0,
                typeof(string),
                FieldNullability.Unknown,
                new PositionalAccess(0))]);
    }

    private static ExecutionPlan CreatePlan(SourceEntityShape source, ExecutionExpression expression)
    {
        var outputShape = new GeneratedRowShape(
            "ResultRow",
            [new FieldBinding(
                "Value",
                "Value",
                0,
                typeof(string),
                FieldNullability.Unknown,
                new GeneratedFieldAccess("Value"))]);
        var append = new ExecutionAppendRow(
            new ExecutionVariable("result", typeof(object)),
            outputShape,
            [new ExecutionRowValue("Value", expression)]);

        return new ExecutionPlan(
            "validator-test",
            [source],
            new ExecutionBlock([append]));
    }
}
