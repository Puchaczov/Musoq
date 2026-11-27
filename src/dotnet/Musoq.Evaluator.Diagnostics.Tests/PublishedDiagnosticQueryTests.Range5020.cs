using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "select Name from #A.Entities() skip 2",
        "select Name from #A.Entities() order by Name skip 2",
        DiagnosticCode.MQ5021_UnorderedSkip,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        36,
        1,
        true,
        "",
        ""
    )]
    [DataRow(
        "with dead as (select Name from #A.Entities()) select Name from #A.Entities()",
        "with live as (select Name from #A.Entities()) select Name from live",
        DiagnosticCode.MQ5022_UnusedCte,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        5,
        4,
        true,
        "",
        ""
    )]
    [DataRow(
        "let dead: int=1; select Name from #A.Entities()",
        "let dead: int=1; select $dead from #A.Entities()",
        DiagnosticCode.MQ5023_UnusedScriptVariable,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        4,
        4,
        true,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() where Name not in ('Alice',null)",
        "select Name from #A.Entities() where Name not in ('Alice','Bob')",
        DiagnosticCode.MQ5024_NullSensitiveNotIn,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        58,
        4,
        true,
        "",
        ""
    )]
    [DataRow(
        "select Id from #A.Entities() where Time='02/31/2026'",
        "select Id from #A.Entities() where Time='2026-02-28'",
        DiagnosticCode.MQ5025_ImpossibleImplicitConversion,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Warning,
        DiagnosticSourceKind.Query,
        40,
        12,
        true,
        "",
        ""
    )]
    public void Range5020_ShouldLocateTheTriggerAndAcceptItsRepair(
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
