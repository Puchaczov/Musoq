using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class JoinRowPresencePredicateTests
{
    // Candidate: SPECSTRESS-2026-09-29-P4-0035
    // Specification: ["2.5","7.6","8.2","8.4","8.9","14.1-14.3","14.8","23.5","24.4"]
    // Historical fixture: {"provider":"SpecStressAliasedCoupledSchemaProvider","schema_registration_key":"#A","source_method":"Entities","source_type":"SpecStressAliasedCoupledSource","left_source":{"alias":"leftRow","arguments":[true,"Mira","Gdansk"],"emitted_rows":1},"right_source":{"alias":"rightRow","arguments":[false,"unused","unused"],"emitted_rows":0},"cte_name":"MissingSide","outer_from_alias":"projected","referenced_source_columns":["Name"],"like_pattern":"*.log","adversarial_dimensions":["The constant glob-like LIKE pattern is inside the LEFT JOIN ON expression.","The right source is empty, so the preserved left row is classified with rightRow IS MISSING.","The CTE exports explicit names that the outer query reads only through its own alias."]}
    // Historical expectation: {"diagnostic_build_succeeded":true,"warnings":[{"code":"MQ5016_GlobWildcardInLike","severity":"Warning","phase":"Bind","count":1}],"forbidden_warnings":[{"code":"MQ5019_NullRejectingOuterJoinFilter","count":0,"reason":"The null-sensitive predicate is in LEFT JOIN ON, not WHERE."}],"execution_succeeded":true,"warning_api_noninterference":true,"column_names":["Name","RightMissing"],"rows":[["Mira",true]],"row_count":1,"row_ordering":"The left source emits exactly one row and the right source emits none."}
    // Observed failure: Expected row [Mira,true], observed [Mira,false] while the left source row was preserved.
    // Regression restored by P3-0058 struct presence lowering repair (68bff5fd0).
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0035")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void Current_P4_0035_JoinOnWarningDoesNotRejectMissingRow()
    {
        const string query = "with MissingSide as (\r\n    select leftRow.Name as Name, rightRow is missing as RightMissing\r\n    from #A.Entities(true, 'Mira', 'Gdansk') leftRow\r\n    left outer join #A.Entities(false, 'unused', 'unused') rightRow\r\n        on leftRow.Name = rightRow.Name or rightRow.Name like '*.log'\r\n)\r\nselect projected.Name as Name, projected.RightMissing as RightMissing\r\nfrom MissingSide projected";
        var provider = new SpecStressAliasedCoupledSchemaProvider();
        var build = InstanceCreator.CompileWithDiagnostics(query, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        using var diagnosticCompiled = build.CompiledQuery;
        Assert.IsTrue(build.Succeeded);
        Assert.AreEqual(1, build.Warnings.Count(warning => warning.CodeString == "MQ5016"));
        Assert.IsTrue(build.Warnings.Single(warning => warning.CodeString == "MQ5016" ).IsWarning);
        Assert.AreEqual(0, build.Warnings.Count(warning => warning.CodeString == "MQ5019"));
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), provider, LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "Mira", true });
        Assert.IsNotNull(diagnosticCompiled);
        using var diagnosticResult = diagnosticCompiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("Name", typeof(string)), ("RightMissing", typeof(bool)));
        TableMaterializationTestHelper.AssertColumns(diagnosticResult,
            ("Name", typeof(string)), ("RightMissing", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsUnordered(diagnosticResult,
            new object?[] { "Mira", true });
    }
}
