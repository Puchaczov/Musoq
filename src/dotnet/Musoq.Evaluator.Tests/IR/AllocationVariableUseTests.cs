using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class AllocationVariableUseTests
{
    public static IEnumerable<object[]> NodeCases => AllocationTraversalFixtures.NodeCases();
    public static IEnumerable<object[]> ExpressionCases => AllocationTraversalFixtures.ExpressionCases();

    [TestMethod]
    [DynamicData(nameof(NodeCases))]
    public void DirectReferences_ShouldMatchPreviousFacts(Type type, bool includeOptional)
    {
        var node = (ExecutionNode)new AllocationTraversalFixtures(includeOptional).Create(type);
        var expected = PreviousExecutionNodeFacts.GetDirectVariableReferences(node).ToArray();

        var actual = ExecutionNodeFacts.GetDirectVariableReferences(node).ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DynamicData(nameof(NodeCases))]
    public void NodeVariableUse_ShouldMatchPreviousAnalysis(Type type, bool includeOptional)
    {
        var node = (ExecutionNode)new AllocationTraversalFixtures(includeOptional).Create(type);
        var names = PreviousExecutionNodeFacts.GetDirectVariableReferences(node).Select(variable => variable.Name)
            .Concat(PreviousExecutionIrAnalysis.FlattenExpressions(new ExecutionBlock([node])).SelectMany(VariableNames))
            .Append("absent").Distinct(StringComparer.Ordinal).ToArray();
        var expected = names.Select(name => PreviousExecutionIrAnalysis.ContainsVariableUse(node, name)).ToArray();

        var actual = names.Select(name => ExecutionIrAnalysis.ContainsVariableUse(node, name)).ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DynamicData(nameof(ExpressionCases))]
    public void ExpressionVariableUse_ShouldMatchPreviousAnalysis(Type type, bool includeOptional)
    {
        var expression = (ExecutionExpression)new AllocationTraversalFixtures(includeOptional).Create(type);
        var names = PreviousExecutionIrAnalysis.FlattenExpressions(expression).SelectMany(VariableNames).Append("absent").Distinct().ToArray();
        var expected = names.Select(name => PreviousExecutionIrAnalysis.ContainsVariableUse(expression, name)).ToArray();

        var actual = names.Select(name => ExecutionIrAnalysis.ContainsVariableUse(expression, name)).ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void VariableNameComparison_ShouldRemainOrdinal()
    {
        var expression = new ExecutionVariableRead(new ExecutionVariable("CaseSensitive", typeof(int)));

        var actual = ExecutionIrAnalysis.ContainsVariableUse(expression, "casesensitive");

        Assert.IsFalse(actual);
    }

    [TestMethod]
    public void FirstMatchingExpression_ShouldNotEnumerateFollowingExpressions()
    {
        var expressions = MatchBeforeThrow();

        var actual = ExecutionIrAnalysis.ContainsVariableUse(expressions, "found");

        Assert.IsTrue(actual);
    }

    private static IEnumerable<ExecutionExpression> MatchBeforeThrow()
    {
        yield return new ExecutionVariableRead(new ExecutionVariable("found", typeof(int)));
        throw new InvalidOperationException("Variable-use analysis enumerated beyond its first match.");
    }

    private static IEnumerable<string> VariableNames(ExecutionExpression expression) => expression.GetType()
        .GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(property => property.PropertyType == typeof(ExecutionVariable))
        .Select(property => property.GetValue(expression)).OfType<ExecutionVariable>().Select(variable => variable.Name);
}
