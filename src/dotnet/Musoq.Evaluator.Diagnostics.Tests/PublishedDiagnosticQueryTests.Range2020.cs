using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "select ^ from #A.Entities()",
        "select 1 ^ 2 from #A.Entities()",
        DiagnosticCode.MQ2020_MissingOperand,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        1,
        true,
        "",
        ""
    )]
    [DataRow(
        "select * from #A.Entities(",
        "select * from #A.Entities()",
        DiagnosticCode.MQ2021_UnclosedFunctionCall,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        26,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "select Name as 1 from #A.Entities()",
        "select Name as Label from #A.Entities()",
        DiagnosticCode.MQ2022_InvalidAlias,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        15,
        1,
        true,
        "token",
        "1"
    )]
    [DataRow(
        "desc query (1+2)",
        "desc query (select 1+2 from #A.Entities())",
        DiagnosticCode.MQ2024_InvalidSubquery,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        11,
        1,
        true,
        "",
        ""
    )]
    [DataRow(
        "1 from #A.Entities()",
        "select 1 from #A.Entities()",
        DiagnosticCode.MQ2025_MissingSelectKeyword,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        0,
        1,
        true,
        "",
        ""
    )]
    [DataRow(
        "select case, [order] from #A.Entities()",
        "select case when Id=1 then Name else City end from #A.Entities()",
        DiagnosticCode.MQ2027_MissingWhenClause,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        4,
        true,
        "",
        ""
    )]
    [DataRow(
        "with p as (",
        "with p as (select Id from #A.Entities()) select Id from p",
        DiagnosticCode.MQ2030_UnsupportedSyntax,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        11,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "param(",
        "param(n:int) select $n from #A.Entities()",
        DiagnosticCode.MQ2031_InvalidScriptParameterDeclaration,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        6,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "param([string]$author) select 1 from #A.Entities()",
        "param(author:string) select $author from #A.Entities()",
        DiagnosticCode.MQ2032_UnsupportedScriptParameterSyntax,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        6,
        15,
        true,
        "",
        ""
    )]
    [DataRow(
        "let topic: string",
        "let topic: string='hi'; select $topic from #A.Entities()",
        DiagnosticCode.MQ2033_InvalidScriptVariableDeclaration,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        4,
        5,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 1 from Data(pageSize: 2);",
        "select 1 from #A.Entities()",
        DiagnosticCode.MQ2034_InvalidNamedSourceArgument,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        18,
        13,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 1 from #A.Entities() as",
        "select 1 from #A.Entities() as a",
        DiagnosticCode.MQ2035_MissingRequiredAlias,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        30,
        0,
        true,
        "aliasKind",
        "missing-after-as"
    )]
    [DataRow(
        "select Id from #A.Entities() where Id in ()",
        "select Id from #A.Entities() where Id in (1)",
        DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        42,
        1,
        true,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() take",
        "select Name from #A.Entities() take 2",
        DiagnosticCode.MQ2038_InvalidSliceCount,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        35,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "select a.Id from #A.Entities() a inner join #A.Entities() b on a.Id=b.Id tie break by b.Id",
        "select a.Id from #A.Entities() a inner join #A.Entities() b on a.Id=b.Id",
        DiagnosticCode.MQ2039_TieBreakRequiresAsOfJoin,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        73,
        3,
        true,
        "",
        ""
    )]
    public void Range2020_ShouldLocateTheTriggerAndAcceptItsRepair(
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
