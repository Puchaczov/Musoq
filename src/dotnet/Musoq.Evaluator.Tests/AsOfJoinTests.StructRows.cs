using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void StructAsOfInner_InjectsOriginalMatchedSourceContext()
    {
        const string query = "select l.Id as LeftId, r.ReadId() as MatchedId from #stress.rows('left') l asof join #stress.rows('right') r on l.KeyValue >= r.KeyValue tie break by r.Priority asc nulls last order by l.Id";
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new SpecStressAsofTieBreakSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result, ("LeftId", typeof(int)), ("MatchedId", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { 1, 12 }, new object?[] { 3, 14 });
    }

    [TestMethod]
    public void StructAsOfLeft_DefaultValuedMatchRetainsRowPresence()
    {
        const string query = "select l.Id as LeftId, r.Id as MatchedId, r is present as Matched from #stress.rows('left') l asof left join #stress.rows('zero') r on l.KeyValue >= r.KeyValue order by l.Id";
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(),
            new SpecStressAsofTieBreakSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var result = compiled.Run();
        TableMaterializationTestHelper.AssertColumns(result,
            ("LeftId", typeof(int)), ("MatchedId", typeof(int?)), ("Matched", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsInOrder(result,
            new object?[] { 0, 0, true }, new object?[] { 1, 0, true },
            new object?[] { 2, null, false }, new object?[] { 3, 0, true });
    }

    // Candidate: SPECSTRESS-2026-09-29-P5-0007
    // Specification: {"specs/musoq-core-language-spec.md":["6.1","8.11.1","8.11.2","8.11.4","8.11.6","8.11.7"]}
    // Historical fixture: {"provider":"SpecStressAsofTieBreakSchemaProvider defined in CurrentProbeTests.cs; schema #stress, typed rows.","left_rows":[{"Id":0,"KeyValue":2,"Priority":null},{"Id":1,"KeyValue":5,"Priority":null},{"Id":2,"KeyValue":null,"Priority":null},{"Id":3,"KeyValue":7,"Priority":null}],"right_rows":[{"Id":10,"KeyValue":3,"Priority":9},{"Id":11,"KeyValue":3,"Priority":1},{"Id":12,"KeyValue":4,"Priority":2},{"Id":13,"KeyValue":4,"Priority":null},{"Id":14,"KeyValue":6,"Priority":1},{"Id":15,"KeyValue":6,"Priority":null}],"tie_break":"Priority ascending NULLS LAST, to make duplicate nearest candidates deterministic.","form_difference":"ASOF LEFT JOIN versus its explicitly accepted ASOF LEFT OUTER JOIN synonym."}
    // Historical expectation: {"both_forms_compile_and_execute":true,"equivalence":"Both forms return the same output column names, column types, and row multiset for identical inputs.","output_order":"No order is asserted; compare rows as a sorted multiset.","diagnostic_code":"Not applicable for valid forms."}
    // Observed failure: valid_asof_left_join|expected_compile_success|actual_MQ9001_internal_compile_failure|typed_source_entity_row_SpecStressAsofTieBreakRow
    // Disposition: Unresolved/cap-deferred: The campaign promotion cap of 12 distinct failures is already reached. Complete the required three-run confirmation, then record this failure without starting minimization or promotion. | Existing ignored test: AsofLeftJoinUsesTieBreakForEqualNearestKeysFromTypedSources.
    [TestMethod]
    [TestProperty("CandidateId", "SPECSTRESS-2026-09-29-P5-0007")]
    [TestProperty("Campaign", "Current 2026-09-29")]
    [TestProperty("Phase", "5")]
    public void AsofLeftJoinUsesTieBreakForEqualNearestKeysFromTypedSources()
    {
        const string query = "select l.Id as LeftId, r.Id as MatchedId from #stress.rows('left') l asof left join #stress.rows('right') r on l.KeyValue >= r.KeyValue tie break by r.Priority asc nulls last";
        using var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new SpecStressAsofTieBreakSchemaProvider(),
            LoggerResolver,
            TestCompilationOptions);
        using var result = TableMaterializationTestHelper.Materialize(compiled.Run());

        AssertTypedTieResult(result);
        const string outerQuery = "select l.Id as LeftId, r.Id as MatchedId from #stress.rows('left') l asof left outer join #stress.rows('right') r on l.KeyValue >= r.KeyValue tie break by r.Priority asc nulls last";
        using var outerCompiled = InstanceCreator.CompileForExecution(outerQuery, Guid.NewGuid().ToString(), new SpecStressAsofTieBreakSchemaProvider(), LoggerResolver, TestCompilationOptions);
        using var outerResult = outerCompiled.Run();
        AssertTypedTieResult(outerResult);
    }
    private static void AssertTypedTieResult(Table result)
    {
        TableMaterializationTestHelper.AssertColumns(result, ("LeftId", typeof(int)), ("MatchedId", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(result,
            new object?[] { 0, null }, new object?[] { 1, 12 },
            new object?[] { 2, null }, new object?[] { 3, 14 });
    }
}
