using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "select Name from #A.Entity()",
        "select Name from #A.Entities()",
        DiagnosticCode.MQ3085_UnknownSource,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        20,
        6,
        true,
        "schema",
        "#A"
    )]
    [DataRow(
        "select MyFunc(Name) from #A.Entities()",
        "select Length(Name) from #A.Entities()",
        DiagnosticCode.MQ3086_UnknownCallable,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        6,
        true,
        "callable",
        "MyFunc"
    )]
    [DataRow(
        "select Substring() from #A.Entities()",
        "select Substring(Name,1) from #A.Entities()",
        DiagnosticCode.MQ3087_InvalidCallableArity,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        9,
        true,
        "callable",
        "Substring"
    )]
    [DataRow(
        "select Avg(Name) from #A.Entities()",
        "select Avg(Id) from #A.Entities()",
        DiagnosticCode.MQ3088_NoMatchingCallableOverload,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        3,
        true,
        "callable",
        "Avg"
    )]
    [DataRow(
        "select ToString(null) from #A.Entities()",
        "select ToString(1) from #A.Entities()",
        DiagnosticCode.MQ3089_AmbiguousCallableOverload,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        -1,
        0,
        false,
        "methodName",
        "ToString"
    )]
    [DataRow(
        "select Id::text from #A.Entities()",
        "select Id::string from #A.Entities()",
        DiagnosticCode.MQ3090_UnsupportedCastTarget,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        8,
        false,
        "",
        ""
    )]
    [DataRow(
        "select 'not-a-number'::int from #A.Entities()",
        "select '42'::int from #A.Entities()",
        DiagnosticCode.MQ3091_InvalidConstantCast,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        14,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Count(Name) as cnt from #A.Entities() group by cnt",
        "select Name,Count(Name) from #A.Entities() group by Name",
        DiagnosticCode.MQ3092_AggregateInGroupBy,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        54,
        3,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() order by 1",
        "select Name from #A.Entities() order by Name",
        DiagnosticCode.MQ3093_OrderByOrdinalUnsupported,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        40,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() where Name rlike '['",
        "select Name from #A.Entities() where Name rlike '[ab]'",
        DiagnosticCode.MQ3094_InvalidConstantRegex,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        48,
        3,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Dictionary[key] from #A.Entities()",
        "select Name[0] from #A.Entities()",
        DiagnosticCode.MQ3096_UnsupportedVariableKeyAccess,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        -1,
        0,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Sum(Id) over (order by Name range between 1 preceding and current row) from #A.Entities()",
        "select Sum(Id) over (order by Id range between 1 preceding and current row) from #A.Entities()",
        DiagnosticCode.MQ3098_InvalidRangeFrameOrderKey,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        20,
        57,
        false,
        "",
        ""
    )]
    [DataRow(
        "select RowNumber() over () from #A.Entities()",
        "select RowNumber() over (order by Id) from #A.Entities()",
        DiagnosticCode.MQ3099_WindowOrderByRequired,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        24,
        2,
        false,
        "",
        ""
    )]
    public void Range3080_ShouldLocateTheTriggerAndAcceptItsRepair(
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
