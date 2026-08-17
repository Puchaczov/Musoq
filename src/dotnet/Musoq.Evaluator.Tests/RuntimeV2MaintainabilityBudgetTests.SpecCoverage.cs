using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    private enum SpecCoverageStatus
    {
        ExactRuntime,
        WeakRuntime,
        InterpreterOnly,
        Unsupported,
        SpecMismatch
    }

    private sealed record SpecCoverageEntry(
        string Feature,
        SpecCoverageStatus Status,
        string[] EvidenceFiles);

    private static readonly SpecCoverageEntry[] SpecCoverageInventory =
    [
        new(
            "TABLE/COUPLE supported type matrix",
            SpecCoverageStatus.ExactRuntime,
            [
                "src/dotnet/Musoq.Evaluator.Tests/SpecExploration.TableCoupleTests.cs",
                "src/dotnet/Musoq.Evaluator.Tests/SpecExploration.TableCoupleTests.AllTypes.cs"
            ]),
        new(
            "CONTAINS null semantics",
            SpecCoverageStatus.ExactRuntime,
            ["src/dotnet/Musoq.Evaluator.Tests/PredicateSyntaxResultContractsTests.cs"]),
        new(
            "GROUP BY ALL result contracts",
            SpecCoverageStatus.ExactRuntime,
            ["src/dotnet/Musoq.Evaluator.Tests/GroupByAllTests.cs"]),
        new(
            "Implicit boolean conversion result contracts",
            SpecCoverageStatus.ExactRuntime,
            ["src/dotnet/Musoq.Evaluator.Tests/ImplicitBooleanConversionTests.cs"]),
        new(
            "RLIKE/NOT RLIKE nullable result contracts",
            SpecCoverageStatus.ExactRuntime,
            ["src/dotnet/Musoq.Evaluator.Tests/PredicateSyntaxResultContractsTests.cs"]),
        new(
            "Semantic empty and outer-join result contracts",
            SpecCoverageStatus.ExactRuntime,
            ["src/dotnet/Musoq.Evaluator.Tests/SemanticLogicalTests.cs"]),
        new(
            "DESC QUERY metadata result contracts",
            SpecCoverageStatus.ExactRuntime,
            ["src/dotnet/Musoq.Evaluator.Tests/Desc.QueryTests.cs"]),
        new(
            "Text interpretation query results",
            SpecCoverageStatus.ExactRuntime,
            [
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.AdvancedFormatsTests.AdvancedText.cs",
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.AdvancedFormatsTests.EdgeAndText.cs",
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.RealWorldAndFeatureTests.TextFormats.cs"
            ]),
        new(
            "Binary interpretation query results",
            SpecCoverageStatus.ExactRuntime,
            [
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.CoreBinaryTests.BasicQueries.cs",
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.CoreBinaryTests.ComputedFields.cs",
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.CoreBinaryTests.MixedComposition.cs",
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.CoreBinaryTests.NestedSchemas.cs",
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.AdvancedFormatsTests.ComplexQueries.cs",
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.AdvancedFormatsTests.RealWorldBinary.cs",
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.SchemaFeaturesTests.AggregationAndGrouping.cs",
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.SchemaFeaturesTests.SchemaComposition.cs",
                "src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.TypesAndExpressionsTests.ExpressionsAndCtes.cs"
            ]),
        new(
            "system.range(start, end)",
            SpecCoverageStatus.SpecMismatch,
            ["specs/musoq-core-language-spec.md"]),
        new(
            "Generic binary-schema SQL instantiation",
            SpecCoverageStatus.SpecMismatch,
            ["src/dotnet/Musoq.Evaluator.Tests/BinaryOrTextual.TypesAndExpressionsTests.PrimitiveTypes.cs"]),
        new(
            "Recursive CTEs",
            SpecCoverageStatus.Unsupported,
            ["specs/musoq-core-language-spec.md"]),
        new(
            "ASOF RIGHT JOIN",
            SpecCoverageStatus.Unsupported,
            ["specs/musoq-core-language-spec.md"]),
        new(
            "GROUPS/EXCLUDE window frames",
            SpecCoverageStatus.Unsupported,
            ["specs/musoq-core-language-spec.md"]),
        new(
            "PERCENT_RANK/CUME_DIST",
            SpecCoverageStatus.Unsupported,
            ["specs/musoq-core-language-spec.md"]),
        new(
            "Positional ORDER BY",
            SpecCoverageStatus.Unsupported,
            ["specs/musoq-core-language-spec.md"]),
        new(
            "AI syntax",
            SpecCoverageStatus.Unsupported,
            ["specs/musoq-ai-spec.md"])
    ];

    [TestMethod]
    public void SpecCoverageInventory_ShouldReferenceExistingEvidenceFiles()
    {
        var repositoryRoot = FindRepositoryRoot();
        var missing = SpecCoverageInventory
            .SelectMany(entry => entry.EvidenceFiles
                .Where(path => !File.Exists(ToAbsolutePath(repositoryRoot, path)))
                .Select(path => $"{entry.Feature}: {path}"))
            .ToArray();

        Assert.IsEmpty(missing, "Spec coverage inventory contains missing evidence files: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void SpecCoverageInventory_ShouldKeepUnsupportedAndMismatchFeaturesOutOfPositiveRuntimeScope()
    {
        var excludedStatuses = SpecCoverageInventory
            .Where(entry => entry.Status is SpecCoverageStatus.Unsupported or SpecCoverageStatus.SpecMismatch)
            .Select(entry => entry.Status)
            .Distinct()
            .ToArray();

        CollectionAssert.AreEquivalent(
            new[] { SpecCoverageStatus.Unsupported, SpecCoverageStatus.SpecMismatch },
            excludedStatuses);
    }
}
