using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins.Attributes;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExpressionStabilityAnalyzerTests
{
    [TestMethod]
    public void StableColumnAndCompositionAreStable()
    {
        var column = new ColumnRef("a", "Value", typeof(int));
        var expression = new BinaryOp(
            BinaryOpKind.Add,
            column,
            new Literal(1, typeof(int)),
            typeof(int));

        Assert.IsTrue(ExpressionStabilityAnalyzer.IsStable(column));
        Assert.IsTrue(ExpressionStabilityAnalyzer.IsStable(expression));
        Assert.IsTrue(IrExpressionDeterminism.IsDeterministic(expression));
    }

    [TestMethod]
    public void VolatileColumnTaintsCompositionAndCseFacts()
    {
        var column = new ColumnRef("a", "Value", typeof(int))
        {
            Stability = ColumnStability.Volatile
        };
        var expression = new BinaryOp(
            BinaryOpKind.Add,
            column,
            new Literal(1, typeof(int)),
            typeof(int));

        Assert.IsFalse(ExpressionStabilityAnalyzer.IsStable(expression));
        Assert.IsFalse(IrExpressionDeterminism.IsDeterministic(expression));
        Assert.IsTrue(IrExpressionDeterminism.TryGetFirstBlockedReason(expression, out var reason));
        StringAssert.Contains(reason, "volatile column");
    }

    [TestMethod]
    public void NonDeterministicMethodAndInjectedContextAreUnstable()
    {
        var volatileCall = new MethodCall(
            typeof(StabilityFixture).GetMethod(nameof(StabilityFixture.Volatile), BindingFlags.Public | BindingFlags.Static)!,
            [],
            null,
            typeof(int));
        var injectedCall = new MethodCall(
            typeof(StabilityFixture).GetMethod(nameof(StabilityFixture.WithStats), BindingFlags.Public | BindingFlags.Static)!,
            [],
            null,
            typeof(int));

        Assert.IsFalse(ExpressionStabilityAnalyzer.IsStable(volatileCall));
        Assert.IsFalse(ExpressionStabilityAnalyzer.IsStable(injectedCall));
    }

    private static class StabilityFixture
    {
        [NonDeterministic]
        public static int Volatile() => 1;

        public static int WithStats([InjectQueryStats] object _) => 1;
    }
}
