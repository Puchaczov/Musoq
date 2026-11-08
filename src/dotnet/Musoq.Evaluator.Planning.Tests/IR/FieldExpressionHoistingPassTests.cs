using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class FieldExpressionHoistingPassTests
{
    [TestMethod]
    public void Optimize_WhenAppendRowRepeatsFieldRead_ShouldInsertLetByDefault()
    {
        var read = new ExecutionFieldRead("a", "Name", typeof(string));
        var plan = CreatePlan(CreateAppendRow(read, read));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];
        var append = (ExecutionAppendRow)result.Plan.Body.Nodes[1];
        var first = (ExecutionVariableRead)append.Values[0].Value;
        var second = (ExecutionVariableRead)append.Values[1].Value;

        Assert.AreEqual("name", let.Variable.Name);
        Assert.AreEqual(read, let.Value);
        Assert.AreSame(let.Variable, first.Variable);
        Assert.AreSame(let.Variable, second.Variable);
    }

    [TestMethod]
    public void Optimize_WhenHoistCandidateIsPresent_ShouldLowerCandidateBeforeDiscovery()
    {
        var variable = new ExecutionVariable("name", typeof(string));
        var read = new ExecutionFieldRead("a", "Name", typeof(string));
        var plan = CreatePlan(new ExecutionHoistCandidateLet(
            variable,
            read,
            ExecutionHoistKind.FieldRead,
            ExecutionHoistScope.AppendValues,
            "field:a:Name"));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var let = (ExecutionLet)result.Plan.Body.Nodes[0];

        Assert.AreSame(variable, let.Variable);
        Assert.AreEqual(read, let.Value);
        Assert.Contains("Lowered 1 hoist candidate let(s)", result.Reason);
    }

    [TestMethod]
    public void Optimize_WhenNoSupportedRepeatedReadsExist_ShouldLeavePlanUnchanged()
    {
        var plan = CreatePlan(CreateAppendRow(
            new ExecutionFieldRead("a", "Name", typeof(string)),
            new ExecutionFieldRead("a", "City", typeof(string))));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenRepeatedFieldReadsAreInsideNestedBlock_ShouldHoistInsideNestedBlock()
    {
        var read = new ExecutionFieldRead("a", "Name", typeof(string));
        var plan = CreatePlan(new ExecutionIf(
            new ExecutionLiteral(true, typeof(bool)),
            new ExecutionBlock([CreateAppendRow(read, read)])));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var branch = (ExecutionIf)result.Plan.Body.Nodes[0];
        var let = (ExecutionLet)branch.Body.Nodes[0];
        var append = (ExecutionAppendRow)branch.Body.Nodes[1];
        var first = (ExecutionVariableRead)append.Values[0].Value;
        var second = (ExecutionVariableRead)append.Values[1].Value;

        Assert.AreEqual("name", let.Variable.Name);
        Assert.AreEqual(read, let.Value);
        Assert.AreSame(let.Variable, first.Variable);
        Assert.AreSame(let.Variable, second.Variable);
    }

    [TestMethod]
    public void Optimize_WhenRepeatedFieldReadsAreInsideLoopBody_ShouldHoistInsideLoopBody()
    {
        var read = new ExecutionFieldRead("a", "Name", typeof(string));
        var plan = CreatePlan(new ExecutionForEach(
            new ExecutionVariable("item", typeof(object)),
            new ExecutionRowStream(new ExecutionVariable("rows", typeof(object)), ExecutionRowStreamKind.Rows),
            new ExecutionBlock([CreateAppendRow(read, read)])));

        var result = Optimize(plan);

        Assert.IsTrue(result.IsChanged);
        var loop = (ExecutionForEach)result.Plan.Body.Nodes[0];
        var let = (ExecutionLet)loop.Body.Nodes[0];
        var append = (ExecutionAppendRow)loop.Body.Nodes[1];
        var first = (ExecutionVariableRead)append.Values[0].Value;
        var second = (ExecutionVariableRead)append.Values[1].Value;

        Assert.AreEqual("name", let.Variable.Name);
        Assert.AreEqual(read, let.Value);
        Assert.AreSame(let.Variable, first.Variable);
        Assert.AreSame(let.Variable, second.Variable);
    }

    [TestMethod]
    public void Optimize_WhenWindowHelperExpressionRepeatsFieldRead_ShouldNotHoistAcrossHelperBoundary()
    {
        var read = new ExecutionFieldRead("a", "Name", typeof(string));
        var node = new ExecutionComputeOffsetWindow(
            new ExecutionVariable("buffer", typeof(object)),
            new ExecutionVariable("item", typeof(object)),
            ExecutionRowAccessMode.Direct,
            read,
            [],
            read,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(null, typeof(string)),
            ExecutionOffsetWindowFunction.Lag,
            new ExecutionVariable("lagValues", typeof(object)));
        var plan = CreatePlan(node);

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenRepeatedFieldReadIsArrayAccessSource_ShouldNotHoistContainerRead()
    {
        var read = new ExecutionFieldRead("a", "Array", typeof(int));
        var plan = CreatePlan(CreateAppendRow(
            new ExecutionArrayAccess(read, new ExecutionLiteral(0, typeof(int)), typeof(int), typeof(int)),
            new ExecutionArrayAccess(read, new ExecutionLiteral(1, typeof(int)), typeof(int), typeof(int))));

        var result = Optimize(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
    }

    [TestMethod]
    public void Optimize_WhenFieldReadDiscoveryIsDisabled_ShouldLeaveRepeatedReadsUnchanged()
    {
        var read = new ExecutionFieldRead("a", "Name", typeof(string));
        var plan = CreatePlan(new ExecutionIf(
            new ExecutionLiteral(true, typeof(bool)),
            new ExecutionBlock([CreateAppendRow(read, read)])));

        var result = Optimize(plan, enableFieldReadDiscovery: false);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
        Assert.Contains("disabled by compilation options", result.Reason);
    }

    private static OptimizationResult<ExecutionPlan> Optimize(
        ExecutionPlan plan,
        bool? enableFieldReadDiscovery = null)
    {
        var options = enableFieldReadDiscovery == null
            ? OptimizationOptions.Default
            : new OptimizationOptions { FieldReadDiscoveryEnabled = enableFieldReadDiscovery.Value };

        return new FieldExpressionHoistingPass().Optimize(
            plan,
            new OptimizationContext(
                OptimizationStage.ExecutionIrOptimization,
                trace: null,
                options,
                OptimizationContextState.Empty));
    }

    private static ExecutionPlan CreatePlan(ExecutionNode node)
    {
        return new ExecutionPlan("compiled", [], new ExecutionBlock([node]));
    }

    private static ExecutionAppendRow CreateAppendRow(
        ExecutionExpression first,
        ExecutionExpression second)
    {
        return new ExecutionAppendRow(
            new ExecutionVariable("result", typeof(object)),
            CreateRowShape(),
            [
                new ExecutionRowValue("First", first),
                new ExecutionRowValue("Second", second)
            ]);
    }

    private static GeneratedRowShape CreateRowShape()
    {
        return new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("First", "First", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("First")),
                new FieldBinding("Second", "Second", 1, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Second"))
            ]);
    }
}
