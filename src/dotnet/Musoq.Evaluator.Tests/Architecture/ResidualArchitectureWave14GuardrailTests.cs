using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave14GuardrailTests
{
    [TestMethod]
    public void CodeGenerationFinalSinkSetup_ShouldUseExecutionRenderArtifacts()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var finalSinkSetupPath = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "CodeGeneration", "CSharpRenderer.FinalSinkSetup.cs");
        var artifactPath = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "ExecutionRenderArtifacts.cs");
        var typedSinkRenderingPath = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "TypedSinkRendering.cs");

        var finalSinkSetupText = File.ReadAllText(finalSinkSetupPath);
        var typedSinkRenderingText = File.ReadAllText(typedSinkRenderingPath);

        Assert.IsTrue(File.Exists(artifactPath));
        Assert.Contains("CreateTypedSinkSetupArtifacts", typedSinkRenderingText);
        Assert.Contains("CreateTypedSinkSetupArtifacts", finalSinkSetupText);

        string[] forbiddenMarkers =
        [
            "EnterTypedSinkRenderContext",
            "CreateTypedSinkEntryStatements",
            "RenderSourceScanForTypedSink",
            "RenderSetupNodeForTypedSink"
        ];

        var offenders = File
            .ReadLines(finalSinkSetupPath)
            .Select((line, index) => new
            {
                Line = index + 1,
                Text = line.Trim()
            })
            .Where(item => forbiddenMarkers.Any(marker => item.Text.Contains(marker, StringComparison.Ordinal)))
            .Select(item => $"{RepositorySourceScan.ToRelative(repositoryRoot, finalSinkSetupPath)}:{item.Line}: {item.Text}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Code generation final-sink setup should consume execution render artifacts instead of typed-sink setup primitives: " +
            string.Join(Environment.NewLine, offenders));
    }
}
