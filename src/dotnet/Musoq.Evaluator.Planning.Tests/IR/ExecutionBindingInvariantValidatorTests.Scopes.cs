using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionBindingInvariantValidatorTests
{
    [TestMethod]
    public void Validate_WhenInnerRowShadowsNativeAlias_ShouldRestoreOuterBindingAfterLoop()
    {
        var plan = CreateShadowedPlan(new PositionalAccess(0));

        ExecutionBindingInvariantValidator.Validate(plan);
    }

    [TestMethod]
    public void Validate_WhenShadowedRowUsesOuterAccessStrategy_ShouldRejectTheActiveBinding()
    {
        var plan = CreateShadowedPlan(new ClrPropertyAccess(nameof(BindingEntity.Key)));

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            ExecutionBindingInvariantValidator.Validate(plan)
        );

        StringAssert.Contains(exception.Message, "declared binding");
    }

    [TestMethod]
    public void Validate_WhenShadowedRowHasNoAccessStrategy_ShouldRejectBeforeRendering()
    {
        var plan = CreateShadowedPlan(null);

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() =>
            ExecutionBindingInvariantValidator.Validate(plan)
        );

        StringAssert.Contains(exception.Message, "has no access strategy");
    }

    private static ExecutionPlan CreateShadowedPlan(FieldAccessStrategy? innerAccess)
    {
        var native = new SourceEntityShape(
            "entity",
            typeof(BindingEntity),
            [
                new FieldBinding(
                    "Key",
                    "entity.Key",
                    0,
                    typeof(string),
                    FieldNullability.Unknown,
                    new ClrPropertyAccess(nameof(BindingEntity.Key))
                ),
            ]
        );
        var materialized = new TableRowShape("entity", CreatePositionalSource().Fields);
        var outerPlan = CreatePlan(
            native,
            new ExecutionFieldRead("entity", "Key", typeof(string), new ClrPropertyAccess(nameof(BindingEntity.Key)))
        );
        var innerPlan = CreatePlan(native, new ExecutionFieldRead("entity", "Key", typeof(string), innerAccess));
        var innerLoop = new ExecutionForEach(
            new ExecutionVariable("entity", typeof(Row)),
            new ExecutionVariableRead(new ExecutionVariable("materializedRows", typeof(Row[]))),
            innerPlan.Body
        );
        var outerLoop = new ExecutionForEach(
            new ExecutionVariable("entity", typeof(BindingEntity)),
            new ExecutionVariableRead(new ExecutionVariable("nativeRows", typeof(BindingEntity[]))),
            new ExecutionBlock([outerPlan.Body.Nodes[0], innerLoop, outerPlan.Body.Nodes[0]])
        );

        return outerPlan with
        {
            Shapes = [native, materialized],
            Body = new ExecutionBlock([outerLoop]),
        };
    }

    public sealed class BindingEntity
    {
        public string Key { get; init; } = string.Empty;
    }
}
