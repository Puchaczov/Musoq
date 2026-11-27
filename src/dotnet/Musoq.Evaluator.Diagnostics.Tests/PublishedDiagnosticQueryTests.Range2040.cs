using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "profile table temp {Name:string}",
        "table temp {Name:string}; select Name from #A.Entities()",
        DiagnosticCode.MQ2040_InvalidDiagnosticCommand,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        8,
        5,
        true,
        "",
        ""
    )]
    [DataRow(
        "select * like 'N%' like '%e' from #A.Entities()",
        "select * like 'N%' from #A.Entities()",
        DiagnosticCode.MQ2041_InvalidStarModifierOrder,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        19,
        4,
        true,
        "",
        ""
    )]
    [DataRow(
        "enum",
        "enum State : int {A=1}; select 1 from #A.Entities()",
        DiagnosticCode.MQ2042_InvalidEnumDeclaration,
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
        "enum State : string {A=1}; select 1 from #A.Entities()",
        "enum State : int {A=1}; select 1 from #A.Entities()",
        DiagnosticCode.MQ2043_InvalidEnumBackingType,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        13,
        6,
        true,
        "",
        ""
    )]
    [DataRow(
        "enum State : int {A}; select 1 from #A.Entities()",
        "enum State : int {A=1}; select 1 from #A.Entities()",
        DiagnosticCode.MQ2044_MissingEnumMemberValue,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        18,
        1,
        true,
        "",
        ""
    )]
    [DataRow(
        "enum State : int {A=1,A=2}; select 1 from #A.Entities()",
        "enum State : int {A=1,B=2}; select 1 from #A.Entities()",
        DiagnosticCode.MQ2045_DuplicateEnumMember,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        22,
        1,
        true,
        "",
        ""
    )]
    [DataRow(
        "enum State : byte {A=256}; select 1 from #A.Entities()",
        "enum State : byte {A=255}; select 1 from #A.Entities()",
        DiagnosticCode.MQ2046_EnumMemberValueOutOfRange,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        21,
        3,
        true,
        "",
        ""
    )]
    [DataRow(
        "enum State : int {}; select 1 from #A.Entities()",
        "enum State : int {A=1}; select 1 from #A.Entities()",
        DiagnosticCode.MQ2047_EmptyEnumDeclaration,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        18,
        1,
        true,
        "",
        ""
    )]
    [DataRow(
        "enum('Ready','Done');",
        "enum State : int {Ready=1,Done=2}; select 1 from #A.Entities()",
        DiagnosticCode.MQ2048_UnsupportedEnumSyntax,
        DiagnosticPhase.Parse,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        0,
        4,
        true,
        "",
        ""
    )]
    public void Range2040_ShouldLocateTheTriggerAndAcceptItsRepair(
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
