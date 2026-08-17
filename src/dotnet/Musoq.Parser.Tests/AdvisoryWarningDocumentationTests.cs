using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class AdvisoryWarningDocumentationTests
{
    private static readonly DiagnosticCode[] ActiveAdvisoryCodes =
    [
        DiagnosticCode.MQ5003_ImplicitTypeConversion,
        DiagnosticCode.MQ5008_UnreachableCode,
        DiagnosticCode.MQ5010_TautologicalCondition,
        DiagnosticCode.MQ5011_ContradictoryCondition,
        DiagnosticCode.MQ5013_SourceContractWarning,
        DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape,
        DiagnosticCode.MQ5015_SuspiciousRegexEscape,
        DiagnosticCode.MQ5016_GlobWildcardInLike,
        DiagnosticCode.MQ5017_NullComparison,
        DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck,
        DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter,
        DiagnosticCode.MQ5020_SetOperationOrderByScope,
        DiagnosticCode.MQ5021_UnorderedSkip,
        DiagnosticCode.MQ5022_UnusedCte,
        DiagnosticCode.MQ5023_UnusedScriptVariable,
        DiagnosticCode.MQ5024_NullSensitiveNotIn,
        DiagnosticCode.MQ5025_ImpossibleImplicitConversion
    ];

    [TestMethod]
    public void ActiveAdvisoryCatalog_ShouldMatchDocumentedWarningContract()
    {
        foreach (var code in ActiveAdvisoryCodes)
        {
            var metadata = ErrorMetadataCatalog.Get(code);

            Assert.IsNotNull(metadata, $"Missing metadata for {code}");
            Assert.AreEqual(code, metadata.Code);
            Assert.AreEqual(DiagnosticSeverity.Warning, ErrorCatalog.GetDefaultSeverity(code));
            Assert.AreEqual(
                code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape
                    ? DiagnosticPhase.Parse
                    : DiagnosticPhase.Bind,
                metadata.Phase,
                $"Unexpected phase for {code}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(metadata.Explanation));
            Assert.IsNotEmpty(metadata.SuggestedFixes);
            Assert.IsTrue(metadata.SuggestedFixes.All(static fix => !string.IsNullOrWhiteSpace(fix)));
            Assert.IsFalse(string.IsNullOrWhiteSpace(metadata.DocsReference));
        }
    }

    [TestMethod]
    public void DocumentedRootedPathWarning_ShouldHaveWarningSeverityFormatterOutputAndSpan()
    {
        const string query = @"select 'C:\new\test' from system.dual()";
        var result = Parse(query);
        var warning = result.Warnings.Single(static item =>
            item.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape);
        var formatted = new DiagnosticFormatter { UseColor = false }.Format(warning);

        Assert.IsTrue(result.Success, result.FormatDiagnostics());
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, warning.Phase);
        Assert.AreEqual(query.IndexOf(@"\n", System.StringComparison.Ordinal), warning.Span.Start);
        Assert.IsTrue(warning.Span.Length > 0);
        StringAssert.Contains(formatted, "MQ5014");
        StringAssert.Contains(formatted, "raw literal");
    }

    [TestMethod]
    public void SpecificationAndReadme_DocumentTheDiagnosticContractAndV18Migration()
    {
        var root = FindRepositoryRoot();
        var specification = File.ReadAllText(Path.Combine(root, "specs", "musoq-core-language-spec.md"));
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));

        StringAssert.Contains(specification, "Diagnostic Contract and Catalog");
        StringAssert.Contains(specification, "MQ3086_UnknownCallable");
        StringAssert.Contains(specification, "MQ3087_InvalidCallableArity");
        StringAssert.Contains(specification, "MQ3088_NoMatchingCallableOverload");
        StringAssert.Contains(specification, "MQ3089_AmbiguousCallableOverload");
        StringAssert.Contains(specification, "MQ5024_NullSensitiveNotIn");
        StringAssert.Contains(specification, "MQ5025_ImpossibleImplicitConversion");
        StringAssert.Contains(specification, "MQ5009 remains a compatibility documentation identifier only");
        StringAssert.Contains(specification, "GeneratedSource");
        StringAssert.Contains(specification, "zero-length span");
        StringAssert.Contains(readme, "where Name not in ('Alice', NULL)");
        StringAssert.Contains(readme, "MQ5025");
    }

    private static ParseResult Parse(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "specs", "musoq-core-language-spec.md")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the Musoq repository root.");
    }
}
