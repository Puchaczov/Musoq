using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave13GuardrailTests
{
    [TestMethod]
    public void SemanticStacks_ShouldBeOwnedBySemanticAnalysisStateAndFrame()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var visitorsDirectory = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "Visitors");

        var stateText = File.ReadAllText(Path.Combine(visitorsDirectory, "SemanticAnalysisState.cs"));
        var frameText = File.ReadAllText(Path.Combine(visitorsDirectory, "Semantics", "SemanticTraversalFrame.cs"));
        var visitorText = File.ReadAllText(Path.Combine(visitorsDirectory, "BuildMetadataAndInferTypesVisitor.cs"));

        Assert.Contains("private readonly Stack<Node> _nodes", stateText);
        Assert.Contains("private readonly Stack<string> _methods", stateText);
        Assert.Contains("new SemanticTraversalFrame(_nodes, _methods)", stateText);
        Assert.DoesNotContain("public Stack<Node> Nodes", stateText);
        Assert.DoesNotContain("public Stack<string> Methods", stateText);
        Assert.DoesNotContain("public Stack<Node> Nodes", frameText);
        Assert.DoesNotContain("public Stack<string> Methods", frameText);
        Assert.DoesNotContain("protected Stack<Node> Nodes", visitorText);
    }

    [TestMethod]
    public void SemanticVisitorPartials_ShouldNotMutateStacksDirectly()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan
            .FilesUnder(
                repositoryRoot,
                "src/dotnet/Musoq.Evaluator/Visitors",
                "*.cs")
            .Where(file => File
                .ReadAllText(file)
                .Contains("partial class BuildMetadataAndInferTypesVisitor", StringComparison.Ordinal))
            .Where(file => !file.EndsWith(
                "BuildMetadataAndInferTypesVisitor.SelectAliasBinding.cs",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        string[] forbiddenMarkers =
        [
            "Nodes.Push(",
            "Nodes.Pop(",
            "Nodes.Peek(",
            "SafePop(Nodes",
            "SafePeek(Nodes",
            "SafePopMultiple(Nodes",
            "Methods.Push(",
            "Methods.Pop(",
            "Methods.Peek(",
            "SafePop(Methods",
            "SafePeek(Methods",
            "SafePopMultiple(Methods"
        ];

        var offenders = files
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(item => forbiddenMarkers.Any(marker => item.Text.Contains(marker, StringComparison.Ordinal)))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Semantic visitor partials should use SemanticTraversalFrame helpers instead of raw stack operations: " +
            string.Join(Environment.NewLine, offenders));
    }
}
