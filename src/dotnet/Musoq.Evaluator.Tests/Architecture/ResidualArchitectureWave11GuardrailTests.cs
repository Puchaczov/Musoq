using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave11GuardrailTests
{
    [TestMethod]
    public void ExpressionSemanticVisitorPaths_ShouldUseTraversalFrameInsteadOfRawNodeStack()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        string[] targetPatterns =
        [
            "BuildMetadataAndInferTypesVisitor.Expression*.cs",
            "BuildMetadataAndInferTypesVisitor.CastBindingVisitor.cs",
            "BuildMetadataAndInferTypesVisitor.ColumnAccess*.cs",
            "BuildMetadataAndInferTypesVisitor.Method*.cs"
        ];

        var files = targetPatterns
            .SelectMany(pattern => RepositorySourceScan.FilesUnder(
                repositoryRoot,
                "src/dotnet/Musoq.Evaluator/Visitors",
                pattern))
            .Distinct(StringComparer.OrdinalIgnoreCase)
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
            "Expression semantic visitor paths should use SemanticTraversalFrame helpers instead of raw Nodes stack operations: " +
            string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void ExpressionSemanticVisitorPaths_ShouldUseSemanticNodeResultApplyHelpers()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var visitorText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "Visitors", "BuildMetadataAndInferTypesVisitor.cs"));
        var literalsText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "Visitors", "BuildMetadataAndInferTypesVisitor.ExpressionLiterals.cs"));
        var operatorsText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "Visitors", "BuildMetadataAndInferTypesVisitor.ExpressionOperators.cs"));

        Assert.Contains("private void PushSemanticNode", visitorText);
        Assert.Contains("private Node PopSemanticNode", visitorText);
        Assert.Contains("private Node[] PopSemanticNodes", visitorText);
        Assert.Contains("PushSemanticNode(new StringNode", literalsText);
        Assert.Contains("PopSemanticNodes(2", operatorsText);
    }
}
