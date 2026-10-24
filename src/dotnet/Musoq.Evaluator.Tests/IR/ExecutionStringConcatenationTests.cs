using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionStringConcatenationTests
{
    [TestMethod]
    public void Chain_ShouldFlattenAndOwnItsOperandsBeforeCse()
    {
        var input = new ExecutionExpression[] { Literal("a"), new ExecutionStringConcatenation([Literal("b"), Literal("c")]) };
        var expression = new ExecutionStringConcatenation(input);
        input[0] = Literal("changed");

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, expression.Expressions.Cast<ExecutionLiteral>()
            .Select(value => (string)value.Value.ToClrValue()!).ToArray());
        Assert.HasCount(3, ExecutionIrAnalysis.GetChildExpressions(expression).ToArray());
        Assert.AreEqual("expr.string-concatenation", ExecutionOperationCatalog.Resolve(expression).Value);
        StringAssert.Contains(ExecutionExpressionFingerprint.ForHoist(expression), "sql-concat:");
    }

    [TestMethod]
    public void StableChain_ShouldRemainParallelAndCseEligible()
    {
        var expression = new ExecutionStringConcatenation([Literal("a"), Literal("b")]);

        Assert.IsTrue(ExpressionStabilityAnalyzer.IsStable(expression));
        Assert.IsTrue(ParallelExecutionEligibilityRules.CanUseFilterProjectExpression(expression).IsEligible);
        Assert.IsTrue(ExecutionExpressionCseFacts.IsWorthHoistingExpression(expression));
    }

    [TestMethod]
    public void VolatileChain_ShouldRemainUnstableAndIneligibleForParallelExecution()
    {
        var input = new ExecutionFieldRead("e", "Name", typeof(string)) { Stability = ColumnStability.Volatile };
        var expression = new ExecutionStringConcatenation([input, Literal("b")]);

        Assert.IsFalse(ExpressionStabilityAnalyzer.IsStable(expression));
        Assert.IsFalse(ParallelExecutionEligibilityRules.CanUseFilterProjectExpression(expression).IsEligible);
    }

    private static ExecutionLiteral Literal(string value) => new(value, typeof(string));
}
