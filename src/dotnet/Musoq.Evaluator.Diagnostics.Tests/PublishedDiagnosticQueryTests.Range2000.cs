using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "desc",
        "desc #A.Entities()",
        DiagnosticCode.MQ2001_UnexpectedToken,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        4,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 1 from #A.Entities() where Id between 0",
        "select 1 from #A.Entities() where Id between 0 and 4",
        DiagnosticCode.MQ2002_MissingToken,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        46,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "select * like from #A.Entities()",
        "select * like 'N%' from #A.Entities()",
        DiagnosticCode.MQ2003_InvalidExpression,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        14,
        4,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 1",
        "select 1 from #A.Entities()",
        DiagnosticCode.MQ2004_MissingFromClause,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        8,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "select",
        "select Id from #A.Entities()",
        DiagnosticCode.MQ2005_InvalidSelectList,
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
        "select Name from #A.Entities() group by",
        "select Name from #A.Entities() group by Name",
        DiagnosticCode.MQ2006_MissingGroupByColumn,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        39,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "select a.Id from #A.Entities() a inner join #A.Entities() b",
        "select a.Id from #A.Entities() a inner join #A.Entities() b on a.Id=b.Id",
        DiagnosticCode.MQ2007_InvalidJoinCondition,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        59,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "unpivot #A.Entities() a on Col in (Id as Id) using Col",
        "unpivot #A.Entities() a on Col in (Id as Id) using Val",
        DiagnosticCode.MQ2008_DuplicateAlias,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        51,
        3,
        true,
        "",
        ""
    )]
    [DataRow(
        "select Id from #A.Entities() order by Id only",
        "select Id from #A.Entities() order by Id",
        DiagnosticCode.MQ2009_InvalidOrderByExpression,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        41,
        4,
        true,
        "",
        ""
    )]
    [DataRow(
        "select (1 + 2 from #A.Entities()",
        "select (1 + 2) from #A.Entities()",
        DiagnosticCode.MQ2010_MissingClosingParenthesis,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        14,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "select [Name from #A.Entities()",
        "select [Name] from #A.Entities()",
        DiagnosticCode.MQ2011_MissingClosingBracket,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        24,
        true,
        "",
        ""
    )]
    [DataRow(
        "table Empty {}; select 1 from #A.Entities()",
        "table Items {Id: int}; select 1 from #A.Entities()",
        DiagnosticCode.MQ2012_InvalidSchemaDefinition,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        0,
        14,
        false,
        "",
        ""
    )]
    [DataRow(
        "with c (1) as (select 1 as X from #A.Entities()) select X from c",
        "with c (Y) as (select 1 as X from #A.Entities()) select Y from c",
        DiagnosticCode.MQ2013_InvalidCTE,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        9,
        1,
        true,
        "",
        ""
    )]
    [DataRow(
        "select Name, from #A.Entities()",
        "select Name from #A.Entities()",
        DiagnosticCode.MQ2014_TrailingComma,
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
        "select ,Name from #A.Entities()",
        "select Name from #A.Entities()",
        DiagnosticCode.MQ2015_LeadingComma,
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
        "param()",
        "param() select Id from #A.Entities()",
        DiagnosticCode.MQ2016_IncompleteStatement,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "table Jobs {",
        "table Jobs {Id:int}; select Id from #A.Entities()",
        DiagnosticCode.MQ2017_UnexpectedEndOfFile,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        12,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 5 5 from #A.Entities()",
        "select 5+5 from #A.Entities()",
        DiagnosticCode.MQ2018_MissingOperator,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        9,
        0,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 1 from #A.Entities() where 1 + * 2",
        "select 1 from #A.Entities() where Id + 1 > 2",
        DiagnosticCode.MQ2019_InvalidOperator,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        38,
        1,
        true,
        "",
        ""
    )]
    public void Range2000_ShouldLocateTheTriggerAndAcceptItsRepair(
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
