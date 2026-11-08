using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void SubqueryRewriteMaintenanceContracts_ShouldStayCentralized()
    {
        var repositoryRoot = FindRepositoryRoot();
        var generatedNameContract = "src/dotnet/Musoq.Evaluator/Visitors/Helpers/Subqueries/GeneratedSubqueryContract.cs";
        var diagnosticFactory = "src/dotnet/Musoq.Evaluator/Visitors/Helpers/Subqueries/SubqueryDiagnosticFactory.cs";
        var generatedNameLiterals = new[] { "\"_sq_", "\"_dt_", "\"_sm_", "\"_corr_" };

        var generatedNameOffenders = EnumerateProductionSourceFiles(repositoryRoot)
            .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => !string.Equals(path, generatedNameContract, StringComparison.Ordinal))
            .Where(path => generatedNameLiterals.Any(literal =>
                File.ReadAllText(ToAbsolutePath(repositoryRoot, path)).Contains(literal, StringComparison.Ordinal)))
            .ToArray();

        Assert.IsEmpty(
            generatedNameOffenders,
            "Generated subquery names and marker literals must live in GeneratedSubqueryContract: " +
            string.Join(", ", generatedNameOffenders));

        var diagnosticOffenders = EnumerateProductionSourceFiles(repositoryRoot)
            .Select(file => Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => path.Contains("SubqueryToCteRewriteVisitor", StringComparison.Ordinal) ||
                           path.Contains("Visitors/Helpers/Subqueries", StringComparison.Ordinal))
            .Where(path => !string.Equals(path, diagnosticFactory, StringComparison.Ordinal))
            .Where(path => File.ReadAllText(ToAbsolutePath(repositoryRoot, path))
                .Contains("MQ2024_InvalidSubquery", StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            diagnosticOffenders,
            "MQ2024 subquery rewrite diagnostics must be created through SubqueryDiagnosticFactory: " +
            string.Join(", ", diagnosticOffenders));
    }
}
