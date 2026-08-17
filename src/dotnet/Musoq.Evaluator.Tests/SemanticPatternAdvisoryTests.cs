using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class SemanticPatternAdvisoryTests
{
    [TestMethod]
    public void RLike_OrdinaryWordBoundaryEscape_ReportsOneWarningAtTheEscape()
    {
        var query = "select Name from #A.Entities() where Name rlike '\\bword\\b'";
        var result = Analyze(query);

        Assert.IsTrue(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5015_SuspiciousRegexEscape),
            string.Join(" | ", result.Diagnostics.Select(static item => $"{item.Code}:{item.Message}@{item.Location.Offset}:{item.Span}")));
        var warning = result.Warnings.Single(static item =>
            item.Code == DiagnosticCode.MQ5015_SuspiciousRegexEscape);

        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        Assert.AreEqual(query.IndexOf("\\b", System.StringComparison.Ordinal), warning.Location.Offset);
        Assert.IsTrue(warning.Message.Contains("regex", System.StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void RLike_RawAndDoubledWordBoundaryEscapes_RemainQuiet()
    {
        var raw = Analyze("select Name from #A.Entities() where Name rlike r'\\bword\\b'");
        var doubled = Analyze("select Name from #A.Entities() where Name rlike '\\\\bword\\\\b'");
        var characterClass = Analyze("select Name from #A.Entities() where Name rlike '[\\b]'");

        Assert.IsFalse(raw.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5015_SuspiciousRegexEscape));
        Assert.IsFalse(doubled.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5015_SuspiciousRegexEscape));
        Assert.IsFalse(characterClass.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5015_SuspiciousRegexEscape));
    }

    [TestMethod]
    public void RLike_ConstantLetChain_ReportsAtTheOriginatingLiteral()
    {
        var query = "let first: string = '\\bword'; let second: string = $first; select Name from #A.Entities() where Name rlike $second";
        var result = Analyze(query);

        Assert.AreEqual(1, result.Warnings.Count(static item =>
            item.Code == DiagnosticCode.MQ5015_SuspiciousRegexEscape),
            string.Join(" | ", result.Diagnostics.Select(static item => $"{item.Code}:{item.Message}@{item.Location.Offset}")));
        var warning = result.Warnings.Single(static item =>
            item.Code == DiagnosticCode.MQ5015_SuspiciousRegexEscape);
        Assert.AreEqual(query.IndexOf("\\b", System.StringComparison.Ordinal), warning.Location.Offset);
    }

    [TestMethod]
    public void Like_GlobPatterns_ReportOnlyWhenThePatternLooksLikeAPath()
    {
        var star = Analyze("select Name from #A.Entities() where Name like '*.log'");
        var question = Analyze("select Name from #A.Entities() where Name like 'file?.txt'");
        var sql = Analyze("select Name from #A.Entities() where Name like '%.log'");
        var sentence = Analyze("select Name from #A.Entities() where Name like 'is this?'");

        Assert.AreEqual(1, star.Warnings.Count(static item => item.Code == DiagnosticCode.MQ5016_GlobWildcardInLike));
        Assert.AreEqual(1, question.Warnings.Count(static item => item.Code == DiagnosticCode.MQ5016_GlobWildcardInLike));
        Assert.IsFalse(sql.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5016_GlobWildcardInLike));
        Assert.IsFalse(sentence.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5016_GlobWildcardInLike));
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = []
            });
        return new QueryAnalyzer(provider).Analyze(query);
    }

}
