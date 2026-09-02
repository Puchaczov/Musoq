using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore010StarModifierTests
{
    [TestMethod]
    public void QualifiedStarModifierChain_ShouldPreserveScopeOrderAndCase()
    {
        const string query =
            "select a.* like '%o%' exclude (country) replace (Population * 3 as population) " +
            "rename (population as Population3x) from #some.entities() a";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var star = GetQuery(result.Root!).Select.Fields.Single().Expression as AllColumnsNode;
        Assert.IsNotNull(star);
        Assert.AreEqual("a", star.Alias);
        Assert.AreEqual("%o%", star.LikePattern);
        Assert.IsFalse(star.IsNotLike);
        CollectionAssert.AreEqual(new[] { "country" }, star.ExcludeColumns);
        Assert.IsNotNull(star.ReplaceItems);
        Assert.AreEqual("population", star.ReplaceItems.Single().ColumnName);
        Assert.IsNotNull(star.RenameItems);
        Assert.AreEqual("population", star.RenameItems.Single().SourceName);
        Assert.AreEqual("Population3x", star.RenameItems.Single().TargetName);
    }

    [TestMethod]
    public void ContextSensitiveModifierNames_ShouldRemainIdentifiersOutsideStar()
    {
        const string query = "select exclude, replace, rename from #some.entities()";

        var result = ParseWithDiagnostics(query);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.IsEmpty(result.Diagnostics, result.FormatDiagnostics());

        var fields = GetQuery(result.Root!).Select.Fields;
        CollectionAssert.AreEqual(
            new[] { "exclude", "replace", "rename" },
            fields.Select(static field => field.FieldName).ToArray());
    }

    [TestMethod]
    public void InvalidModifierOrder_ShouldExposeStructuredParseDiagnosticAtOffendingModifier()
    {
        const string query = "select * replace (1 as Name) like 'N%' from #some.entities()";

        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2041_InvalidStarModifierOrder, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf("like", System.StringComparison.Ordinal), 4), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    [TestMethod]
    public void MistypedModifier_ShouldExposeStructuredNearMissDiagnosticAtTypo()
    {
        const string query = "select * exclud (Name) from #some.entities()";

        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ2001_UnexpectedToken, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf("exclud", System.StringComparison.Ordinal), 6), diagnostic.Span);
        StringAssert.Contains(diagnostic.Message, "Did you mean EXCLUDE");
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static QueryNode GetQuery(RootNode root)
    {
        var statements = Assert.IsInstanceOfType<StatementsArrayNode>(root.Expression);
        var statement = Assert.IsInstanceOfType<SingleSetNode>(statements.Statements.Single().Node);
        return statement.Query;
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
