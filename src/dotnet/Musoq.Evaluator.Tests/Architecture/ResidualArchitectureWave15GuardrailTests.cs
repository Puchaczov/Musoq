using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave15GuardrailTests
{
    [TestMethod]
    public void CodeGeneration_ShouldNotCallExecutionTypedSinkInternals()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/CodeGeneration",
            "*.cs");

        string[] forbiddenMarkers =
        [
            "EnterTypedSinkRendering",
            "EnterTypedSinkRenderContext",
            "EnterQueryRunContextRendering",
            "EnterQueryRunContextRenderContext",
            "RenderExpressionForTypedSink",
            "CreateTypedSinkEntryStatements",
            "RenderSourceScanForTypedSink",
            "RenderSetupNodeForTypedSink",
            "RenderGeneratedRowCreationForTypedSink",
            "RenderOptionalGeneratedRowProjectionForTypedSink"
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
            "IR.CodeGeneration should consume execution render artifact/final-sink APIs instead of typed-sink internals: " +
            string.Join(Environment.NewLine, offenders));
    }
}
