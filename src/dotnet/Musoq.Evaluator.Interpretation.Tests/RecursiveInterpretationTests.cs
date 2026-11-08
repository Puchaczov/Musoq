using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed partial class RecursiveInterpretationTests
{
    private const string Hex = "02491302172B023D41024F53";
    private const string Schemas = "binary SampleBlock { ValueCount: byte, Values: byte[ValueCount] } binary SampleBatch { Blocks: SampleBlock[2] } ";
    private const string Slice = "FromBase64(ToBase64(o.Bytes, o.Off + o.Used, Length(o.Bytes) - o.Off - o.Used))";

    [TestMethod]
    [DataRow(false, "o.Bytes")]
    [DataRow(true, "o.Bytes")]
    [DataRow(false, Slice)]
    [DataRow(true, Slice)]
    [DataRow(false, "FromHexToBytes('02491302172B023D41024F53')")]
    public void RecursivePartialInterpretation_ShouldWalkTwoRecordChunks(bool earlierCte, string argument)
    {
        var sql = Query(earlierCte, argument);
        using var query = Compile(sql);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0, 0], [0, 6], [6, 6]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query).Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
    }

    [TestMethod]
    [DataRow("InterpretAt<SampleWindow>(o.Bytes, o.Off + o.Used)")]
    [DataRow("Interpret<SampleWindow>(FromBase64(ToBase64(o.Bytes, o.Off + o.Used, Length(o.Bytes) - o.Off - o.Used)))")]
    public void RecursiveInterpretation_WithChainedTypedApply_ShouldKeepFrontierArguments(string window)
    {
        var sql = ChainQuery(window);
        using var query = Compile(sql, "0400000004000000");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0, 0], [0, 4], [4, 4]);
        Assert.IsFalse(GeneratedExecutionIlInspection.GetGeneratedComputeMethods(query).Any(GeneratedExecutionIlInspection.ContainsBoxOpcode));
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(),
            CreateProvider("0400000004000000"), new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        Assert.DoesNotContain("_tableResults[", inspection.GeneratedCSharpCode);
    }

    private static string Query(bool earlierCte, string argument) => Schemas +
        (earlierCte ? "with recursive data as (select f.Content as Bytes from #test.files() f), " : "with recursive ") +
        "offs (Bytes, Off, Used) as (" +
        (earlierCte ? "select d.Bytes, 0, 0 from data d" : "select f.Content, 0, 0 from #test.files() f") +
        " union all select o.Bytes, o.Off + o.Used, p.BytesConsumed from offs o " +
        $"cross apply PartialInterpret<SampleBatch>({argument}) p where o.Off + o.Used < Length(o.Bytes)) " +
        "select o.Off, o.Used from offs o order by o.Off, o.Used";

    private static string ChainQuery(string window) =>
        "binary SampleWindow { Bytes: byte[4] } binary SampleAdvance { Used: int le } " +
        "with recursive offs (Bytes, Off, Used) as (select f.Content, 0, 0 from #test.files() f union all " +
        "select o.Bytes, o.Off + o.Used, p.Used from offs o " +
        $"cross apply {window} h cross apply Interpret<SampleAdvance>(h.Bytes) p " +
        "where o.Off + o.Used < Length(o.Bytes)) select o.Off, o.Used from offs o order by o.Off, o.Used";

    private static CompiledQuery Compile(string sql, string? hex = Hex, CompilationOptions? options = null, int copies = 1) =>
        InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), CreateProvider(hex, copies),
            new TestsLoggerResolver(), options ?? new CompilationOptions(usePrimitiveTypeValidation: false));

    private static ISchemaProvider CreateProvider(string? hex, int copies = 1) =>
        new BinarySchemaProvider(new Dictionary<string, IEnumerable<BinaryEntity>>
        {
            ["#test"] = Enumerable.Range(0, copies).Select(_ => new BinaryEntity
                { Content = hex == null ? null! : Convert.FromHexString(hex) }).ToArray()
        });
}
