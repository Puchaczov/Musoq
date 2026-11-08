using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void RecursiveRendererPartials_ShouldStayWithinExactPostRefactorNonBlankBudget()
    {
        var directory = ToAbsolutePath(
            FindRepositoryRoot(),
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution");
        var files = Directory
            .EnumerateFiles(directory, "ExecutionCSharpRenderer.RecursiveCte.*.cs")
            .Order(StringComparer.Ordinal)
            .ToArray();
        var lineCount = files.Sum(CountNonBlankLines);

        Assert.HasCount(3, files);
        Assert.IsLessThanOrEqualTo(
            360,
            lineCount,
            $"Recursive renderer partials grew to {lineCount}/360 nonblank lines: " +
            string.Join(", ", files.Select(Path.GetFileName)));
    }

    private static int CountNonBlankLines(string filePath) =>
        File.ReadLines(filePath).Count(static line => !string.IsNullOrWhiteSpace(line));
}
