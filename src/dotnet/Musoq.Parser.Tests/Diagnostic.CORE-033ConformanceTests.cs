using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticCore033ConformanceTests
{
    [TestMethod]
    public void FormalGrammar_RepresentativeQueries_ShouldParseWithoutDiagnostics()
    {
        var queries = new[]
        {
            "params(limit: int? = null, ids: string[]) select $limit from system.dual()",
            "let adjusted: int = (1 + 2) * 3; select $adjusted from system.dual()",
            "select distinct City, Count(*) filter (where Population > 0) as Total, " +
            "RowNumber() over (partition by City order by Population desc nulls last " +
            "rows between 1 preceding and current row) as RowNo " +
            "from #A.Entities() source where Population is not null group by City " +
            "having Count(*) > 0 window w as (partition by City order by Population " +
            "rows between unbounded preceding and current row) qualify RowNumber() over w > 0 " +
            "order by City asc nulls first skip 0 take 1",
            "from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id " +
            "cross join #C.Entities() c where a.Id > 0 group by a.Id " +
            "window w as (order by a.Id) select a.Id qualify RowNumber() over w > 0 " +
            "order by a.Id skip 0 take 1",
            "from values { { Name: 'A', Score: (1 + 2), }, " +
            "{ Name: r'B', Score: 3, }, } v select v.Name, v.Score",
            "select d.Value from (select Value from #A.Entities()) d",
            "select * from #schema.method('arg', limit: 2) source",
            "select * from SomeFunction(1, 'two') source",
            "select b.Ordinal from schema.first() a cross apply a.Values b with ordinality",
            "select e.Id, s.State from schema.events() e asof left outer join " +
            "schema.snapshots() s on e.Time >= s.Time tie break by s.Time desc nulls last",
            "pivot #sales.orders() on Quarter in ('Q1' as Q1, 'Q2') " +
            "using Sum(Amount) as Total group by ALL order by Quarter skip 0 take 1",
            "unpivot #sales.wide() on Quarter in (Q1 as Q1, Q2) using Sales " +
            "keep Region as Group order by Group skip 0 take 1",
            "select Id from schema.left() l union (Id) select Id from schema.right() r " +
            "order by Id skip 0 take 1",
            "table T { Id: int, Name: string?, }; couple schema.method with table T " +
            "and settings prod as Source; select Id from Source()",
            "desc query (with cte as (select Name from schema.method()) select Name from cte)",
            "select (1 + 2 * 3) ?? 0, Name like 'A%', Name rlike r'^[A-Z]', " +
            "Name between 'A' and 'Z', Name contains ('a', 'b'), " +
            "Name is distinct from null, any(Name, Message) like '%error%', " +
            "all(Name, Message) rlike 'error.*', case Code when 1 then 'one' else 'other' end " +
            "from schema.method()"
        };

        foreach (var query in queries)
        {
            var result = ParseWithDiagnostics(query);

            Assert.IsTrue(result.Success, query + Environment.NewLine + result.FormatDiagnostics());
            Assert.IsEmpty(result.Diagnostics, query + Environment.NewLine + result.FormatDiagnostics());
        }
    }

    [TestMethod]
    public void DiagnosticCatalog_EachCode_ShouldExposePhaseMetadataAndDefaultActions()
    {
        var codes = Enum.GetValues<DiagnosticCode>().Distinct().ToArray();
        var descriptors = DiagnosticDescriptorRegistry.All.ToDictionary(static descriptor => descriptor.Code);

        Assert.HasCount(codes.Length, descriptors);

        foreach (var code in codes)
        {
            Assert.IsTrue(descriptors.TryGetValue(code, out var descriptor), $"Missing descriptor for {code}.");
            Assert.IsNotNull(descriptor);
            Assert.AreEqual(code, descriptor!.Code);
            Assert.AreEqual(DiagnosticPhaseMapping.FromCode(code), descriptor.DefaultPhase, code.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(descriptor.MessageTemplate), code.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(descriptor.Explanation), code.ToString());
            Assert.IsFalse(string.IsNullOrWhiteSpace(descriptor.DocsReference), code.ToString());
            Assert.HasCount(descriptor.SuggestedFixes.Count, descriptor.DefaultActions, code.ToString());

            for (var index = 0; index < descriptor.DefaultActions.Count; index++)
            {
                var action = descriptor.DefaultActions[index];
                Assert.AreEqual(DiagnosticActionKind.Suggestion, action.Kind, code.ToString());
                Assert.IsNull(action.TextEdit, code.ToString());
                Assert.AreEqual(descriptor.SuggestedFixes[index], action.Title, code.ToString());
            }
        }
    }

    [TestMethod]
    public void GrammarNearMisses_ShouldReportOneStructuredParseDiagnosticAtTheOffendingSpan()
    {
        AssertParseDiagnostic(
            "select * exclud (Name) from #some.entities()",
            DiagnosticCode.MQ2001_UnexpectedToken,
            new TextSpan(9, 6));

        const string invalidNullOrdering = "select Name from #A.entities() order by City nulls middle";
        AssertParseDiagnostic(
            invalidNullOrdering,
            DiagnosticCode.MQ2009_InvalidOrderByExpression,
            new TextSpan(invalidNullOrdering.IndexOf("middle", StringComparison.Ordinal), "middle".Length));

        const string invalidSettings = "desc settings #schema";
        AssertParseDiagnostic(
            invalidSettings,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            new TextSpan(invalidSettings.IndexOf("#schema", StringComparison.Ordinal), "#schema".Length));

        const string emptyPredicate = "select 1 from #test.rows() where Name in ()";
        AssertParseDiagnostic(
            emptyPredicate,
            DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed,
            new TextSpan(emptyPredicate.IndexOf(')', emptyPredicate.IndexOf("in", StringComparison.Ordinal)), 1));

        const string unsupportedCommand = "explain select 1 from #test.rows()";
        AssertParseDiagnostic(
            unsupportedCommand,
            DiagnosticCode.MQ2040_InvalidDiagnosticCommand,
            new TextSpan(unsupportedCommand.IndexOf("select", StringComparison.Ordinal), "select".Length));
    }

    [TestMethod]
    public void WindowFrameOffsetOutsideTheNodeRange_ShouldUseNumericLiteralDiagnostic()
    {
        const string offset = "2147483648";
        var query = $"select Sum(Value) over (order by Value rows {offset} preceding) from #A.Entities()";
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(DiagnosticCode.MQ1009_NumericLiteralOutOfRange, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(new TextSpan(query.IndexOf(offset, StringComparison.Ordinal), offset.Length), diagnostic.Span);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
    }

    private static void AssertParseDiagnostic(string query, DiagnosticCode expectedCode, TextSpan expectedSpan)
    {
        var result = ParseWithDiagnostics(query);

        Assert.IsFalse(result.Success, result.FormatDiagnostics());
        Assert.HasCount(1, result.Diagnostics, result.FormatDiagnostics());

        var diagnostic = result.Diagnostics.Single();
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.IsTrue(diagnostic.Location.IsValid);
        Assert.IsTrue(diagnostic.EndLocation.IsValid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.ContextSnippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);
        Assert.IsTrue(diagnostic.SuggestedFixes.All(static action => !string.IsNullOrWhiteSpace(action.Title)));
    }

    private static ParseResult ParseWithDiagnostics(string query)
    {
        var lexer = new Lexer(query, true, recoverOnError: true);
        return new Parser(lexer, lexer.Diagnostics).ParseWithDiagnostics();
    }
}
