using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void ProductionCode_ShouldNotAddNewNotImplementedExceptionSites()
    {
        var repositoryRoot = FindRepositoryRoot();
        var files = EnumerateProductionSourceFiles(repositoryRoot);

        var offenders = files
            .Select(file =>
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/');
                var occurrences = CountOccurrences(file, "NotImplementedException");
                ProductionNotImplementedExceptionBudgets.TryGetValue(relativePath, out var budget);

                return new SourceFileBudget(relativePath, occurrences, budget);
            })
            .Where(file => file.LineCount > file.Budget)
            .Select(file => $"{file.FileName}: {file.LineCount}/{file.Budget}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Production NotImplementedException sites must be removed or explicitly budgeted: " +
            string.Join(", ", offenders));

        var staleBudgets = ProductionNotImplementedExceptionBudgets
            .Select(entry => new SourceFileBudget(
                entry.Key,
                CountOccurrences(ToAbsolutePath(repositoryRoot, entry.Key), "NotImplementedException"),
                entry.Value))
            .Where(file => file.LineCount < file.Budget)
            .Select(file => $"{file.FileName}: {file.LineCount}/{file.Budget}")
            .ToArray();

        Assert.IsEmpty(
            staleBudgets,
            "Remove stale NotImplementedException budgets once sites are cleaned up: " +
            string.Join(", ", staleBudgets));
    }

    [TestMethod]
    public void ProductionCode_ShouldClassifyHighCountNotSupportedExceptionSites()
    {
        var repositoryRoot = FindRepositoryRoot();
        var files = EnumerateProductionSourceFiles(repositoryRoot);

        var offenders = files
            .Select(file =>
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/');
                var occurrences = CountOccurrences(file, "NotSupportedException");
                ProductionHighCountNotSupportedExceptionBudgets.TryGetValue(relativePath, out var budget);

                return new SourceFileBudget(relativePath, occurrences, Math.Max(budget, HighCountNotSupportedExceptionThreshold - 1));
            })
            .Where(file => file.LineCount > file.Budget)
            .Select(file => $"{file.FileName}: {file.LineCount}/{file.Budget}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Production files with high NotSupportedException counts must be classified explicitly: " +
            string.Join(", ", offenders));

        var staleBudgets = ProductionHighCountNotSupportedExceptionBudgets
            .Select(entry => new SourceFileBudget(
                entry.Key,
                CountOccurrences(ToAbsolutePath(repositoryRoot, entry.Key), "NotSupportedException"),
                entry.Value))
            .Where(file => file.LineCount < file.Budget || file.LineCount < HighCountNotSupportedExceptionThreshold)
            .Select(file => $"{file.FileName}: {file.LineCount}/{file.Budget}")
            .ToArray();

        Assert.IsEmpty(
            staleBudgets,
            "Remove stale high-count NotSupportedException budgets once sites are cleaned up: " +
            string.Join(", ", staleBudgets));

        var missingRationales = ProductionHighCountNotSupportedExceptionBudgets
            .Keys
            .Where(path =>
                !ProductionHighCountNotSupportedExceptionRationales.TryGetValue(path, out var rationale) ||
                string.IsNullOrWhiteSpace(rationale))
            .ToArray();

        Assert.IsEmpty(
            missingRationales,
            "Every high-count NotSupportedException budget needs a rationale: " +
            string.Join(", ", missingRationales));

        var staleRationales = ProductionHighCountNotSupportedExceptionRationales
            .Keys
            .Except(ProductionHighCountNotSupportedExceptionBudgets.Keys, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            staleRationales,
            "Remove stale NotSupportedException rationales once budgets move or disappear: " +
            string.Join(", ", staleRationales));
    }

    [TestMethod]
    public void ProductionCode_ShouldNotContainUnsupportedTodoFallbackMarkers()
    {
        var repositoryRoot = FindRepositoryRoot();
        var offenders = EnumerateProductionSourceFiles(repositoryRoot)
            .Select(file =>
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/');
                return new SourceFileBudget(relativePath, CountOccurrences(file, "TODO: Unsupported"), 0);
            })
            .Where(file => file.LineCount > file.Budget)
            .Select(file => $"{file.FileName}: {file.LineCount}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Unsupported interpreter/codegen paths must fail fast instead of leaving TODO fallbacks: " + string.Join(", ", offenders));
    }
}
