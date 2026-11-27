using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "select * exclude (Missing) from #A.Entities()",
        "select * exclude (City) from #A.Entities()",
        DiagnosticCode.MQ3041_StarExcludeColumnNotFound,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select * replace (1 as Missing) from #A.Entities()",
        "select * replace (1 as Id) from #A.Entities()",
        DiagnosticCode.MQ3042_StarReplaceColumnNotFound,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select * exclude (x) from values {(x:1)} v",
        "select * exclude (y) from values {(x:1,y:2)} v",
        DiagnosticCode.MQ3043_StarExcludeRemovesAllColumns,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select * exclude (City) replace (1 as city) from #A.Entities()",
        "select * exclude (City) replace (1 as Id) from #A.Entities()",
        DiagnosticCode.MQ3044_StarColumnInBothExcludeAndReplace,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select * like 'Z%' from #A.Entities()",
        "select * like 'N%' from #A.Entities()",
        DiagnosticCode.MQ3045_StarLikeMatchedNoColumns,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select * exclude (Name,name) from #A.Entities()",
        "select * exclude (Name,City) from #A.Entities()",
        DiagnosticCode.MQ3046_StarExcludeDuplicateColumn,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select * replace (1 as Name,2 as name) from #A.Entities()",
        "select * replace (1 as Name,2 as Id) from #A.Entities()",
        DiagnosticCode.MQ3047_StarReplaceDuplicateColumn,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select * like 'N%' replace (1 as City) from #A.Entities()",
        "select * like 'N%' replace (1 as Name) from #A.Entities()",
        DiagnosticCode.MQ3048_StarReplaceTargetsRemovedColumn,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select a.Id from #A.Entities() a where a.Id in (select b.Id,b.City from #A.Entities() b)",
        "select a.Id from #A.Entities() a where a.Id in (select b.Id from #A.Entities() b)",
        DiagnosticCode.MQ3049_InSubqueryMultipleColumns,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        41,
        47,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Name from #A.Entities() qualify Name='Alice'",
        "select Name from #A.Entities() where Name='Alice'",
        DiagnosticCode.MQ3050_QualifyRequiresWindowFunction,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        39,
        12,
        false,
        "",
        ""
    )]
    [DataRow(
        "select ToUpper(Name) filter (where Name='Alice') from #A.Entities()",
        "select Count(Name) filter (where Name='Alice') from #A.Entities()",
        DiagnosticCode.MQ3051_FilterOnNonAggregate,
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
        "select Sum(Id) over (range between unbounded preceding and current row) from #A.Entities()",
        "select Sum(Id) over (order by Id range between unbounded preceding and current row) from #A.Entities()",
        DiagnosticCode.MQ3052_RangeFrameRequiresOrderBy,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        20,
        51,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Sum(Id) over (order by Id rows between unbounded following and current row) from #A.Entities()",
        "select Sum(Id) over (order by Id rows between unbounded preceding and current row) from #A.Entities()",
        DiagnosticCode.MQ3053_InvalidWindowFrameBounds,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        20,
        62,
        false,
        "",
        ""
    )]
    [DataRow(
        "select x from values {(x:1)} a where x in (select * exclude (y) from values {(z:1,y:2)} b)",
        "select x from values {(x:1)} a where x in (select z from values {(z:1,y:2)} b)",
        DiagnosticCode.MQ3054_StarModifierInInSubquery,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        37,
        53,
        false,
        "",
        ""
    )]
    [DataRow(
        "param(xs:int[]) select * from values {(x:$xs)} v",
        "param(xs:int) select * from values {(x:$xs)} v",
        DiagnosticCode.MQ3055_InvalidValuesSource,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        41,
        3,
        false,
        "",
        ""
    )]
    [DataRow(
        "param(a:string); param(b:int); select 1 from #A.Entities()",
        "param(a:string,b:int); select 1 from #A.Entities()",
        DiagnosticCode.MQ3056_DuplicateScriptParameterBlock,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        17,
        12,
        false,
        "",
        ""
    )]
    [DataRow(
        "let n:int=1; param(a:string); select $n from #A.Entities()",
        "param(a:string); let n:int=1; select $n from #A.Entities()",
        DiagnosticCode.MQ3057_ScriptParameterBlockAfterStatement,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        13,
        15,
        false,
        "",
        ""
    )]
    [DataRow(
        "param(a:string,a:int); select 1 from #A.Entities()",
        "param(a:string,b:int); select 1 from #A.Entities()",
        DiagnosticCode.MQ3058_DuplicateScriptParameterName,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        15,
        5,
        false,
        "",
        ""
    )]
    [DataRow(
        "select $missing from #A.Entities()",
        "param(missing:int) select $missing from #A.Entities()",
        DiagnosticCode.MQ3059_UndeclaredScriptParameter,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        7,
        8,
        false,
        "",
        ""
    )]
    public void Range3040_ShouldLocateTheTriggerAndAcceptItsRepair(
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
