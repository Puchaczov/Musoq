using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void RuntimeV2ExecutionFiles_ShouldStayUnderLineBudgetOrHaveExplicitJustification()
    {
        var executionDirectory = Path.Combine(FindRepositoryRoot(), "src", "dotnet", "Musoq.Evaluator", "IR", "Execution");
        var files = Directory
            .EnumerateFiles(executionDirectory, "*.cs")
            .Select(file => new SourceFileBudget(Path.GetFileName(file), CountBudgetedLines(file)))
            .ToArray();

        var offenders = files
            .Where(file => file.LineCount > RuntimeV2FileLineBudget)
            .Where(file => !OversizedRuntimeV2FileJustifications.ContainsKey(file.FileName))
            .Select(file => $"{file.FileName}: {file.LineCount}/{RuntimeV2FileLineBudget}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Runtime-v2 files above the maintainability budget need an explicit justification: " +
            string.Join(", ", offenders));

        var staleJustifications = OversizedRuntimeV2FileJustifications
            .Select(entry => new
            {
                FileName = entry.Key,
                FilePath = Path.Combine(executionDirectory, entry.Key)
            })
            .Where(entry => !File.Exists(entry.FilePath) || CountBudgetedLines(entry.FilePath) <= RuntimeV2FileLineBudget)
            .Select(entry => entry.FileName)
            .ToArray();

        Assert.IsEmpty(
            staleJustifications,
            "Remove maintainability-budget justifications once files are back under budget: " +
            string.Join(", ", staleJustifications));
    }

    [TestMethod]
    public void EvaluationHelperDomainFiles_ShouldStayUnderRuntimeV2LineBudget()
    {
        var helperDirectory = Path.Combine(FindRepositoryRoot(), "src", "dotnet", "Musoq.Evaluator", "Helpers");
        var offenders = Directory
            .EnumerateFiles(helperDirectory, "EvaluationHelper*.cs")
            .Select(file => new SourceFileBudget(Path.GetFileName(file), CountBudgetedLines(file)))
            .Where(file => file.LineCount > RuntimeV2FileLineBudget)
            .Select(file => $"{file.FileName}: {file.LineCount}/{RuntimeV2FileLineBudget}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "EvaluationHelper domain partials should remain small: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void ProductionHotspotFiles_ShouldNotGrowBeyondRound13Baseline()
    {
        var repositoryRoot = FindRepositoryRoot();
        var offenders = ProductionHotspotFileBudgets
            .Select(entry => new SourceFileBudget(
                entry.Key,
                CountBudgetedLines(ToAbsolutePath(repositoryRoot, entry.Key)),
                entry.Value))
            .Where(file => file.LineCount > file.Budget)
            .Select(file => $"{file.FileName}: {file.LineCount}/{file.Budget}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Round 13 production hotspots should split before they grow: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void ProductionFamilies_ShouldNotGrowNewOversizedPartials()
    {
        var repositoryRoot = FindRepositoryRoot();
        var offenders = ProductionFamilyBudgets
            .SelectMany(budget =>
            {
                var directory = ToAbsolutePath(repositoryRoot, budget.RelativeDirectory);
                return EnumerateSourceFamilyFiles(directory, budget.SearchPattern)
                    .Select(file => new SourceFileBudget(
                        Path.GetFileName(file),
                        CountBudgetedLines(file),
                        budget.MaxFileLines));
            })
            .Where(file => file.LineCount > file.Budget)
            .Select(file => $"{file.FileName}: {file.LineCount}/{file.Budget}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Large production families should add focused partials instead of oversized files: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void ProductionFamilyTotals_ShouldNotGrowBeyondRound13Baseline()
    {
        var repositoryRoot = FindRepositoryRoot();
        var offenders = ProductionFamilyTotalBudgets
            .Select(budget =>
            {
                var directory = ToAbsolutePath(repositoryRoot, budget.RelativeDirectory);
                var lineCount = EnumerateSourceFamilyFiles(directory, budget.SearchPattern)
                    .Sum(CountBudgetedLines);

                return new SourceFileBudget(
                    $"{budget.RelativeDirectory}/{budget.SearchPattern}",
                    lineCount,
                    budget.MaxTotalLines);
            })
            .Where(file => file.LineCount > file.Budget)
            .Select(file => $"{file.FileName}: {file.LineCount}/{file.Budget}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Tracked production families should split or retire files before total size grows: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void ExecutionExpressionFingerprintImplementations_ShouldNotGrowBeyondRound13Inventory()
    {
        var repositoryRoot = FindRepositoryRoot();
        var implementations = EnumerateProductionSourceFiles(repositoryRoot)
            .SelectMany(file =>
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/');
                return File.ReadLines(file)
                    .Select(line => line.TrimStart())
                    .Where(IsExecutionExpressionFingerprintImplementation)
                    .Select(line => $"{relativePath}: {line}");
            })
            .ToArray();

        Assert.IsLessThanOrEqualTo(
            ExecutionExpressionFingerprintImplementationBudget,
            implementations.Length,
            "Execution expression fingerprint/signature implementations should be shared before new variants appear: " +
            string.Join(", ", implementations));
    }

    [TestMethod]
    public void LargeTestFixtures_ShouldNotGrowBeyondRound13Baseline()
    {
        var repositoryRoot = FindRepositoryRoot();
        var offenders = LargeTestFixtureBudgets
            .Select(entry => new SourceFileBudget(
                entry.Key,
                CountBudgetedLines(ToAbsolutePath(repositoryRoot, entry.Key)),
                entry.Value))
            .Where(file => file.LineCount > file.Budget)
            .Select(file => $"{file.FileName}: {file.LineCount}/{file.Budget}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Large test fixtures should split before they grow: " + string.Join(", ", offenders));
    }

    [TestMethod]
    public void SplitTestFixtureFamilies_ShouldNotGrowNewOversizedPartials()
    {
        var repositoryRoot = FindRepositoryRoot();
        var offenders = SplitTestFixtureFamilyBudgets
            .SelectMany(budget =>
            {
                var directory = ToAbsolutePath(repositoryRoot, budget.RelativeDirectory);
                return Directory
                    .EnumerateFiles(directory, budget.SearchPattern)
                    .Select(file => new SourceFileBudget(
                        Path.GetFileName(file),
                        CountBudgetedLines(file),
                        budget.MaxFileLines));
            })
            .Where(file => file.LineCount > file.Budget)
            .Select(file => $"{file.FileName}: {file.LineCount}/{file.Budget}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Split test fixture families should add focused partials instead of oversized files: " +
            string.Join(", ", offenders));
    }

    private static bool IsExecutionExpressionFingerprintImplementation(string line)
    {
        return line.StartsWith("private static string CreateExpressionSignature(", StringComparison.Ordinal) ||
               line.StartsWith("private static string CreateExpressionHoistSignature(", StringComparison.Ordinal) ||
               line.StartsWith("private static string CreateWindowExpressionListSignature(", StringComparison.Ordinal) ||
               line.StartsWith("private static string CreateWindowExpressionSignature(", StringComparison.Ordinal) ||
               line.StartsWith("private static string CreateMethodSignature(", StringComparison.Ordinal) ||
               line.StartsWith("private static string CreateTypeSignature(", StringComparison.Ordinal);
    }
}
