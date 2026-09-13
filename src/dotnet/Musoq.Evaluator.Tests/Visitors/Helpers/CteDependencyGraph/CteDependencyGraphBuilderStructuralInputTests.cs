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
        Assert.Contains(CteGraphNode.OuterQueryNodeName, graph.GetCte("patterns").Dependents);
    }
}
