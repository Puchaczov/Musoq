using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void ProductionSourceFiles_ShouldNotBeEmptyPlaceholders()
    {
        var repositoryRoot = FindRepositoryRoot();
        var offenders = EnumerateProductionSourceFiles(repositoryRoot)
            .Select(file => new SourceFileBudget(
                Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'),
                CountBudgetedLines(file)))
            .Where(static file => !Path.GetFileName(file.FileName)
                .Equals("GlobalUsings.cs", StringComparison.Ordinal))
            .Where(static file => file.LineCount <= 2)
            .Select(static file => $"{file.FileName}: {file.LineCount} line(s)")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Production files should carry real implementation, not placeholder namespaces: " +
            string.Join(", ", offenders));
    }
}
