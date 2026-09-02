using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class LoopInvariantCodeMotionPassTests
{
    [TestMethod]
    public void Optimize_WhenTwoSerialLoopsReadOuterAndInnerFields_ShouldPlaceLetsInOwnerScopes()
    {
        var a = Var("a", typeof(object));
        var b = Var("b", typeof(object));
        var outer = new ExecutionForEach(
            a,
            Rows("aRows"),
            new ExecutionBlock([
                new ExecutionForEach(
                    b,
                    Rows("bRows"),
                    new ExecutionBlock([
                        new ExecutionForEach(
                            Var("c", typeof(object)),
                            Rows("cRows"),
                            new ExecutionBlock([
                                Append(Field("a", "Value"), Field("b", "Value"), Field("c", "Value"))
                            ]))
                    ]))
            ]));

        var result = Optimize(outer);

        Assert.IsTrue(result.IsChanged);
        var rewrittenOuter = (ExecutionForEach)result.Plan.Body.Nodes[0];
        var outerLet = (ExecutionLet)rewrittenOuter.Body.Nodes[0];
        var rewrittenInner = (ExecutionForEach)rewrittenOuter.Body.Nodes[1];
        var innerLet = (ExecutionLet)rewrittenInner.Body.Nodes[0];
        var rewrittenLeaf = (ExecutionForEach)rewrittenInner.Body.Nodes[1];
        var append = (ExecutionAppendRow)rewrittenLeaf.Body.Nodes[0];

        Assert.AreEqual("aValue", outerLet.Variable.Name);
        Assert.AreEqual("bValue", innerLet.Variable.Name);
        Assert.AreEqual("a", ((ExecutionFieldRead)outerLet.Value).Alias);
        Assert.AreEqual("b", ((ExecutionFieldRead)innerLet.Value).Alias);
        Assert.AreSame(outerLet.Variable, ((ExecutionVariableRead)append.Values[0].Value).Variable);
        Assert.AreSame(innerLet.Variable, ((ExecutionVariableRead)append.Values[1].Value).Variable);
        Assert.IsInstanceOfType<ExecutionFieldRead>(append.Values[2].Value);
        StringAssert.Contains(result.Reason, "Placements");
    }

    [TestMethod]
    public void Optimize_WhenStableCompositionIsRepeatedByDescendant_ShouldHoistMaximalExpression()
    {
        var expression = new ExecutionBinary(
            BinaryOpKind.Add,
            Field("a", "Value"),
            new ExecutionLiteral(1, typeof(int)),
            typeof(int));
        var outer = new ExecutionForEach(
            Var("a", typeof(object)),
            Rows("aRows"),
            new ExecutionBlock([
                new ExecutionForEach(
                    Var("b", typeof(object)),
                    Rows("bRows"),
                    new ExecutionBlock([Append(expression)]))
            ]));

        var result = Optimize(outer);

        Assert.IsTrue(result.IsChanged);
        var rewrittenOuter = (ExecutionForEach)result.Plan.Body.Nodes[0];
        var let = (ExecutionLet)rewrittenOuter.Body.Nodes[0];
        var inner = (ExecutionForEach)rewrittenOuter.Body.Nodes[1];
        var value = ((ExecutionAppendRow)inner.Body.Nodes[0]).Values[0].Value;
        Assert.AreEqual(expression, let.Value);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)value).Variable);
    }

    [TestMethod]
    public void Optimize_WhenParentDependsOnInnerRow_ShouldRecursivelyHoistStableOuterChild()
    {
        var outerRead = Field("a", "Value");
        var expression = new ExecutionBinary(
            BinaryOpKind.Add,
            outerRead,
            Field("b", "Value"),
            typeof(int));
        var outer = new ExecutionForEach(
            Var("a", typeof(object)),
            Rows("aRows"),
            new ExecutionBlock([
                new ExecutionForEach(
                    Var("b", typeof(object)),
                    Rows("bRows"),
                    new ExecutionBlock([
                        new ExecutionForEach(
                            Var("c", typeof(object)),
                            Rows("cRows"),
                            new ExecutionBlock([Append(expression)]))
                    ]))
            ]));

        var result = Optimize(outer);

        Assert.IsTrue(result.IsChanged);
        var rewrittenOuter = (ExecutionForEach)result.Plan.Body.Nodes[0];
        var aLet = (ExecutionLet)rewrittenOuter.Body.Nodes[0];
        var rewrittenB = (ExecutionForEach)rewrittenOuter.Body.Nodes[1];
        var bLet = (ExecutionLet)rewrittenB.Body.Nodes[0];
        var rewrittenC = (ExecutionForEach)rewrittenB.Body.Nodes[1];
        var append = (ExecutionAppendRow)rewrittenC.Body.Nodes[0];
        Assert.AreEqual("aValue", aLet.Variable.Name);
        Assert.AreEqual("expr", bLet.Variable.Name);
        Assert.AreSame(bLet.Variable, ((ExecutionVariableRead)append.Values[0].Value).Variable);
    }

    [TestMethod]
    public void Optimize_WhenFieldIsVolatile_ShouldLeaveRepeatedReadAtLeaf()
    {
        var volatileRead = Field("a", "Value") with { Stability = ColumnStability.Volatile };
        var outer = new ExecutionForEach(
            Var("a", typeof(object)),
            Rows("aRows"),
            new ExecutionBlock([
                new ExecutionForEach(
                    Var("b", typeof(object)),
                    Rows("bRows"),
                    new ExecutionBlock([Append(volatileRead)]))
            ]));

        var result = Optimize(outer);

        Assert.IsFalse(result.IsChanged);
        Assert.IsFalse(ExecutionIrAnalysis.FlattenNodes(result.Plan.Body).OfType<ExecutionLet>().Any());
        Assert.Contains("volatile or unknown", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenExpressionIsInConditionalOnlyArm_ShouldNotHoist()
    {
        var read = Field("a", "Value");
        var outer = new ExecutionForEach(
            Var("a", typeof(object)),
            Rows("aRows"),
            new ExecutionBlock([
                new ExecutionForEach(
                    Var("b", typeof(object)),
                    Rows("bRows"),
                    new ExecutionBlock([
                        new ExecutionIf(
                            new ExecutionLiteral(true, typeof(bool)),
                            new ExecutionBlock([Append(read)]))
                    ]))
            ]));

        var result = Optimize(outer);

        Assert.IsFalse(result.IsChanged);
        Assert.Contains("conditional", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenGuardedBodyContainsDescendantLoop_ShouldHoistInsideGuard()
    {
        var outer = new ExecutionForEach(
            Var("a", typeof(object)),
            Rows("aRows"),
            new ExecutionBlock([
                new ExecutionIf(
                    new ExecutionLiteral(true, typeof(bool)),
                    new ExecutionBlock([
                        new ExecutionForEach(
                            Var("b", typeof(object)),
                            Rows("bRows"),
                            new ExecutionBlock([Append(Field("a", "Value"))]))
                    ]))
            ]));

        var result = Optimize(outer);

        Assert.IsTrue(result.IsChanged);
        var rewrittenOuter = (ExecutionForEach)result.Plan.Body.Nodes[0];
        var guard = (ExecutionIf)rewrittenOuter.Body.Nodes[0];
        Assert.IsInstanceOfType<ExecutionLet>(guard.Body.Nodes[0]);
        Assert.IsInstanceOfType<ExecutionForEach>(guard.Body.Nodes[1]);
        var nested = (ExecutionForEach)guard.Body.Nodes[1];
        var value = ((ExecutionAppendRow)nested.Body.Nodes[0]).Values[0].Value;
        Assert.AreSame(((ExecutionLet)guard.Body.Nodes[0]).Variable, ((ExecutionVariableRead)value).Variable);
    }

    [TestMethod]
    public void Optimize_WhenCandidateIsAlsoUsedByNullExtension_ShouldReuseExistingOuterLocal()
    {
        var read = Field("a", "Value");
        var outer = new ExecutionForEach(
            Var("a", typeof(object)),
            Rows("aRows"),
            new ExecutionBlock([
                new ExecutionForEach(
                    Var("b", typeof(object)),
                    Rows("bRows"),
                    new ExecutionBlock([Append(read)])),
                new ExecutionIf(
                    new ExecutionLiteral(true, typeof(bool)),
                    new ExecutionBlock([Append(read)]))
            ]));

        var result = Optimize(outer);

        Assert.IsTrue(result.IsChanged);
        var rewrittenOuter = (ExecutionForEach)result.Plan.Body.Nodes[0];
        var let = (ExecutionLet)rewrittenOuter.Body.Nodes[0];
        var conditional = (ExecutionIf)rewrittenOuter.Body.Nodes[2];
        var conditionalValue = ((ExecutionAppendRow)conditional.Body.Nodes[0]).Values[0].Value;
        Assert.AreEqual("aValue", let.Variable.Name);
        Assert.AreSame(let.Variable, ((ExecutionVariableRead)conditionalValue).Variable);
    }

    [TestMethod]
    public void Optimize_WhenIndexedOrOrdinalLoopIsUsed_ShouldRecognizeLoopDependencies()
    {
        var ordinal = Var("ordinal", typeof(long));
        var indexed = new ExecutionForEachWithOrdinality(
            Var("a", typeof(object)),
            Rows("aRows"),
            ordinal,
            new ExecutionBlock([
                new ExecutionForEachIndexed(
                    Var("b", typeof(object)),
                    Var("index", typeof(int)),
                    Var("bRows", typeof(object)),
                    ExecutionRowAccessMode.Direct,
                    new ExecutionBlock([
                        Append(new ExecutionBinary(
                            BinaryOpKind.Add,
                            new ExecutionVariableRead(ordinal),
                            new ExecutionLiteral(1L, typeof(long)),
                            typeof(long)))
                    ])
                )
            ]));

        var result = Optimize(indexed);

        Assert.IsTrue(result.IsChanged);
        var outer = (ExecutionForEachWithOrdinality)result.Plan.Body.Nodes[0];
        Assert.IsInstanceOfType<ExecutionLet>(outer.Body.Nodes[0]);
        Assert.IsInstanceOfType<ExecutionForEachIndexed>(outer.Body.Nodes[1]);
    }

    [TestMethod]
    public void Optimize_WhenNameAlreadyExists_ShouldUseDeterministicCollisionSuffix()
    {
        var outer = new ExecutionForEach(
            Var("a", typeof(object)),
            Rows("aRows"),
            new ExecutionBlock([
                new ExecutionLet(Var("aValue", typeof(int)), new ExecutionLiteral(0, typeof(int))),
                new ExecutionForEach(
                    Var("b", typeof(object)),
                    Rows("bRows"),
                    new ExecutionBlock([Append(Field("a", "Value"))]))
            ]));

        var result = Optimize(outer);

        Assert.IsTrue(result.IsChanged);
        var rewrittenOuter = (ExecutionForEach)result.Plan.Body.Nodes[0];
        Assert.AreEqual("aValue1", ((ExecutionLet)rewrittenOuter.Body.Nodes[1]).Variable.Name);
    }

    [TestMethod]
    public void Optimize_WhenRunTwice_ShouldBeIdempotent()
    {
        var outer = new ExecutionForEach(
            Var("a", typeof(object)),
            Rows("aRows"),
            new ExecutionBlock([
                new ExecutionForEach(
                    Var("b", typeof(object)),
                    Rows("bRows"),
                    new ExecutionBlock([Append(Field("a", "Value"))]))
            ]));

        var first = Optimize(outer).Plan;
        var second = OptimizePlan(first);

        Assert.IsFalse(second.IsChanged);
        Assert.AreSame(first, second.Plan);
    }

    [TestMethod]
    public void Optimize_WhenDisabled_ShouldNotChangePlan()
    {
        var outer = new ExecutionForEach(
            Var("a", typeof(object)),
            Rows("aRows"),
            new ExecutionBlock([
                new ExecutionForEach(
                    Var("b", typeof(object)),
                    Rows("bRows"),
                    new ExecutionBlock([Append(Field("a", "Value"))]))
            ]));

        var result = Optimize(outer, enabled: false);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(outer, result.Plan.Body.Nodes[0]);
        StringAssert.Contains(result.Reason, "disabled");
    }

    private static OptimizationResult<ExecutionPlan> Optimize(
        ExecutionNode node,
        bool enabled = true)
    {
        return OptimizePlan(
            new ExecutionPlan("compiled", [], new ExecutionBlock([node])),
            enabled);
    }

    private static OptimizationResult<ExecutionPlan> OptimizePlan(
        ExecutionPlan plan,
        bool enabled = true)
    {
        return new LoopInvariantCodeMotionPass().Optimize(
            plan,
            new OptimizationContext(
                OptimizationStage.ExecutionIrOptimization,
                trace: null,
                new OptimizationOptions { LoopInvariantCodeMotionEnabled = enabled },
                OptimizationContextState.Empty));
    }

    private static ExecutionVariable Var(string name, Type type) => new(name, type);

    private static ExecutionExpression Rows(string name) =>
        new ExecutionRowStream(Var(name, typeof(object)), ExecutionRowStreamKind.Rows);

    private static ExecutionFieldRead Field(string alias, string name) =>
        new(alias, name, typeof(int));

    private static ExecutionAppendRow Append(params ExecutionExpression[] values)
    {
        var shape = new GeneratedRowShape(
            "Result",
            values.Select((value, index) => new FieldBinding(
                    $"Value{index}",
                    $"Value{index}",
                    index,
                    value.ReturnType.ResolveClrType(),
                    FieldNullability.Unknown,
                    new GeneratedFieldAccess($"Value{index}")))
                .ToArray());
        return new ExecutionAppendRow(
            Var("result", typeof(object)),
            shape,
            values.Select((value, index) => new ExecutionRowValue($"Value{index}", value)).ToArray());
    }
}
