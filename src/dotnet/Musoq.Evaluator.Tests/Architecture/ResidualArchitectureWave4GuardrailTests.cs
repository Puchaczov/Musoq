using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave4GuardrailTests
{
    [TestMethod]
    public void CodeGeneration_ShouldUseContextReturningTypedSinkScopes()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var codegenFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/CodeGeneration",
            "CSharpRenderer*.cs");
        var offenders = codegenFiles
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(item =>
                item.Text.Contains("EnterTypedSinkRendering(", StringComparison.Ordinal) ||
                item.Text.Contains("EnterQueryRunContextRendering(", StringComparison.Ordinal))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Code generation should use context-returning render scopes so typed-sink/final-shape rendering can move off ambient state: " +
            string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void FinalShapeRowsRenderer_ShouldNotEnterAmbientQueryRunContextScope()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var finalShapeRowsFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "FinalShape", "ExecutionCSharpRenderer.FinalShapeRows.cs");
        var text = File.ReadAllText(finalShapeRowsFile);

        Assert.IsFalse(
            text.Contains("EnterQueryRunContextRendering(", StringComparison.Ordinal),
            "Final-shape rows rendering should mutate the active ExecutionRenderContext session directly instead of entering an ambient query-run-context scope.");
    }
}
