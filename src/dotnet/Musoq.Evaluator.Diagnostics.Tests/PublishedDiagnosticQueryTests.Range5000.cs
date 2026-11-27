using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "select Id from #A.Entities() where Time='01/02/2026'",
        "select Id from #A.Entities() where Time='2026-01-02'",
        DiagnosticCode.MQ5003_ImplicitTypeConversion,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        40,
        12,
        true,
        "",
        ""
    )]
    [DataRow(
        "select case when false then 'dead' else 'live' end from #A.Entities()",
        "select case when Id=1 then 'dead' else 'live' end from #A.Entities()",
        DiagnosticCode.MQ5008_UnreachableCode,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        17,
        5,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 1 from #A.Entities() where Id=Id",
        "select 1 from #A.Entities() where Id=1",
        DiagnosticCode.MQ5010_TautologicalCondition,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        34,
        5,
        true,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() where 1=2",
        "select Name from #A.Entities() where Id=2",
        DiagnosticCode.MQ5011_ContradictoryCondition,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        37,
        3,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 'C:\\new\\test' from #A.Entities()",
        "select r'C:\\new\\test' from #A.Entities()",
        DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        10,
        2,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() where Name rlike '\\bword\\b'",
        "select Name from #A.Entities() where Name rlike r'\\bword\\b'",
        DiagnosticCode.MQ5015_SuspiciousRegexEscape,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        49,
        2,
        true,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() where Name like '*.log'",
        "select Name from #A.Entities() where Name like '%.log'",
        DiagnosticCode.MQ5016_GlobWildcardInLike,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        48,
        1,
        true,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() where Name=null",
        "select Name from #A.Entities() where Name is null",
        DiagnosticCode.MQ5017_NullComparison,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        42,
        4,
        true,
        "",
        ""
    )]
    [DataRow(
        "select a.Id from #A.Entities() a left join #A.Entities() b on a.Id=b.Id where b.NullableValue is null",
        "select a.Id from #A.Entities() a left join #A.Entities() b on a.Id=b.Id where b is missing",
        DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        80,
        13,
        true,
        "",
        ""
    )]
    [DataRow(
        "select a.Id from #A.Entities() a left join #A.Entities() b on a.Id=b.Id where b.Id>0",
        "select a.Id from #A.Entities() a left join #A.Entities() b on a.Id=b.Id and b.Id>0",
        DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        80,
        4,
        true,
        "",
        ""
    )]
    public void Range5000_ShouldLocateTheTriggerAndAcceptItsRepair(
        string trigger,
        string repair,
        DiagnosticCode code,
        DiagnosticPhase phase,
        DiagnosticSeverity severity,
        DiagnosticSourceKind domain,
        int offset,
        int length,
        bool hasSuggestions,
        string argument,
        string value
    ) =>
        AssertAnalysisPair(
            trigger,
            repair,
            code,
            phase,
            severity,
            domain,
            offset,
            length,
            hasSuggestions,
            argument,
            value
        );
}
