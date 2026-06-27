using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PlanOptimizationRunnerTests
{
    [TestMethod]
    public void Run_WhenPassesAreOrdered_ShouldPassChangedPlanToNextPass()
    {
        var first = new AppendPass("First", "A");
        var second = new AppendPass("Second", "B");
        var trace = new OptimizationTrace();
        var context = new OptimizationContext(OptimizationStage.LogicalOptimization, trace);
        var runner = new PlanOptimizationRunner<string>(
            OptimizationStage.LogicalOptimization,
            [first, second]);

        var result = runner.Run("start", context);

        Assert.IsTrue(result.IsChanged);
        Assert.AreEqual("startAB", result.Plan);
        Assert.AreEqual("start", first.Inputs[0]);
        Assert.AreEqual("startA", second.Inputs[0]);
        Assert.HasCount(2, trace.Entries);
        Assert.AreEqual("First", trace.Entries[0].PassName);
        Assert.AreEqual("Second", trace.Entries[1].PassName);
    }

    [TestMethod]
    public void Run_WhenPassReturnsNoChange_ShouldPassSamePlanForward()
    {
        var plan = new ReferencePlan("initial");
        var noChange = new NoChangePass<ReferencePlan>("NoChange");
        var observer = new ObserveReferencePass("Observer");
        var runner = new PlanOptimizationRunner<ReferencePlan>(
            OptimizationStage.PhysicalOptimization,
            [noChange, observer]);

        var result = runner.Run(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreSame(plan, result.Plan);
        Assert.AreSame(plan, observer.ObservedPlan);
    }

    [TestMethod]
    public void Run_WhenContextStageDiffers_ShouldRecordRunnerStageInTrace()
    {
        var trace = new OptimizationTrace();
        var context = new OptimizationContext(OptimizationStage.LogicalOptimization, trace);
        var runner = new PlanOptimizationRunner<string>(
            OptimizationStage.ExecutionIrOptimization,
            [new AppendPass("Append", "x")]);

        runner.Run(string.Empty, context);

        Assert.HasCount(1, trace.Entries);
        Assert.AreEqual(OptimizationStage.ExecutionIrOptimization, trace.Entries[0].Stage);
    }

    [TestMethod]
    public void Run_WhenFixedPointStabilizes_ShouldStopAfterNoChangeIteration()
    {
        var trace = new OptimizationTrace();
        var runner = new PlanOptimizationRunner<string>(
            OptimizationStage.LogicalOptimization,
            [new AppendUntilLengthPass("AppendUntilTwo", 2)],
            OptimizationPassRunMode.FixedPoint,
            maxIterations: 8);

        var result = runner.Run(string.Empty, new OptimizationContext(OptimizationStage.LogicalOptimization, trace));

        Assert.IsTrue(result.IsChanged);
        Assert.AreEqual("xx", result.Plan);
        Assert.HasCount(3, trace.Entries);
        Assert.AreEqual(1, trace.Entries[0].Iteration);
        Assert.AreEqual(2, trace.Entries[1].Iteration);
        Assert.AreEqual(3, trace.Entries[2].Iteration);
        Assert.IsFalse(trace.Entries[2].IsChanged);
    }

    [TestMethod]
    public void Run_WhenFixedPointNeverStabilizes_ShouldStopAtMaxIterations()
    {
        var trace = new OptimizationTrace();
        var runner = new PlanOptimizationRunner<string>(
            OptimizationStage.PhysicalOptimization,
            [new AppendPass("AlwaysAppend", "x")],
            OptimizationPassRunMode.FixedPoint,
            maxIterations: 3);

        var result = runner.Run(string.Empty, new OptimizationContext(OptimizationStage.PhysicalOptimization, trace));

        Assert.IsTrue(result.IsChanged);
        Assert.AreEqual("xxx", result.Plan);
        Assert.HasCount(4, trace.Entries);
        Assert.AreEqual("FixedPoint", trace.Entries[^1].PassName);
        Assert.AreEqual("MaxIterationsReached", trace.Entries[^1].Outcome);
    }

    [TestMethod]
    public void Run_WhenNoPassesConfigured_ShouldReturnNoChangeWithoutTraceEntries()
    {
        var trace = new OptimizationTrace();
        var runner = new PlanOptimizationRunner<string>(
            OptimizationStage.CodegenReadability,
            []);

        var result = runner.Run("same", new OptimizationContext(OptimizationStage.CodegenReadability, trace));

        Assert.IsFalse(result.IsChanged);
        Assert.AreEqual("same", result.Plan);
        Assert.IsEmpty(trace.Entries);
    }

    [TestMethod]
    public void Run_WhenPassChangesPlan_ShouldInvalidateStaleAnalysisFacts()
    {
        var context = new OptimizationContext(OptimizationStage.LogicalOptimization);
        context.AnalysisFacts.Set("required-columns", "before");
        var observer = new ObserveFactPass("Observer", "required-columns");
        var runner = new PlanOptimizationRunner<string>(
            OptimizationStage.LogicalOptimization,
            [new AppendPass("Change", "x"), observer]);

        runner.Run(string.Empty, context);

        Assert.IsFalse(observer.Observed);
        Assert.IsFalse(context.AnalysisFacts.TryGet<string>("required-columns", out _));
        Assert.Contains(
            "analysis facts: consumed 0, recomputed 0, invalidated 1.",
            context.Trace.Entries[0].Reason);
    }

    [TestMethod]
    public void Run_WhenPassReturnsNoChange_ShouldPreserveAnalysisFacts()
    {
        var context = new OptimizationContext(OptimizationStage.LogicalOptimization);
        context.AnalysisFacts.Set("required-columns", "before");
        var observer = new ObserveFactPass("Observer", "required-columns");
        var runner = new PlanOptimizationRunner<string>(
            OptimizationStage.LogicalOptimization,
            [new NoChangePass<string>("NoChange"), observer]);

        runner.Run(string.Empty, context);

        Assert.IsTrue(observer.Observed);
        Assert.AreEqual("before", observer.Value);
        Assert.AreEqual("Observer", context.AnalysisFacts.Snapshot()[0].Consumers[0]);
        Assert.Contains(
            "analysis facts: consumed 1, recomputed 0, invalidated 0.",
            context.Trace.Entries[1].Reason);
    }

    [TestMethod]
    public void Run_WhenChangedPassRecomputesAnalysisFact_ShouldPreserveRecomputedFact()
    {
        var context = new OptimizationContext(OptimizationStage.LogicalOptimization);
        context.AnalysisFacts.Set("required-columns", "before");
        var observer = new ObserveFactPass("Observer", "required-columns");
        var runner = new PlanOptimizationRunner<string>(
            OptimizationStage.LogicalOptimization,
            [new SetFactAndChangePass("RewriteAndAnalyze", "required-columns", "after"), observer]);

        runner.Run(string.Empty, context);

        Assert.IsTrue(observer.Observed);
        Assert.AreEqual("after", observer.Value);
        var fact = context.AnalysisFacts.Snapshot()[0];
        Assert.AreEqual("RewriteAndAnalyze", fact.ProducedByPass);
        Assert.AreEqual(OptimizationAnalysisInvalidationRule.OnPlanChanged, fact.InvalidationRule);
        Assert.Contains(
            "analysis facts: consumed 0, recomputed 1, invalidated 0.",
            context.Trace.Entries[0].Reason);
    }

    [TestMethod]
    public void Run_WhenFactNeverInvalidates_ShouldPreserveItAcrossChangedPlan()
    {
        var context = new OptimizationContext(OptimizationStage.LogicalOptimization);
        context.AnalysisFacts.Set(
            "source-capabilities",
            "stable",
            OptimizationAnalysisInvalidationRule.Never);
        var observer = new ObserveFactPass("Observer", "source-capabilities");
        var runner = new PlanOptimizationRunner<string>(
            OptimizationStage.LogicalOptimization,
            [new AppendPass("Change", "x"), observer]);

        runner.Run(string.Empty, context);

        Assert.IsTrue(observer.Observed);
        Assert.AreEqual("stable", observer.Value);
    }

    private sealed record ReferencePlan(string Value);

    private sealed class AppendPass(string name, string suffix) : IPlanOptimizationPass<string>
    {
        public List<string> Inputs { get; } = [];

        public string Name { get; } = name;

        public OptimizationResult<string> Optimize(string plan, OptimizationContext context)
        {
            Inputs.Add(plan);
            return OptimizationResult<string>.Changed(plan + suffix, $"Appended {suffix}.");
        }
    }

    private sealed class AppendUntilLengthPass(string name, int targetLength) : IPlanOptimizationPass<string>
    {
        public string Name { get; } = name;

        public OptimizationResult<string> Optimize(string plan, OptimizationContext context)
        {
            return plan.Length >= targetLength
                ? OptimizationResult<string>.NoChange(plan, "Target length reached.")
                : OptimizationResult<string>.Changed(plan + "x", "Appended one character.");
        }
    }

    private sealed class NoChangePass<TPlan>(string name) : IPlanOptimizationPass<TPlan>
    {
        public string Name { get; } = name;

        public OptimizationResult<TPlan> Optimize(TPlan plan, OptimizationContext context)
        {
            return OptimizationResult<TPlan>.NoChange(plan, "No change requested.");
        }
    }

    private sealed class ObserveReferencePass(string name) : IPlanOptimizationPass<ReferencePlan>
    {
        public string Name { get; } = name;

        public ReferencePlan? ObservedPlan { get; private set; }

        public OptimizationResult<ReferencePlan> Optimize(ReferencePlan plan, OptimizationContext context)
        {
            ObservedPlan = plan;
            return OptimizationResult<ReferencePlan>.NoChange(plan, "Observed plan.");
        }
    }

    private sealed class ObserveFactPass(string name, string factKey) : IPlanOptimizationPass<string>
    {
        public string Name { get; } = name;

        public bool Observed { get; private set; }

        public string? Value { get; private set; }

        public OptimizationResult<string> Optimize(string plan, OptimizationContext context)
        {
            Observed = context.AnalysisFacts.TryConsume<string>(factKey, Name, out var value);
            Value = value;
            return OptimizationResult<string>.NoChange(plan, "Observed analysis fact.");
        }
    }

    private sealed class SetFactAndChangePass(string name, string factKey, string factValue) : IPlanOptimizationPass<string>
    {
        public string Name { get; } = name;

        public OptimizationResult<string> Optimize(string plan, OptimizationContext context)
        {
            context.AnalysisFacts.Set(factKey, factValue);
            return OptimizationResult<string>.Changed(plan + "x", "Recomputed analysis fact for changed plan.");
        }
    }
}
