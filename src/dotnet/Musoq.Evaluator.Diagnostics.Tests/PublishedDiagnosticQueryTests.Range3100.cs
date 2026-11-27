using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "select Sum(RowNumber() over (order by Id)) over (order by Id) from #A.Entities()",
        "select Sum(Id) over (order by Id) from #A.Entities()",
        DiagnosticCode.MQ3100_NestedWindowFunction,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        54,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() where RowNumber() over (order by Id)=1",
        "select Name from #A.Entities() qualify RowNumber() over (order by Id)=1",
        DiagnosticCode.MQ3101_WindowFunctionInFilter,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        37,
        30,
        false,
        "",
        ""
    )]
    [DataRow(
        "select * from Source(); table MyTable {Name:string}; couple #A.Entities with table MyTable as Source",
        "table MyTable {Name:string}; couple #A.Entities with table MyTable as Source; select * from Source()",
        DiagnosticCode.MQ3102_InvalidStatementOrder,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        24,
        27,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Ntile(0) over (order by Id) from #A.Entities()",
        "select Ntile(1) over (order by Id) from #A.Entities()",
        DiagnosticCode.MQ3103_InvalidWindowFunctionArgument,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        13,
        1,
        true,
        "",
        ""
    )]
    [DataRow(
        "select RowNumber() over missing from #A.Entities()",
        "select RowNumber() over w from #A.Entities() window w as (order by Id)",
        DiagnosticCode.MQ3104_UnknownNamedWindow,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        24,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() window w as (order by Id),w as (order by City)",
        "select Name from #A.Entities() window w as (order by Id)",
        DiagnosticCode.MQ3105_DuplicateNamedWindow,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        57,
        20,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Self.ToString() as Text from #A.Entities()",
        "select Name as Text from #A.Entities()",
        DiagnosticCode.MQ3117_UnsupportedColumnInstanceMethod,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        12,
        8,
        false,
        "",
        ""
    )]
    public void Range3100_ShouldLocateTheTriggerAndAcceptItsRepair(
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
