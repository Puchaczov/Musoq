using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave10GuardrailTests
{
    [TestMethod]
    public void SemanticTraversalFoundation_ShouldUseTypedFrameAndNodeResult()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var visitorsDirectory = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "Visitors");

        Assert.IsTrue(File.Exists(Path.Combine(visitorsDirectory, "Semantics", "SemanticTraversalFrame.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(visitorsDirectory, "Semantics", "SemanticNodeResult.cs")));

        var stateText = File.ReadAllText(Path.Combine(visitorsDirectory, "SemanticAnalysisState.cs"));
        var visitorText = File.ReadAllText(Path.Combine(visitorsDirectory, "BuildMetadataAndInferTypesVisitor.cs"));
        var queryNodesText = File.ReadAllText(Path.Combine(visitorsDirectory, "BuildMetadataAndInferTypesVisitor.QueryState.QueryNodes.cs"));
        var setOperatorsText = File.ReadAllText(Path.Combine(visitorsDirectory, "BuildMetadataAndInferTypesVisitor.SetOperators.cs"));

        Assert.Contains("public SemanticTraversalFrame Traversal", stateText);
        Assert.Contains("private readonly Stack<Node> _nodes", stateText);
        Assert.Contains("private readonly Stack<string> _methods", stateText);
        Assert.Contains("new SemanticTraversalFrame(_nodes, _methods)", stateText);
        Assert.Contains("private SemanticTraversalFrame TraversalFrame", visitorText);
        Assert.Contains("Root => TraversalFrame.PeekNode<RootNode>", visitorText);
        Assert.Contains("SemanticNodeResult", visitorText);
        Assert.Contains("TraversalFrame.PushMethod", queryNodesText);
        Assert.Contains("TraversalFrame.PopMethod", setOperatorsText);
        Assert.DoesNotContain("Methods =>", visitorText);
        Assert.DoesNotContain("Methods.Push", setOperatorsText);
        Assert.DoesNotContain("Methods.Pop", setOperatorsText);
    }
}
