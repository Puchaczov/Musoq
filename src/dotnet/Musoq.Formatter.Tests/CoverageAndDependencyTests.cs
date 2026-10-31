using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Formatter.Tests.Verification;
using Musoq.Parser.Syntax;
using Musoq.Parser.Tokens;

namespace Musoq.Formatter.Tests;

[TestClass]
public sealed class CoverageAndDependencyTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlySet<string>>> FixtureCoverage = new(() =>
        CorpusFixtures.Load().ToDictionary(fixture => fixture.Id, fixture => AstCoverage.Types(fixture.Source), StringComparer.Ordinal));

    public static IEnumerable<object[]> NodeCases => Entries("parserNodes")
        .Select(entry => new object[] { entry.GetProperty("fullName").GetString()!, entry.GetRawText() });

    public static IEnumerable<object[]> FamilyCases => Entries("families")
        .Select(entry => new object[] { entry.GetProperty("id").GetString()!, entry.GetRawText() });

    public static IEnumerable<object[]> SyntaxCases => Entries("syntaxKinds").SelectMany(entry =>
        entry.GetProperty("fixtures").EnumerateArray().Select(id => new object[] {
            entry.GetProperty("kind").GetString()!, id.GetString()!, entry.GetRawText() }));

    [TestMethod]
    public void LibraryDependencies_ShouldContainOnlyTheParserAmongMusoqAssemblies()
    {
        var references = VerificationCompiler.Current.References("Formatter");

        CollectionAssert.AreEquivalent(new[] { "Musoq.Parser" }, references);
    }

    [TestMethod]
    public void TestDependencies_ShouldContainOnlyTheFormatterAndParserAmongMusoqAssemblies()
    {
        var references = VerificationCompiler.Current.References("Tests");

        CollectionAssert.AreEquivalent(new[] { "Musoq.Formatter", "Musoq.Parser" }, references);
    }

    [TestMethod]
    public void Inventory_ShouldMatchPublicParserNodesAndSyntaxHelpers()
    {
        var inventory = CoverageManifest.Inventory;
        var actual = VerificationCompiler.Current.ParserDeclarations();
        Assert.AreEqual(inventory.Length, inventory.Distinct(StringComparer.Ordinal).Count());
        CollectionAssert.AreEquivalent(actual, inventory, "Unexpected: " + string.Join(", ", actual.Except(inventory)) +
            "; missing: " + string.Join(", ", inventory.Except(actual)));
    }

    [TestMethod]
    public void FixtureIdentifiers_ShouldBeUnique()
    {
        var cases = CorpusFixtures.Load();

        Assert.AreEqual(cases.Count, cases.Select(fixture => fixture.Id).Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    [DynamicData(nameof(NodeCases))]
    public void SourceNodeAssociation_ShouldBeExercisedByItsFixtures(string type, string entryJson)
    {
        using var entry = JsonDocument.Parse(entryJson);

        Assert.AreEqual(type, entry.RootElement.GetProperty("fullName").GetString());
        VerifyNode(entry.RootElement, FixtureCoverage.Value);
    }

    [TestMethod]
    [DynamicData(nameof(FamilyCases))]
    public void LanguageFamily_ShouldReferenceRetainedFixtures(string family, string entryJson)
    {
        using var entry = JsonDocument.Parse(entryJson);
        var ids = CorpusFixtures.Load().Select(fixture => fixture.Id).ToHashSet(StringComparer.Ordinal);
        var fixtures = entry.RootElement.GetProperty("fixtures").EnumerateArray().Select(id => id.GetString()!).ToArray();

        Assert.IsNotEmpty(fixtures, family);
        CollectionAssert.AreEqual(Array.Empty<string>(), fixtures.Where(id => !ids.Contains(id)).ToArray(), family);
    }

    [TestMethod]
    public void ManifestKinds_ShouldMatchAllGrammarKindsAndLanguageFamilies()
    {
        var kinds = Entries("syntaxKinds").Select(entry => entry.GetProperty("kind").GetString()!).ToArray();
        var families = Entries("families").Select(entry => entry.GetProperty("id").GetString()!).ToArray();

        CollectionAssert.AreEquivalent(VerificationCompiler.Current.SyntaxKinds(), kinds);
        Assert.AreEqual(12, families.Length);
        Assert.AreEqual(families.Length, families.Distinct(StringComparer.Ordinal).Count());
    }

    [TestMethod]
    [DynamicData(nameof(SyntaxCases))]
    public void SyntaxRegionAssociation_ShouldBeExercisedByItsFixture(string kind, string id, string entryJson)
    {
        using var entry = JsonDocument.Parse(entryJson);
        var fixture = CorpusFixtures.Load().Single(fixture => fixture.Id == id);

        Assert.AreEqual(kind, entry.RootElement.GetProperty("kind").GetString());
        VerifySyntax(entry.RootElement, Parser.Parser.ParseSyntax(fixture.Source), fixture);
    }

    private static void VerifySyntax(JsonElement entry, SyntaxParseResult parsed, CorpusFixtures.Fixture fixture)
    {
        var kind = (SyntaxKind)VerificationCompiler.Current.SyntaxKindValue(entry.GetProperty("kind").GetString()!);
        Assert.IsTrue(parsed.Success, fixture.Id);
        if (kind == SyntaxKind.HavingClause)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.GetProperty("reason").GetString()));
            Assert.IsTrue(parsed.Tokens.Any(token => token.Role == SyntaxTokenRole.Keyword && token.TokenType == TokenType.Having), fixture.Id);
            Assert.IsTrue(AstCoverage.Types(fixture.Source).Contains("Musoq.Parser.Nodes.HavingNode"), fixture.Id);
            return;
        }
        Assert.IsTrue(Kinds(parsed.Root!).Contains(kind), kind + ": " + fixture.Id);
    }

    private static void VerifyNode(JsonElement entry, IReadOnlyDictionary<string, IReadOnlySet<string>> coverage)
    {
        var name = entry.GetProperty("fullName").GetString()!;
        Assert.IsFalse(string.IsNullOrWhiteSpace(entry.GetProperty("rule").GetString()), name);
        var disposition = entry.GetProperty("disposition").GetString();
        if (disposition == "normalized-ast-only")
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(entry.GetProperty("reason").GetString()), name);
            Assert.IsFalse(coverage.Values.Any(types => types.Contains(name)), "Parsed declaration misclassified: " + name);
            return;
        }
        Assert.AreEqual("source-syntax", disposition, name);
        var fixtures = entry.GetProperty("fixtures").EnumerateArray().Select(id => id.GetString()!).ToArray();
        Assert.IsNotEmpty(fixtures, name);
        foreach (var id in fixtures)
        {
            Assert.IsTrue(coverage.ContainsKey(id), name + ": " + id);
            Assert.IsTrue(coverage[id].Contains(name), name + " not exercised by " + id);
        }
    }

    private static IEnumerable<SyntaxKind> Kinds(SyntaxNode node)
    {
        yield return node.Kind;
        foreach (var child in node.Children)
            foreach (var kind in Kinds(child))
                yield return kind;
    }

    private static JsonElement[] Entries(string property) => CoverageManifest.Entries(property);
}
