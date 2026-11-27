using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "select Name from #A.Entities() union (Name) select 1 as Name from #A.Entities()",
        "select Name from #A.Entities() union (Name) select City as Name from #A.Entities()",
        DiagnosticCode.MQ3020_SetOperatorColumnTypes,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        51,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select 1 from #A.Entities() a inner join #A.Entities() a on 1=1",
        "select 1 from #A.Entities() a inner join #A.Entities() b on a.Id=b.Id",
        DiagnosticCode.MQ3021_DuplicateAlias,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        55,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Length() from #A.Entities() a cross join #A.Entities() b",
        "select a.Length(a.Name) from #A.Entities() a cross join #A.Entities() b",
        DiagnosticCode.MQ3022_MissingAlias,
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
        "select * from R r",
        "with R as (select Id from #A.Entities()) select * from R r",
        DiagnosticCode.MQ3023_TableNotDefined,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        14,
        3,
        true,
        "table",
        "R"
    )]
    [DataRow(
        "select City,Count(*) from #A.Entities() group by 3",
        "select City,Count(*) from #A.Entities() group by 1",
        DiagnosticCode.MQ3024_GroupByIndexOutOfRange,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        49,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Id from #A.Entities() a cross apply a.Id b",
        "select a.Id from #A.Entities() a",
        DiagnosticCode.MQ3025_ColumnMustBeArray,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        48,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "select Self from #A.Entities()",
        "select Name from #A.Entities()",
        DiagnosticCode.MQ3027_InvalidExpressionType,
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
        "select null.Something from #A.Entities()",
        "select null from #A.Entities()",
        DiagnosticCode.MQ3028_UnknownProperty,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        12,
        9,
        true,
        "property",
        "Something"
    )]
    [DataRow(
        "select a.Id from #A.Entities() a asof join #A.Entities() b on a.Id=b.Id",
        "select a.Id from #A.Entities() a asof join #A.Entities() b on a.Id>=b.Id",
        DiagnosticCode.MQ3036_AsOfJoinMissingInequality,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        64,
        7,
        false,
        "",
        ""
    )]
    [DataRow(
        "select a.Id from #A.Entities() a asof join #A.Entities() b on a.Id>=b.Id and a.Population<=b.Population",
        "select a.Id from #A.Entities() a asof join #A.Entities() b on a.Id>=b.Id and a.Population=b.Population",
        DiagnosticCode.MQ3037_AsOfJoinMultipleInequalities,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        64,
        39,
        false,
        "",
        ""
    )]
    [DataRow(
        "select a.Id from #A.Entities() a asof join #A.Entities() b on a.Id>=b.Id or a.Population=b.Population",
        "select a.Id from #A.Entities() a asof join #A.Entities() b on a.Id>=b.Id and a.Population=b.Population",
        DiagnosticCode.MQ3038_AsOfJoinOrNotSupported,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        64,
        37,
        false,
        "",
        ""
    )]
    [DataRow(
        "select a.n from values {(n:1,m:2)} a asof join values {(n:1,m:2)} b on a.n>=a.m",
        "select a.n from values {(n:1,m:2)} a asof join values {(n:1,m:2)} b on a.n>=b.m",
        DiagnosticCode.MQ3039_AsOfJoinInequalityMustReferenceBothSides,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        73,
        6,
        false,
        "",
        ""
    )]
    public void Range3020_ShouldLocateTheTriggerAndAcceptItsRepair(
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
