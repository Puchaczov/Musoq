using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCatalogFreshnessTests
{
    [TestMethod]
    public void CommittedMachineReadableCatalog_MatchesDescriptorRegistry()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "specs", "diagnostic-catalog.json");
        var document = JsonSerializer.Deserialize<CatalogDocument>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(document);
        Assert.AreEqual(1, document.SchemaVersion);
        Assert.AreEqual(
            "Musoq.Parser.Diagnostics.DiagnosticDescriptorRegistry",
            document.GeneratedFrom);

        var expected = DiagnosticDescriptorRegistry.All
            .OrderBy(static descriptor => (int)descriptor.Code)
            .ToArray();

        Assert.HasCount(expected.Length, document.Diagnostics);

        for (var index = 0; index < expected.Length; index++)
        {
            var descriptor = expected[index];
            var entry = document.Diagnostics[index];

            Assert.AreEqual(descriptor.Code.ToString(), entry.Code, $"Entry {index} code");
            Assert.AreEqual((int)descriptor.Code, entry.Number, entry.Code);
            Assert.AreEqual(descriptor.DefaultPhase.ToString(), entry.Phase, entry.Code);
            Assert.AreEqual(descriptor.DefaultSeverity.ToString(), entry.Severity, entry.Code);
            Assert.AreEqual(descriptor.Category, entry.Category, entry.Code);
            Assert.AreEqual(descriptor.MessageTemplate, entry.MessageTemplate, entry.Code);
            Assert.AreEqual(descriptor.Explanation, entry.Explanation, entry.Code);
            Assert.AreEqual(descriptor.DocsReference, entry.DocsReference, entry.Code);
            CollectionAssert.AreEqual(descriptor.SuggestedFixes.ToArray(), entry.SuggestedFixes, entry.Code);
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "specs", "musoq-core-language-spec.md")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the Musoq repository root.");
    }

    private sealed class CatalogDocument
    {
        public int SchemaVersion { get; set; }

        public string GeneratedFrom { get; set; } = string.Empty;

        public CatalogEntry[] Diagnostics { get; set; } = [];
    }

    private sealed class CatalogEntry
    {
        public string Code { get; set; } = string.Empty;

        public int Number { get; set; }

        public string Phase { get; set; } = string.Empty;

        public string Severity { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string MessageTemplate { get; set; } = string.Empty;

        public string? Explanation { get; set; }

        public string? DocsReference { get; set; }

        public string[] SuggestedFixes { get; set; } = [];
    }
}
