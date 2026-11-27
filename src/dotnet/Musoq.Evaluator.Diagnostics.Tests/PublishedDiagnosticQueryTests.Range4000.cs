using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "binary Broken { Value: byte, Value: byte }; select 1 from #A.Entities()",
        "binary Fixed { Value: byte, Tail: byte }; select 1 from #A.Entities()",
        DiagnosticCode.MQ4008_DuplicateSchemaField,
        DiagnosticPhase.Schema,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Schema,
        29,
        5,
        true,
        "",
        ""
    )]
    public void Range4000_ShouldLocateTheTriggerAndAcceptItsRepair(
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
