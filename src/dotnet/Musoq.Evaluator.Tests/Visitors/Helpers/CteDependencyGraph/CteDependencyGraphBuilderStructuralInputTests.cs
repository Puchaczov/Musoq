using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Tests.Visitors.Helpers.CteDependencyGraph;

[TestClass]
public sealed class CteDependencyGraphBuilderStructuralInputTests
{
    [TestMethod]
    public void Build_DirectCteArgument_ShouldMarkReferencedCteReachable()
    {
        var cteA = new CteInnerExpressionNode(new IntegerNode("1"), "patterns");
        var outer = new SchemaFromNode(
            "inputs",
            "match",
            new ArgsListNode([new IdentifierNode("patterns")]),
            "m",
            typeof(object),
            0);
        var graph = new CteDependencyGraphBuilder().Build(
            new CteExpressionNode([cteA], outer));

        Assert.IsTrue(graph.GetCte("patterns").IsReachable);
        Assert.IsFalse(graph.HasReachableDependencyCycle);
        Assert.Contains(CteGraphNode.OuterQueryNodeName, graph.GetCte("patterns").Dependents);
    }
    [TestMethod]
    public void Build_MutualCycleWithAcyclicDependencies_ShouldReportReachableCycle()
    {
        var baseRows = new CteInnerExpressionNode(new IntegerNode("1"), "baseRows");
        var left = new CteInnerExpressionNode(Reference("rightRows", "baseRows"), "leftRows");
        var right = new CteInnerExpressionNode(Reference("leftRows", "baseRows"), "rightRows");

        var graph = new CteDependencyGraphBuilder().Build(
            new CteExpressionNode([baseRows, left, right], Reference("leftRows")));

        Assert.IsTrue(graph.HasReachableDependencyCycle);
        Assert.AreEqual(0, graph.GetCte("baseRows").ExecutionLevel);
    }

    [TestMethod]
    public void Build_DeadMutualCycle_ShouldNotReportReachableCycle()
    {
        var left = new CteInnerExpressionNode(Reference("rightRows"), "leftRows");
        var right = new CteInnerExpressionNode(Reference("leftRows"), "rightRows");

        var graph = new CteDependencyGraphBuilder().Build(
            new CteExpressionNode([left, right], new IntegerNode("1")));

        Assert.IsFalse(graph.HasReachableDependencyCycle);
        Assert.HasCount(2, graph.DeadCtes);
    }

    private static SchemaFromNode Reference(params string[] names)
    {
        return new SchemaFromNode("inputs", "consume",
            new ArgsListNode(System.Array.ConvertAll(names, static name => (Node)new IdentifierNode(name))),
            "n", typeof(object), 0);
    }
}
