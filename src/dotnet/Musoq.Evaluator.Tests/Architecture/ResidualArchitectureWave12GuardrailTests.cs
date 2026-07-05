using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave12GuardrailTests
{
    [TestMethod]
    public void SemanticVisitorQueryPaths_ShouldUseTraversalFrameInsteadOfRawNodeStack()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan
            .FilesUnder(
                repositoryRoot,
                "src/dotnet/Musoq.Evaluator/Visitors",
                "BuildMetadataAndInferTypesVisitor*.cs")
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
            "SafePopMultiple(Nodes"
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
            "Semantic visitor query paths should route node stack operations through SemanticTraversalFrame helpers: " +
            string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void SelectAliasCloneVisitor_ShouldRemainOutsideSemanticStackRatchet()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var file = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "Visitors", "BuildMetadataAndInferTypesVisitor.SelectAliasBinding.cs");
        var text = File.ReadAllText(file);

        Assert.Contains("private sealed class SelectAliasCloneVisitor : CloneQueryVisitor", text);
        Assert.Contains("public Node ClonedNode => Nodes.Peek();", text);
    }
}
