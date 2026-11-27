using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "select XYZ from #A.Entities()",
        "select Id from #A.Entities()",
        DiagnosticCode.MQ3001_UnknownColumn,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        3,
        true,
        "column",
        "XYZ"
    )]
    [DataRow(
        "select Id from values {(Id:7)} l cross join values {(Id:'seven')} r",
        "select l.Id from values {(Id:7)} l cross join values {(Id:'seven')} r",
        DiagnosticCode.MQ3002_AmbiguousColumn,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        2,
        true,
        "column",
        "Id"
    )]
    [DataRow(
        "select Name from #A.Entities() where Name",
        "select Name from #A.Entities() where Name='Alice'",
        DiagnosticCode.MQ3005_TypeMismatch,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        37,
        4,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Name+5 from #A.Entities()",
        "select Id+5 from #A.Entities()",
        DiagnosticCode.MQ3007_InvalidOperandTypes,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        6,
        false,
        "",
        ""
    )]
    [DataRow(
        "desc #nonexistent.table()",
        "desc #A.Entities()",
        DiagnosticCode.MQ3010_UnknownSchema,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        23,
        2,
        true,
        "schema",
        "#nonexistent"
    )]
    [DataRow(
        "select Id from #A.Entities() where Count(Id)>0",
        "select Id,Count(Id) from #A.Entities() group by Id having Count(Id)>0",
        DiagnosticCode.MQ3011_AggregateNotAllowed,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        35,
        5,
        true,
        "",
        ""
    )]
    [DataRow(
        "select Name,Count(1) from #A.Entities()",
        "select Name,Count(1) from #A.Entities() group by Name",
        DiagnosticCode.MQ3012_NonAggregateInSelect,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        4,
        false,
        "",
        ""
    )]
    [DataRow(
        "select x.Name from #A.Entities() a",
        "select a.Name from #A.Entities() a",
        DiagnosticCode.MQ3015_UnknownAlias,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        1,
        true,
        "alias",
        "x"
    )]
    [DataRow(
        "with l as (select n from r), r as (select n from l) select n from l",
        "with l as (select 1 as n from #A.Entities()), r as (select n from l) select n from r",
        DiagnosticCode.MQ3016_CircularReference,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        0,
        4,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Self[0] from #A.Entities()",
        "select Name[0] from #A.Entities()",
        DiagnosticCode.MQ3017_ObjectNotArray,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        7,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Name['test'] from #A.Entities()",
        "select Name[0] from #A.Entities()",
        DiagnosticCode.MQ3018_NoIndexer,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        12,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() union select Name,City from #A.Entities()",
        "select Name from #A.Entities() union select City from #A.Entities()",
        DiagnosticCode.MQ3019_SetOperatorColumnCount,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        44,
        9,
        false,
        "",
        ""
    )]
    public void Range3000_ShouldLocateTheTriggerAndAcceptItsRepair(
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
