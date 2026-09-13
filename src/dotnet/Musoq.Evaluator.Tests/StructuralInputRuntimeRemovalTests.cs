using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Architecture;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class StructuralInputRuntimeRemovalTests
{
    [TestMethod]
    public void StructuralPreparationProductionFiles_ShouldNotReferenceObsoleteRuntimeConverters()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var runtimeFile = Path.Combine(
            root,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "Helpers",
            "StructuralInputRuntime.cs");
        Assert.IsFalse(File.Exists(runtimeFile), runtimeFile);

        string[] files =
        [
            Path.Combine(root, "src", "dotnet", "Musoq.Targets.CSharpClr", "Rendering", "Execution", "ExecutionCSharpRenderer.StructuralInputs.cs"),
            Path.Combine(root, "src", "dotnet", "Musoq.Targets.CSharpClr", "Rendering", "Execution", "StructuralCtePreparationMembers.cs")
        ];
        var violations = files
            .Where(File.Exists)
            .SelectMany(file => File.ReadAllText(file)
                .Split('\n')
                .Select((line, index) => (file, line, index: index + 1)))
            .Where(static item => item.line.Contains("StructuralInputRuntime", StringComparison.Ordinal) ||
                                  item.line.Contains("PrepareRows", StringComparison.Ordinal) ||
                                  item.line.Contains("Array.GetValue", StringComparison.Ordinal) ||
                                  item.line.Contains("CreateDelegate", StringComparison.Ordinal))
            .Select(static item => $"{item.file}:{item.index}: {item.line.Trim()}")
            .ToArray();

        Assert.IsEmpty(violations, string.Join(Environment.NewLine, violations));
    }

    [TestMethod]
    public void GeneratedCaptureRuntime_ShouldNotReflectOverCollectionEntries()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var runtimeFile = Path.Combine(
            root,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "Helpers",
            "StructuralParameterCaptureRuntime.cs");
        var source = File.ReadAllText(runtimeFile);
        string[] forbidden =
        [
            "GetRecordFields",
            "GetProperty(\"Key\"",
            "GetProperty(\"Value\"",
            "PropertyInfo.GetValue",
            "CreateDelegate",
            "Array.GetValue"
        ];

        foreach (var marker in forbidden)
            Assert.DoesNotContain(marker, source, marker);
    }
}
