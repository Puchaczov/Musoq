using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class JoinRowPresencePredicateTests
{
    // Candidate: SPECSTRESS-2026-09-29-P3-0058
    // Specification: ["5.1","5.2","6.1","6.2","8.2","8.4","19.5","3.1","3.2","3.3","4.1","5.2","5.3","10.2"]
    // Historical fixture: {"provider":"SpecStressCoupledSchemaProvider with SpecStressCoupledSource","schema_registration_key":"A","source_method":"Entities","table_declaration":"Record { Name: string }","source_invocations":2,"invocations":[{"alias":"First","arguments":[true,"left"],"emitted_rows":[{"Name":"left"}]},{"alias":"Second","arguments":[true,"right"],"emitted_rows":[{"Name":"right"}]}],"coupled_row_type":"SpecStressCoupledRow","coupled_field_types":{"Name":"System.String"},"source_constructor_parameters":{"enabled":"System.Boolean","displayName":"System.String"}}
    // Historical expectation: {"query_rejected":false,"compilation_succeeded":true,"execution_submitted":true,"columns":[{"name":"LeftName","type":"System.String"}],"row_count":1,"row_ordering":"Single left row retained by r IS MISSING.","rows":[["left"]]}
    // Observed failure: spec-mismatch|CORE-JOIN-ROW-PRESENCE-PREDICATES|expected-one-left-row-after-unmatched-left-join|actual-zero-rows
    // Regression: struct source presence must remain available until outer-join null extension is applied.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P3-0058")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "3")]
    public void MissingRightRowRemainsAvailableToLeftJoinPresencePredicate()
    {
        const string query = "table Record { Name: string }; couple A.Entities with table Record as First; couple A.Entities with table Record as Second; select l.Name as LeftName from First(true, 'left') l left join Second(true, 'right') r on l.Name = r.Name where r is missing";
        using var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new SpecStressCoupledSchemaProvider(),
            LoggerResolver,
            TestCompilationOptions);
        using var result = TableMaterializationTestHelper.Materialize(compiled.Run());

        TableMaterializationTestHelper.AssertColumns(result, ("LeftName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { "left" });
    }

    // Candidate: SPECSTRESS-2026-09-29-P4-0062
    // Specification: {"specs/musoq-core-language-spec.md":["5.3","6.2","6.3","6.6","8.4","14.1","14.3","14.8"],"specs/musoq-table-couple-spec.md":["3.1","3.3","3.6","4.1","4.3","4.4","4.5","5.2","5.4","8.3","10.1","10.2","10.6"]}
    // Historical fixture: {"schema_provider":"SpecStressCoupledSchemaProvider","schema":"A","schema_method":"Entities(bool enabled, string displayName)","table":{"name":"FullJoinInput","columns":[{"name":"Name","type":"string"}]},"coupled_alias":"Coupled","left_cte":{"name":"LeftRows","call_arguments":[true,"A"],"emits":[{"Name":"A"}]},"right_call":{"arguments":[true,"B"],"emits":[{"Name":"B"}]},"join":"FULL OUTER JOIN on l.Name = r.Name","projections":["l.Name AS LeftName","r.Name AS RightName","l IS MISSING AS LeftMissing","r IS MISSING AS RightMissing"]}
    // Historical expectation: {"diagnostic_build_succeeded":true,"execution_succeeded":true,"column_names":["LeftName","RightName","LeftMissing","RightMissing"],"column_types":["System.String","System.String","System.Boolean","System.Boolean"],"row_count":2,"rows":[["A",null,false,true],[null,"B",true,false]],"row_ordering":"No top-level ORDER BY is specified; compare as an exact row multiset."}
    // Observed failure: full-outer-join|CORE-JOIN-ROW-PRESENCE-PREDICATES|expected-RightMissing-true|actual-false-for-null-extended-right-row|after-execution
    // Related regression: the same premature presence folding affected the direct struct side of a full join.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P4-0062")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "4")]
    public void Current_P4_0062_FullJoinPreservesBothMissingSides()
    {
        const string query = "table FullJoinInput {\r\n    Name: string\r\n};\r\ncouple A.Entities with table FullJoinInput as Coupled;\r\nwith LeftRows as (\r\n    select c.Name as Name\r\n    from Coupled(true, 'A') c\r\n)\r\nselect l.Name as LeftName,\r\n       r.Name as RightName,\r\n       l is missing as LeftMissing,\r\n       r is missing as RightMissing\r\nfrom LeftRows l\r\nfull outer join Coupled(true, 'B') r on l.Name = r.Name";

        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), new SpecStressCoupledSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("LeftName", typeof(System.String)),
            ("RightName", typeof(System.String)),
            ("LeftMissing", typeof(System.Boolean)),
            ("RightMissing", typeof(System.Boolean)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { "A", null, false, true },
            new object?[] { null, "B", true, false });
    }

    [TestMethod]
    [DataRow("left join", "r", "left", "present")]
    [DataRow("left join", "r", "right", "missing")]
    [DataRow("right join", "l", "left", "present")]
    [DataRow("right join", "l", "right", "missing")]
    public void CoupledStructJoin_PresenceClassifiesMatchedAndUnmatchedRows(string join, string optionalAlias, string retainedName, string expectedState)
    {
        var query = $"table Record {{ Name: string }}; couple A.Entities with table Record as Coupled; select case when {optionalAlias} is missing then 'missing' else 'present' end as State from Coupled(true, 'left') l {join} Coupled(true, '{retainedName}') r on l.Name = r.Name";
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), new SpecStressCoupledSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var result = TableMaterializationTestHelper.Materialize(compiled.Run());
        TableMaterializationTestHelper.AssertColumns(result, ("State", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { expectedState });
    }
}
