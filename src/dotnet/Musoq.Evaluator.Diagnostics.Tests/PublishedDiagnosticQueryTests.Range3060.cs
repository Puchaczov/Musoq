using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class PublishedDiagnosticQueryTests
{
    [TestMethod]
    [DataRow(
        "param(ids:object[]); select 1 from #A.Entities()",
        "param(ids:int[]); select 1 from #A.Entities()",
        DiagnosticCode.MQ3060_UnsupportedScriptParameterType,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        6,
        12,
        false,
        "",
        ""
    )]
    [DataRow(
        "param(ids:int[]=null); select 1 from #A.Entities()",
        "param(ids:int[]?=null); select 1 from #A.Entities()",
        DiagnosticCode.MQ3061_InvalidScriptParameterDefault,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        6,
        14,
        false,
        "",
        ""
    )]
    [DataRow(
        "param(k:string); let k:string='local'; select $k from #A.Entities()",
        "param(k:string); let other:string='local'; select $other from #A.Entities()",
        DiagnosticCode.MQ3063_DuplicateScriptSymbolName,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        17,
        20,
        false,
        "",
        ""
    )]
    [DataRow(
        "let n:object='v'; select 1 from #A.Entities()",
        "let n:string='v'; select $n from #A.Entities()",
        DiagnosticCode.MQ3064_UnsupportedScriptVariableType,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        0,
        16,
        false,
        "",
        ""
    )]
    [DataRow(
        "let n=41; select 1 from #A.Entities()",
        "let n:int=41; select $n from #A.Entities()",
        DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        0,
        8,
        false,
        "",
        ""
    )]
    [DataRow(
        "let n:int=$later; select 1 from #A.Entities()",
        "let later:int=1; let n:int=$later; select $n from #A.Entities()",
        DiagnosticCode.MQ3066_ScriptVariableUsedBeforeDeclaration,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        0,
        16,
        false,
        "",
        ""
    )]
    [DataRow(
        "select * rename (Name as Label,name as Other) from #A.Entities()",
        "select * rename (Name as Label,City as Other) from #A.Entities()",
        DiagnosticCode.MQ3068_StarRenameDuplicateSource,
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
        "select * rename (Name as City) from #A.Entities()",
        "select * rename (Name as Label) from #A.Entities()",
        DiagnosticCode.MQ3069_StarRenameDuplicateTarget,
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
        "select * rename (Missing as Label) from #A.Entities()",
        "select * rename (Name as Label) from #A.Entities()",
        DiagnosticCode.MQ3070_StarRenameColumnNotFound,
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
        "with r as (select 1 as n from #A.Entities() union all select n+1 from r where n<2) select n from r",
        "with recursive r as (select 1 as n from #A.Entities() union all select n+1 from r where n<2) select n from r",
        DiagnosticCode.MQ3072_RecursiveCteRequiresKeyword,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        70,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "with recursive r as (select n from r) select n from r",
        "with recursive r as (select 1 as n from values {(i:0)} v union all select n+1 as n from r where n<2) select n from r",
        DiagnosticCode.MQ3073_InvalidRecursiveCteShape,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        15,
        22,
        false,
        "",
        ""
    )]
    [DataRow(
        "with recursive r as (select n from r union all select n+1 from r where n<2) select n from r",
        "with recursive r as (select 1 as n from #A.Entities() union all select n+1 from r where n<2) select n from r",
        DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        35,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "with recursive r as (select 1 as n from #A.Entities() union all select RowNumber() over (order by n) as n from r) select n from r",
        "with recursive r as (select 1 as n from #A.Entities() union all select n+1 as n from r where n<2) select n from r",
        DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        71,
        29,
        false,
        "",
        ""
    )]
    [DataRow(
        "with recursive r as (select 1 as n from values {(i:0)} v union all select 'abc' as n from r where n<2) select n from r",
        "with recursive r as (select 1 as n from values {(i:0)} v union all select n+1 as n from r where n<2) select n from r",
        DiagnosticCode.MQ3076_RecursiveCteOutputMismatch,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        74,
        5,
        false,
        "",
        ""
    )]
    [DataRow(
        "with r (a,b) as (select Id from #A.Entities()) select 1 from r",
        "with r (a) as (select Id from #A.Entities()) select 1 from r",
        DiagnosticCode.MQ3077_CteColumnListCountMismatch,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        8,
        1,
        false,
        "",
        ""
    )]
    [DataRow(
        "with r (Name,Name) as (select City,Country from #A.Entities()) select Name from r",
        "with r (Name,Place) as (select City,Country from #A.Entities()) select Name from r",
        DiagnosticCode.MQ3078_DuplicateCteColumnName,
        DiagnosticPhase.Bind,
        DiagnosticSeverity.Error,
        DiagnosticSourceKind.Query,
        13,
        4,
        false,
        "",
        ""
    )]
    public void Range3060_ShouldLocateTheTriggerAndAcceptItsRepair(
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
