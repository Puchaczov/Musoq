using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "select @ from #A.Entities()",
        "select Id from #A.Entities()",
        DiagnosticCode.MQ1001_UnknownToken,
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
        "select 'unclosed",
        "select 'closed' from #A.Entities()",
        DiagnosticCode.MQ1002_UnterminatedString,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        9,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 1..2 from #A.Entities()",
        "select 1.2 from #A.Entities()",
        DiagnosticCode.MQ1003_InvalidNumericLiteral,
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
        "select '\\x1' from #A.Entities()",
        "select '\\x41' from #A.Entities()",
        DiagnosticCode.MQ1004_InvalidEscapeSequence,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        8,
        3,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 1 from #A.Entities() /* open",
        "select 1 from #A.Entities() /* closed */",
        DiagnosticCode.MQ1005_UnterminatedBlockComment,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        28,
        7,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 0x from #A.Entities()",
        "select 0xF from #A.Entities()",
        DiagnosticCode.MQ1006_InvalidHexNumber,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        2,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 0b from #A.Entities()",
        "select 0b1 from #A.Entities()",
        DiagnosticCode.MQ1007_InvalidBinaryNumber,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        2,
        true,
        "",
        ""
    )]
    [DataRow(
        "select 0o89 from #A.Entities()",
        "select 0o67 from #A.Entities()",
        DiagnosticCode.MQ1008_InvalidOctalNumber,
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
        "select 128b from #A.Entities()",
        "select 127b from #A.Entities()",
        DiagnosticCode.MQ1009_NumericLiteralOutOfRange,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        4,
        true,
        "",
        ""
    )]
    public void Range1000_ShouldLocateTheTriggerAndAcceptItsRepair(
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
