using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ContextualStringAdvisoryTests
{
    [TestMethod]
    [DataRow("path", true)]
    [DataRow("file_path", true)]
    [DataRow("file-path", true)]
    [DataRow("paths", true)]
    [DataRow("file", true)]
    [DataRow("files", true)]
    [DataRow("filename", true)]
    [DataRow("directory", true)]
    [DataRow("dir", true)]
    [DataRow("folder", true)]
    [DataRow("root", true)]
    [DataRow("message", false)]
    public void PathSensitiveNameNormalizer_UsesConservativeVocabulary(string name, bool expected)
    {
        Assert.AreEqual(expected, SuspiciousOrdinaryStringEscapeDiagnostics.IsPathSensitiveName(name));
    }

    [TestMethod]
    public void AmbiguousDateText_ImplicitDateTimeConversion_ReportsMq5003()
    {
        const string query = "select 1 from #A.Entities() where Time = '01/02/2026'";
        var result = AnalyzeBasic(query);

        AssertNoErrors(result);
        var warning = result.Warnings.Single(static item =>
            item.Code == DiagnosticCode.MQ5003_ImplicitTypeConversion);
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, warning.Phase);
        Assert.AreEqual(query.IndexOf("'01/02/2026'", StringComparison.Ordinal), warning.Span.Start);
        Assert.IsTrue(warning.Message.Contains("ambiguous", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void AmbiguousDateText_DateTimeOffsetConversion_ReportsMq5003()
    {
        var result = AnalyzeBasic(
            "param (moment: datetimeoffset); select 1 from #A.Entities() where $moment = '01/02/2026'");

        AssertNoErrors(result);
        Assert.AreEqual(
            1,
            result.Warnings.Count(static item => item.Code == DiagnosticCode.MQ5003_ImplicitTypeConversion));
    }

    [TestMethod]
    public void UnambiguousDatesAndExplicitFormats_RemainQuiet()
    {
        var dayFirst = AnalyzeBasic("select 1 from #A.Entities() where Time = '13/02/2026'");
        var yearFirst = AnalyzeBasic("select 1 from #A.Entities() where Time = '2026/01/02'");
        var monthName = AnalyzeBasic("select 1 from #A.Entities() where Time = 'January 2, 2026'");
        var monthThirteenth = AnalyzeBasic("select 1 from #A.Entities() where Time = '01/13/2026'");
        var explicitResult = AnalyzeBasic(
            "select ToDateTimeWithFormat('01/02/2026', 'dd/MM/yyyy') from #A.Entities()");
        var projection = AnalyzeBasic("select '01/02/2026' from #A.Entities()");

        AssertNoErrors(dayFirst);
        AssertNoErrors(yearFirst);
        AssertNoErrors(monthName);
        AssertNoErrors(monthThirteenth);
        AssertNoErrors(explicitResult);
        AssertNoErrors(projection);
        Assert.IsFalse(dayFirst.Warnings.Any(static item =>
            item.Code == DiagnosticCode.MQ5003_ImplicitTypeConversion));
        Assert.IsFalse(yearFirst.Warnings.Any(static item =>
            item.Code == DiagnosticCode.MQ5003_ImplicitTypeConversion));
        Assert.IsFalse(monthName.Warnings.Any(static item =>
            item.Code == DiagnosticCode.MQ5003_ImplicitTypeConversion));
        Assert.IsFalse(monthThirteenth.Warnings.Any(static item =>
            item.Code == DiagnosticCode.MQ5003_ImplicitTypeConversion));
        Assert.IsFalse(explicitResult.Warnings.Any(static item =>
            item.Code == DiagnosticCode.MQ5003_ImplicitTypeConversion));
        Assert.IsFalse(projection.Warnings.Any(static item =>
            item.Code == DiagnosticCode.MQ5003_ImplicitTypeConversion));
    }

    [TestMethod]
    public void ImpossibleImplicitTemporalConversions_ReportMq5025()
    {
        const string dateQuery = "select 1 from #A.Entities() where Time = '02/31/2026'";
        var dateResult = AnalyzeBasic(dateQuery);
        AssertNoErrors(dateResult);
        var dateWarning = dateResult.Warnings.Single(static item =>
            item.Code == DiagnosticCode.MQ5025_ImpossibleImplicitConversion);
        Assert.AreEqual(DiagnosticSeverity.Warning, dateWarning.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, dateWarning.Phase);
        Assert.AreEqual(dateQuery.IndexOf("'02/31/2026'", StringComparison.Ordinal), dateWarning.Span.Start);

        var offsetResult = AnalyzeBasic(
            "param (moment: datetimeoffset); select 1 from #A.Entities() where $moment = 'not-a-date'");
        AssertNoErrors(offsetResult);
        Assert.AreEqual(1, offsetResult.Warnings.Count(static item =>
            item.Code == DiagnosticCode.MQ5025_ImpossibleImplicitConversion));
    }

    [TestMethod]
    public void ExplicitTemporalConversion_IsNotClassifiedAsImplicitFailure()
    {
        var result = AnalyzeBasic("select ToDateTime('not-a-date') from #A.Entities()");

        AssertNoErrors(result);
        Assert.IsFalse(result.Warnings.Any(static item =>
            item.Code == DiagnosticCode.MQ5025_ImpossibleImplicitConversion));
    }

    [TestMethod]
    public void PathColumn_EqualityInequalityAndIn_ReportsRelativeRisk()
    {
        var equality = AnalyzePath(
            @"select FilePath from #P.Entities() where FilePath = 'some\text'");
        var inequality = AnalyzePath(
            @"select FilePath from #P.Entities() where FilePath <> 'some\text'");
        var inList = AnalyzePath(
            @"select FilePath from #P.Entities() where FilePath in ('some\text', r'C:\safe')");

        AssertNoErrors(equality);
        AssertNoErrors(inequality);
        AssertNoErrors(inList);
        Assert.AreEqual(1, CountPathWarnings(equality));
        Assert.AreEqual(1, CountPathWarnings(inequality));
        Assert.AreEqual(1, CountPathWarnings(inList));
    }

    [TestMethod]
    public void PathColumn_LikeAndNotLike_ReportButRLikeRemainsQuiet()
    {
        var like = AnalyzePath(
            @"select FilePath from #P.Entities() where FilePath like 'some\text'");
        var notLike = AnalyzePath(
            @"select FilePath from #P.Entities() where FilePath not like 'some\text'");
        var rlike = AnalyzePath(
            @"select FilePath from #P.Entities() where FilePath rlike 'some\text'");

        AssertNoErrors(like);
        AssertNoErrors(notLike);
        AssertNoErrors(rlike);
        Assert.AreEqual(1, CountPathWarnings(like));
        Assert.AreEqual(1, CountPathWarnings(notLike));
        Assert.AreEqual(0, CountPathWarnings(rlike));
    }

    [TestMethod]
    public void PathColumn_ConstantLetOrigin_IsReportedAtOriginalLiteral()
    {
        const string query = @"let candidate: string = 'some\text'; select FilePath from #P.Entities() where FilePath = $candidate";
        var result = AnalyzePath(query);

        AssertNoErrors(result);
        var warning = result.Warnings.Single(static item =>
            item.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape);
        Assert.AreEqual(query.IndexOf(@"\t", StringComparison.Ordinal), warning.Span.Start);
    }

    [TestMethod]
    public void PathColumn_RootedRawAndDoubledAlternatives_DeduplicateOrStayQuiet()
    {
        var rooted = AnalyzePath(
            @"select FilePath from #P.Entities() where FilePath = 'C:\new\test'");
        var raw = AnalyzePath(
            @"select FilePath from #P.Entities() where FilePath = r'some\text'");
        var doubled = AnalyzePath(
            @"select FilePath from #P.Entities() where FilePath = 'some\\text'");
        var message = AnalyzePath(
            @"select Message from #P.Entities() where Message = 'Hello\nWorld'");

        AssertNoErrors(rooted);
        AssertNoErrors(raw);
        AssertNoErrors(doubled);
        AssertNoErrors(message);
        Assert.AreEqual(1, CountPathWarnings(rooted));
        Assert.AreEqual(0, CountPathWarnings(raw));
        Assert.AreEqual(0, CountPathWarnings(doubled));
        Assert.AreEqual(0, CountPathWarnings(message));
    }

    [TestMethod]
    public void PathNamedLetWarning_IsNotDuplicatedByColumnContext()
    {
        var result = AnalyzePath(
            @"let path: string = 'some\text'; select FilePath from #P.Entities() where FilePath = $path");

        AssertNoErrors(result);
        Assert.AreEqual(1, CountPathWarnings(result));
    }

    [TestMethod]
    public void BareStringProjection_RemainsQuiet()
    {
        var result = AnalyzePath(@"select 'some\text' from #P.Entities()");

        AssertNoErrors(result);
        Assert.AreEqual(0, CountPathWarnings(result));
    }

    private static QueryAnalysisResult AnalyzeBasic(string query)
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#A"] = []
            });
        return new QueryAnalyzer(provider).Analyze(query);
    }

    private static QueryAnalysisResult AnalyzePath(string query)
    {
        return new QueryAnalyzer(new PathSchemaProvider()).Analyze(query);
    }

    private static int CountPathWarnings(QueryAnalysisResult result)
    {
        return result.Warnings.Count(static item =>
            item.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape);
    }

    private static void AssertNoErrors(QueryAnalysisResult result)
    {
        Assert.IsFalse(result.HasErrors, string.Join(" | ", result.Diagnostics));
    }

    private sealed class PathSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new GenericSchema<PathEntity, PathEntityTable>(
                [new PathEntity()],
                new Dictionary<string, int>
                {
                    [nameof(PathEntity.FilePath)] = 0,
                    [nameof(PathEntity.Message)] = 1
                },
                new Dictionary<int, Func<PathEntity, object?>>
                {
                    [0] = entity => entity.FilePath,
                    [1] = entity => entity.Message
                });
        }
    }

    private sealed class PathEntity
    {
        public string FilePath { get; } = string.Empty;

        public string Message { get; } = string.Empty;
    }

    private sealed class PathEntityTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(PathEntity.FilePath), 0, typeof(string)),
            new SchemaColumn(nameof(PathEntity.Message), 1, typeof(string))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(PathEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }
}
