using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
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
        Assert.HasCount(89, nodes);
        Assert.HasCount(38, expressions);
        Assert.HasCount(127, ExecutionOperationCatalog.AllOperationIds);
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
    public void NodeDefinitionInventory_ShouldHaveUniqueTypesAndOperationIds()
    {
        var definitions = ExecutionNodeDefinitionCatalog.Definitions;

        Assert.HasCount(definitions.Count, definitions.Select(static definition => definition.NodeType).Distinct().ToArray());
        Assert.HasCount(definitions.Count, definitions.Select(static definition => definition.OperationId).Distinct().ToArray());
        CollectionAssert.AreEqual(
            definitions.Select(static definition => definition.NodeType).ToArray(),
            ExecutionNodeRegistry.Descriptors.Select(static descriptor => descriptor.NodeType).ToArray());
    }

    [TestMethod]
    public void NodeDefinitionInventory_ShouldCoverEveryConcreteExecutionNode()
    {
        var concreteNodes = typeof(ExecutionPlan).Assembly.GetTypes()
            .Where(static type => !type.IsAbstract && typeof(ExecutionNode).IsAssignableFrom(type))
            .ToArray();
        var definedNodes = ExecutionNodeDefinitionCatalog.Definitions
            .Select(static definition => definition.NodeType)
            .ToArray();

        Assert.IsEmpty(
            concreteNodes.Except(definedNodes).ToArray(),
            "Concrete execution nodes missing from the authoritative node-definition inventory.");
        Assert.IsEmpty(
            definedNodes.Except(concreteNodes).ToArray(),
            "Node-definition inventory contains a type that is not a concrete execution node.");
    }

    [TestMethod]
    public void NodeDefinitionInventory_ShouldRegisterDeterministically()
    {
        var first = ExecutionNodeRegistry.Descriptors
            .Select(static descriptor => descriptor.NodeType.FullName)
            .ToArray();
        var second = ExecutionNodeRegistry.Descriptors
            .Select(static descriptor => descriptor.NodeType.FullName)
            .ToArray();

        CollectionAssert.AreEqual(first, second);
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
    public void CSharpClrCapabilities_ShouldMatchNodeTargetCapabilityMetadata()
    {
        CollectionAssert.AreEquivalent(
            ExecutionOperationCatalog.CSharpClrSupportedOperationIds.ToArray(),
            ExecutionTargetCapabilities.CSharpClr.SupportedOperations.ToArray());
    }

    [TestMethod]
    public void NodeDefinitionInventory_ShouldRequireRegisteredPrinterRewriterAndCapabilityMetadata()
    {
        var definitions = ExecutionNodeDefinitionCatalog.Definitions;

        Assert.IsTrue(definitions.All(static definition =>
            definition.Behavior.Printer is not null &&
            definition.Behavior.Rewriter is not null));
        Assert.IsTrue(definitions.All(static definition =>
            definition.Behavior.TargetCapability is
                ExecutionNodeTargetCapability.Supported or ExecutionNodeTargetCapability.Unsupported));
    }

    [TestMethod]
    public void NodeDefinitionInventory_ShouldExposeExecutableBehaviorRegistrations()
    {
        var behaviorType = typeof(ExecutionNodeDefinition).GetProperty(nameof(ExecutionNodeDefinition.Behavior))!.PropertyType;

        CollectionAssert.AreEquivalent(
            new[] { "Printer", "Rewriter", "TargetCapability" },
            behaviorType.GetProperties().Select(static property => property.Name).ToArray());
        Assert.IsTrue(ExecutionNodeDefinitionCatalog.Definitions.All(static definition =>
            definition.Behavior.Printer.Method != null &&
            definition.Behavior.Rewriter.Method != null));
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

    private sealed record TestUnregisteredExpression() : ExecutionExpression(ExecutionClrBindingFactory.FromClr(typeof(int)));
}
