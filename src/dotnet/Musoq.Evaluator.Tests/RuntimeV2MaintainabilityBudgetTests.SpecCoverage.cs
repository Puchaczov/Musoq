using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    private static readonly IReadOnlyDictionary<string, FeatureEvidenceKind[]> ExpectedEvidenceByStatus =
        new Dictionary<string, FeatureEvidenceKind[]>(StringComparer.Ordinal)
        {
            ["exact-runtime"] = [FeatureEvidenceKind.RuntimePositive],
            ["partial-runtime"] =
                [FeatureEvidenceKind.RuntimePositive, FeatureEvidenceKind.RuntimeNegativeDiagnostic],
            ["interpreter-only"] =
                [FeatureEvidenceKind.InterpreterPositive, FeatureEvidenceKind.RuntimeNegativeDiagnostic],
            ["unsupported"] = [FeatureEvidenceKind.RuntimeNegativeDiagnostic],
            ["proposal"] = []
        };

    private static readonly IReadOnlyDictionary<string, FeatureEvidenceKind> EvidenceKindByManifestName =
        new Dictionary<string, FeatureEvidenceKind>(StringComparer.Ordinal)
        {
            ["runtime-positive"] = FeatureEvidenceKind.RuntimePositive,
            ["runtime-negative-diagnostic"] = FeatureEvidenceKind.RuntimeNegativeDiagnostic,
            ["interpreter-positive"] = FeatureEvidenceKind.InterpreterPositive
        };

    private static readonly HashSet<string> SupportedOwners =
        ["core", "host", "language-proposal"];

    [TestMethod]
    public void FeatureCoverageLedger_ShouldHaveValidShapeAndDocumentation()
    {
        var repositoryRoot = FindRepositoryRoot();
        var ledger = ReadFeatureCoverageLedger(repositoryRoot);

        Assert.AreEqual(1, ledger.SchemaVersion);
        Assert.IsNotEmpty(ledger.Features);

        var duplicateIds = ledger.Features
            .GroupBy(static feature => feature.Id, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();
        Assert.IsEmpty(duplicateIds, "Duplicate feature ids: " + string.Join(", ", duplicateIds));

        foreach (var feature in ledger.Features)
        {
            Assert.IsTrue(
                IsStableFeatureId(feature.Id),
                $"Feature id '{feature.Id}' must use lowercase kebab-case.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(feature.Title), feature.Id);
            Assert.IsTrue(SupportedOwners.Contains(feature.Owner), $"{feature.Id}: unsupported owner '{feature.Owner}'.");
            Assert.IsTrue(
                ExpectedEvidenceByStatus.ContainsKey(feature.Status),
                $"{feature.Id}: unsupported status '{feature.Status}'. Spec mismatches must fail, not become a status.");
            Assert.IsNotEmpty(feature.Documentation, $"{feature.Id}: missing documentation.");

            var missingDocumentation = feature.Documentation
                .Where(path => !File.Exists(ToAbsolutePath(repositoryRoot, path)))
                .ToArray();
            Assert.IsEmpty(
                missingDocumentation,
                $"{feature.Id}: missing documentation: {string.Join(", ", missingDocumentation)}");

            var expectedEvidence = ExpectedEvidenceByStatus[feature.Status];
            var declaredEvidence = feature.RequiredEvidence
                .Select(name => ResolveEvidenceKind(feature.Id, name))
                .ToArray();
            CollectionAssert.AreEquivalent(
                expectedEvidence,
                declaredEvidence,
                $"{feature.Id}: status '{feature.Status}' has the wrong evidence contract.");

            if (feature.Status == "proposal")
            {
                Assert.IsTrue(
                    feature.Documentation.Any(path =>
                        File.ReadAllText(ToAbsolutePath(repositoryRoot, path))
                            .Contains("**Status:** Proposal", StringComparison.OrdinalIgnoreCase)),
                    $"{feature.Id}: proposal documentation must be marked 'Status: Proposal'.");
            }
        }
    }

    [TestMethod]
    public void FeatureCoverageLedger_ShouldResolveEveryEvidenceRequirementToExecutableTests()
    {
        var ledger = ReadFeatureCoverageLedger(FindRepositoryRoot());
        var featuresById = ledger.Features.ToDictionary(static feature => feature.Id, StringComparer.Ordinal);
        var evidence = typeof(RuntimeV2MaintainabilityBudgetTests).Assembly
            .GetTypes()
            .SelectMany(static type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public))
            .SelectMany(method => method.GetCustomAttributes<FeatureEvidenceAttribute>(inherit: false)
                .Select(attribute => new FeatureEvidenceDeclaration(method, attribute)))
            .ToArray();

        var unknownFeatureIds = evidence
            .Where(item => !featuresById.ContainsKey(item.Attribute.FeatureId))
            .Select(item => $"{item.Attribute.FeatureId} ({FormatMethod(item.Method)})")
            .ToArray();
        Assert.IsEmpty(unknownFeatureIds, "Evidence references unknown features: " + string.Join(", ", unknownFeatureIds));

        var duplicateEvidence = evidence
            .GroupBy(item => (item.Method, item.Attribute.FeatureId, item.Attribute.Kind))
            .Where(static group => group.Count() > 1)
            .Select(group => $"{group.Key.FeatureId}/{group.Key.Kind} on {FormatMethod(group.Key.Method)}")
            .ToArray();
        Assert.IsEmpty(duplicateEvidence, "Duplicate evidence declarations: " + string.Join(", ", duplicateEvidence));

        foreach (var item in evidence)
        {
            Assert.IsNotNull(
                item.Method.GetCustomAttribute<TestMethodAttribute>(inherit: false),
                $"{FormatMethod(item.Method)} is feature evidence but is not an executable MSTest method.");
        }

        foreach (var feature in ledger.Features)
        {
            var actualKinds = evidence
                .Where(item => item.Attribute.FeatureId == feature.Id)
                .Select(static item => item.Attribute.Kind)
                .Distinct()
                .ToArray();
            var requiredKinds = feature.RequiredEvidence
                .Select(name => ResolveEvidenceKind(feature.Id, name))
                .ToArray();

            CollectionAssert.AreEquivalent(
                requiredKinds,
                actualKinds,
                $"{feature.Id}: compiled test evidence does not match the ledger.");
        }
    }

    private static FeatureCoverageLedger ReadFeatureCoverageLedger(string repositoryRoot)
    {
        var path = ToAbsolutePath(repositoryRoot, "specs/feature-coverage.json");
        var ledger = JsonSerializer.Deserialize<FeatureCoverageLedger>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.IsNotNull(ledger, "Could not deserialize specs/feature-coverage.json.");
        return ledger;
    }

    private static FeatureEvidenceKind ResolveEvidenceKind(string featureId, string name)
    {
        Assert.IsTrue(
            EvidenceKindByManifestName.TryGetValue(name, out var kind),
            $"{featureId}: unsupported evidence kind '{name}'.");
        return kind;
    }

    private static bool IsStableFeatureId(string value)
    {
        if (string.IsNullOrEmpty(value) || value[0] == '-' || value[^1] == '-')
            return false;

        return value.All(static character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
    }

    private static string FormatMethod(MethodInfo method) =>
        $"{method.DeclaringType?.FullName}.{method.Name}";

    private sealed record FeatureCoverageLedger(int SchemaVersion, FeatureCoverageEntry[] Features);

    private sealed record FeatureCoverageEntry(
        string Id,
        string Title,
        string Owner,
        string Status,
        string[] Documentation,
        string[] RequiredEvidence);

    private sealed record FeatureEvidenceDeclaration(MethodInfo Method, FeatureEvidenceAttribute Attribute);
}
