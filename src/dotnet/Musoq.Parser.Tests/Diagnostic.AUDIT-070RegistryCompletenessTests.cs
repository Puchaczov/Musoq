using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticAudit070RegistryCompletenessTests
{
    [TestMethod]
    public void ActiveRegistry_ShouldCoverEveryEnumCodeWithCoherentMetadataAndActions()
    {
        var codes = Enum.GetValues<DiagnosticCode>()
            .Distinct()
            .OrderBy(static code => (int)code)
            .ToArray();
        var descriptors = DiagnosticDescriptorRegistry.All.ToArray();
        var metadata = ErrorMetadataCatalog.All.ToArray();

        Assert.IsEmpty(
            codes.GroupBy(static code => (int)code)
                .Where(static group => group.Count() > 1)
                .Select(static group => group.Key.ToString()),
            "Diagnostic numeric values must not have aliases.");
        Assert.HasCount(codes.Length, descriptors);
        Assert.HasCount(codes.Length, metadata);
        Assert.AreEqual(codes.Length, descriptors.Select(static descriptor => descriptor.Code).Distinct().Count());
        Assert.AreEqual(codes.Length, metadata.Select(static entry => entry.Code).Distinct().Count());

        var missingDescriptors = codes
            .Except(descriptors.Select(static descriptor => descriptor.Code))
            .ToArray();
        Assert.IsEmpty(missingDescriptors, "Missing diagnostic descriptors: " + string.Join(", ", missingDescriptors));

        var orphanedMetadata = metadata
            .Where(entry => !codes.Contains(entry.Code))
            .Select(static entry => entry.Code)
            .ToArray();
        Assert.IsEmpty(orphanedMetadata, "Orphaned diagnostic metadata: " + string.Join(", ", orphanedMetadata));

        foreach (var code in codes)
        {
            var descriptor = descriptors.Single(descriptor => descriptor.Code == code);
            var entry = metadata.Single(item => item.Code == code);

            Assert.AreEqual(ExpectedSeverity(code), descriptor.DefaultSeverity, code.ToString());
            Assert.AreEqual(DiagnosticPhaseMapping.FromCode(code), descriptor.DefaultPhase, code.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(descriptor.Category), code.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(descriptor.MessageTemplate), code.ToString());
            Assert.AreNotEqual($"Error {code}", descriptor.MessageTemplate, code.ToString());

            Assert.AreEqual(code, entry.Code, code.ToString());
            Assert.AreEqual(descriptor.DefaultPhase, entry.Phase, code.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Explanation), code.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.DocsReference), code.ToString());
            Assert.IsNotEmpty(entry.SuggestedFixes, code.ToString());
            Assert.IsTrue(entry.SuggestedFixes.All(static fix => !string.IsNullOrWhiteSpace(fix)), code.ToString());

            Assert.HasCount(entry.SuggestedFixes.Length, descriptor.DefaultActions, code.ToString());
            for (var index = 0; index < descriptor.DefaultActions.Count; index++)
            {
                var action = descriptor.DefaultActions[index];
                Assert.AreEqual(DiagnosticActionKind.Suggestion, action.Kind, code.ToString());
                Assert.IsNull(action.TextEdit, code.ToString());
                Assert.AreEqual(entry.SuggestedFixes[index], action.Title, code.ToString());
            }
        }
    }

    private static DiagnosticSeverity ExpectedSeverity(DiagnosticCode code)
    {
        return (int)code is >= 5000 and < 6000
            ? DiagnosticSeverity.Warning
            : DiagnosticSeverity.Error;
    }
}
