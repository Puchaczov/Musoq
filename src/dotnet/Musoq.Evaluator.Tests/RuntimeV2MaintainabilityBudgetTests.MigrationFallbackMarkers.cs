using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    [TestMethod]
    public void ProductionCode_ShouldNotReintroduceRuntimeV2MigrationFallbackMarkers()
    {
        var repositoryRoot = FindRepositoryRoot();
        var markers = new[]
        {
            "OptimizationFallbackWarningReporter",
            "TableFallback",
            "PredicateOuterJoinFallback",
            "RenderParallelLoopSerialFallback",
            "SerialFallback",
            "SerialPath",
            "RenderParallelLoopSerialPath",
            "SetOperationTableStrategy.RowComparer",
            "ExecutionSetOperationStrategy.RowComparer",
            "BaseOperations.Union",
            "BaseOperations.Except",
            "BaseOperations.Intersect",
            "DiagnosticOnlyOpportunity",
            "APPLY fallback",
            "fallback dispatch",
            "legacy right-side",
            "materialized fallback",
            "V1 sidecar",
            "diagnostic-only in v1"
        };

        var offenders = EnumerateProductionSourceFiles(repositoryRoot)
            .SelectMany(file => FindForbiddenMarkers(repositoryRoot, file, markers))
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Runtime-v2 migration fallback markers must stay out of production code: " +
            string.Join(", ", offenders));
    }

    private static string[] FindForbiddenMarkers(
        string repositoryRoot,
        string file,
        string[] markers)
    {
        var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace(Path.DirectorySeparatorChar, '/');
        return markers
            .Select(marker => (Marker: marker, Count: CountOccurrences(file, marker)))
            .Where(match => match.Count > 0)
            .Select(match => $"{relativePath}: {match.Count} occurrence(s) of '{match.Marker}'")
            .ToArray();
    }
}
