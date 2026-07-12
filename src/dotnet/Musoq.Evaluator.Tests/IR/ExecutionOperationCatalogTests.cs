using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Targets.Execution;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionOperationCatalogTests
{
    [TestMethod]
    public void Catalog_ShouldRegisterEveryConcreteNodeAndExpressionExactlyOnce()
    {
        var assemblyTypes = typeof(ExecutionPlan).Assembly.GetTypes();
        var nodes = assemblyTypes
            .Where(static type => !type.IsAbstract && typeof(ExecutionNode).IsAssignableFrom(type))
            .ToArray();
        var expressions = assemblyTypes
            .Where(static type => !type.IsAbstract && typeof(ExecutionExpression).IsAssignableFrom(type))
            .ToArray();

        CollectionAssert.AreEquivalent(nodes, ExecutionOperationCatalog.RegisteredNodeTypes.ToArray());
        CollectionAssert.AreEquivalent(expressions, ExecutionOperationCatalog.RegisteredExpressionTypes.ToArray());
        Assert.HasCount(85, nodes);
        Assert.HasCount(37, expressions);
        Assert.HasCount(122, ExecutionOperationCatalog.AllOperationIds);
    }

    [TestMethod]
    public void Catalog_OperationIds_ShouldBeUniqueAndStableTokens()
    {
        var values = ExecutionOperationCatalog.AllOperationIds
            .Select(static operationId => operationId.Value)
            .ToArray();

        Assert.HasCount(values.Length, values.Distinct(StringComparer.Ordinal));
        Assert.IsTrue(values.All(static value =>
            value.Length > 0 && value.All(static character =>
                char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character is '.' or '-')));
    }

    [TestMethod]
    public void Analyze_WhenOperationsRepeat_ShouldReturnDeterministicOccurrenceCounts()
    {
        var variable = new ExecutionVariable("sum", typeof(int));
        var plan = new ExecutionPlan(
            "Q_Operations",
            [],
            new ExecutionBlock([
                new ExecutionLet(
                    variable,
                    new ExecutionBinary(
                        BinaryOpKind.Add,
                        new ExecutionLiteral(1, typeof(int)),
                        new ExecutionLiteral(2, typeof(int)),
                        typeof(int)))
            ]));

        var report = ExecutionTargetOperationAnalyzer.Analyze(plan);

        AssertUsage(report, "variable.let", 1);
        AssertUsage(report, "expr.binary", 1);
        AssertUsage(report, "expr.literal", 2);
        Assert.HasCount(3, report.Operations);
    }

    [TestMethod]
    public void CSharpClrCapabilities_ShouldExplicitlySupportEveryRegisteredOperation()
    {
        CollectionAssert.AreEquivalent(
            ExecutionOperationCatalog.AllOperationIds.ToArray(),
            ExecutionTargetCapabilities.CSharpClr.SupportedOperations.ToArray());
    }

    [TestMethod]
    public void Catalog_WhenNodeOrExpressionIsUnregistered_ShouldRejectDeterministically()
    {
        var nodeException = Assert.ThrowsExactly<NotSupportedException>(
            () => ExecutionOperationCatalog.Resolve(new TestUnregisteredNode()));
        var expressionException = Assert.ThrowsExactly<NotSupportedException>(
            () => ExecutionOperationCatalog.Resolve(new TestUnregisteredExpression()));

        StringAssert.Contains(nodeException.Message, typeof(TestUnregisteredNode).FullName!);
        StringAssert.Contains(expressionException.Message, typeof(TestUnregisteredExpression).FullName!);
    }

    private static void AssertUsage(ExecutionTargetOperationReport report, string operationId, int count)
    {
        var usage = report.Operations.Single(item => item.OperationId.Value == operationId);
        Assert.AreEqual(count, usage.OccurrenceCount);
    }

    private sealed record TestUnregisteredNode : ExecutionNode;

    private sealed record TestUnregisteredExpression() : ExecutionExpression(ExecutionTypeRef.FromClr(typeof(int)));
}
