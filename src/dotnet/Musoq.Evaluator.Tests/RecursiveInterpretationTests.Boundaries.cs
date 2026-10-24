using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

public sealed partial class RecursiveInterpretationTests
{
    [TestMethod]
    public void InlineChunkWalker_ShouldExecuteTheHexFixture()
    {
        var sql = Query(false, Slice).Replace("select f.Content, 0, 0 from #test.files() f",
            "select FromHexToBytes('02491302172B023D41024F53'), 0, 0 from values { (Dummy: 'x') } d", StringComparison.Ordinal);
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), new Components.TestSchemaProvider(),
            new Components.TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0, 0], [0, 6], [6, 6]);
    }

    [TestMethod]
    public void RecursiveInterpretation_WithNullFrontierBytes_ShouldRejectTheRowBeforeInterpretation()
    {
        using var query = Compile(Query(false, Slice), null);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0, 0]);
    }

    [TestMethod]
    public void RecursiveInterpretation_WithVolatileChainArguments_ShouldRetainItsEvaluationBoundary()
    {
        var sql = ChainQuery("InterpretAt<SampleWindow>(case when Rand() >= 0 then o.Bytes else o.Bytes end, o.Off + o.Used)");
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(),
            CreateProvider("0400000004000000"), new Components.TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        Assert.Contains("PhysicalCteRef [oh as oh]", inspection.PhysicalPlanText);
        using var query = Compile(sql, "0400000004000000");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0, 0], [0, 4], [4, 4]);
    }

    [TestMethod]
    public void RecursiveInterpretationChain_WithMultipleFrontierRows_ShouldKeepEachDecodedPayload()
    {
        const string sql = "binary SampleWindow { Bytes: byte[8] } binary SampleAdvance { Used: int le, Value: int le } " +
            "with recursive offs (Name, Bytes, Off, Used, Value) as (select f.Name, f.Content, 0, 0, 0 from #test.files() f union all " +
            "select o.Name, o.Bytes, o.Off + o.Used, p.Used, p.Value from offs o " +
            "cross apply InterpretAt<SampleWindow>(o.Bytes, o.Off + o.Used) h cross apply Interpret<SampleAdvance>(h.Bytes) p " +
            "where o.Off + o.Used < Length(o.Bytes)) select o.Name, o.Off, o.Used, o.Value from offs o order by o.Name, o.Off, o.Used";
        var provider = new Components.BinarySchemaProvider(new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<Components.BinaryEntity>>
        {
            ["#test"] = [new() { Name = "a", Content = [8, 0, 0, 0, 42, 0, 0, 0, 8, 0, 0, 0, 43, 0, 0, 0] },
                new() { Name = "b", Content = [8, 0, 0, 0, 99, 0, 0, 0, 8, 0, 0, 0, 100, 0, 0, 0] }]
        });
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider,
            new Components.TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            ["a", 0, 0, 0], ["a", 0, 8, 42], ["a", 8, 8, 43],
            ["b", 0, 0, 0], ["b", 0, 8, 99], ["b", 8, 8, 100]);
    }

    [TestMethod]
    [DataRow("union all", 2, 6)]
    [DataRow("union", 1, 3)]
    [DataRow("union (Off, Used)", 2, 3)]
    public void RecursiveInterpretation_ShouldPreserveFrontierMultiplicityAndAdmission(string operation, int copies, int expected)
    {
        var sql = Query(false, Slice).Replace(" union all ", " " + operation + " ", StringComparison.Ordinal);
        using var query = Compile(sql, copies: copies);
        using var table = query.Run();
        Assert.AreEqual(expected, table.Count);
        Assert.AreEqual(expected / 3 * 18, table.Sum(row => row.GetValue<int>(0) + row.GetValue<int>(1)));
    }

    [TestMethod]
    public void RecursiveInterpretation_AtExactEof_ShouldFilterBeforeStrictInterpretation()
    {
        const string sql = "binary SampleAdvance { Used: int le } with recursive offs (Bytes, Off, Used) as (" +
            "select f.Content, 0, 0 from #test.files() f union all " +
            "select o.Bytes, o.Off + o.Used, p.Used from offs o cross apply InterpretAt<SampleAdvance>(o.Bytes, o.Off + o.Used) p " +
            "where o.Off + o.Used < Length(o.Bytes)) select o.Off, o.Used from offs o order by o.Off, o.Used";
        using var query = Compile(sql, "0400000004000000");
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0, 0], [0, 4], [4, 4]);
    }

    [TestMethod]
    public void RecursivePartialInterpretation_WithMalformedFinalChunk_ShouldRetainPartialErrorAndConsumedPosition()
    {
        var sql = Schemas + "with recursive offs (Bytes, Off, Used, ErrorText) as (" +
            "select f.Content, 0, 0, null::string from #test.files() f union all " +
            "select o.Bytes, o.Off + o.Used, p.BytesConsumed, p.ErrorMessage from offs o " +
            $"cross apply PartialInterpret<SampleBatch>({Slice}) p where o.Off + o.Used < Length(o.Bytes)) " +
            "select o.Off, o.Used, o.ErrorText from offs o order by o.Off, o.Used";
        using var query = Compile(sql, "02491302172B023D4102");
        using var table = query.Run();
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(6, table[2].GetValue<int>(0));
        Assert.AreEqual(4, table[2].GetValue<int>(1));
        StringAssert.Contains(table[2].GetValue<string>(2), "ISE0001");
        StringAssert.Contains(table[2].GetValue<string>(2), "Values");
    }

    [TestMethod]
    public void RecursiveStrictInterpretation_WithMalformedFinalChunk_ShouldPreserveMq7101()
    {
        const string sql = "binary SampleAdvance { Used: int le } with recursive offs (Bytes, Off, Used) as (" +
            "select f.Content, 0, 0 from #test.files() f union all " +
            "select o.Bytes, o.Off + o.Used, p.Used from offs o cross apply InterpretAt<SampleAdvance>(o.Bytes, o.Off + o.Used) p " +
            "where o.Off + o.Used < Length(o.Bytes)) select o.Off, o.Used from offs o";
        using var query = Compile(sql, "04000000040000", new CompilationOptions(ParallelizationMode.None, usePrimitiveTypeValidation: false));
        var exception = Assert.Throws<Musoq.Schema.Interpreters.ParseException>(() => TableMaterializationTestHelper.Materialize(query.Run()));
        Assert.AreEqual(DiagnosticCode.MQ7101_InterpretationInsufficientData, exception.ToDiagnostic().Code);
        Assert.AreEqual("Used", exception.FieldName);
        Assert.AreEqual(4, exception.Position);
    }

    [TestMethod]
    [DataRow(1, 100, DiagnosticCode.MQ7007_RecursiveCteIterationLimitExceeded)]
    [DataRow(100, 1, DiagnosticCode.MQ7008_RecursiveCteRowLimitExceeded)]
    public void RecursiveInterpretation_ShouldPreserveConfiguredFrontierLimits(int iterations, int rows, DiagnosticCode code)
    {
        var options = new CompilationOptions(ParallelizationMode.None, usePrimitiveTypeValidation: false)
            .WithRecursiveCteLimits(new(iterations, rows));
        using var query = Compile(Query(false, Slice), options: options);
        var exception = Assert.Throws<Musoq.Evaluator.Exceptions.RecursiveCteLimitExceededException>(
            () => TableMaterializationTestHelper.Materialize(query.Run()));
        Assert.AreEqual(code, exception.Code);
    }

    [TestMethod]
    public void RecursiveInterpretation_ShouldHonorTheConfiguredRepeatBoundary()
    {
        using var query = CompileRepeatingChunk(4);
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0], [4]);
    }

    [TestMethod]
    public void RecursiveInterpretation_ExceedingRepeatLimit_ShouldPreserveMq7109AndPosition()
    {
        using var query = CompileRepeatingChunk(3);
        var exception = Assert.Throws<Musoq.Schema.Interpreters.ParseException>(
            () => TableMaterializationTestHelper.Materialize(query.Run()));
        Assert.AreEqual(DiagnosticCode.MQ7109_InterpretationMaxIterationsExceeded, exception.ToDiagnostic().Code);
        Assert.AreEqual(3, exception.Position);
    }

    private static CompiledQuery CompileRepeatingChunk(int maximum)
    {
        const string sql = "binary SampleBatch { Blocks: byte repeat until eof } " +
            "with recursive offs (Bytes, Off, Used) as (select f.Content, ToInt32(0), ToInt32(0) from #schema.first() f union all " +
            "select o.Bytes, o.Off + o.Used, Length(p.Blocks)::int from offs o " +
            "cross apply Interpret<SampleBatch>(o.Bytes) p where o.Off + o.Used < Length(o.Bytes)) " +
            "select o.Used from offs o order by o.Used";
        var provider = Schema.LibraryExecution.LibraryExecutionFixture.CreateProvider<Components.BinaryEntity, Musoq.Plugins.LibraryBase>(
            [new Components.BinaryEntity { Content = [1, 2, 3, 4] }]);
        var options = new CompilationOptions(ParallelizationMode.None, usePrimitiveTypeValidation: false)
            .WithInterpretationLimits(new(maximum));
        return InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider, new Components.TestsLoggerResolver(), options);
    }

    [TestMethod]
    public void RecursiveInterpretation_WithPreCancelledToken_ShouldRemainCancellation()
    {
        using var query = Compile(Query(false, Slice));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => TableMaterializationTestHelper.Materialize(query.Run(cancellation.Token)));
    }

    [TestMethod]
    public void RecursiveInterpretation_WhenLoadedFromArtifact_ShouldKeepTypedFrontierBindings()
    {
        var sql = Query(true, Slice);
        var provider = CreateProvider(Hex);
        var logger = new Components.TestsLoggerResolver();
        var options = new CompilationOptions(usePrimitiveTypeValidation: false);
        var artifact = InstanceCreator.CompileArtifactWithDiagnostics(sql, Guid.NewGuid().ToString(), provider, logger, options);
        Assert.IsTrue(artifact.Succeeded, string.Join(";", artifact.Diagnostics));
        var loaded = InstanceCreator.CreateExecutableFromArtifactWithDiagnostics(sql, artifact.Artifact!, provider, logger, options);
        Assert.IsTrue(loaded.Succeeded, string.Join(";", loaded.Diagnostics));
        using var query = loaded.CompiledQuery!;
        using var table = query.Run();
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0, 0], [0, 6], [6, 6]);
    }

    [TestMethod]
    [DataRow("group by o.Bytes, o.Off, o.Used, p.BytesConsumed")]
    [DataRow("order by o.Off")]
    public void UnsupportedRecursiveInterpretationOperators_ShouldReportMq3075(string clause)
    {
        var sql = Query(false, Slice).Replace("< Length(o.Bytes))", "< Length(o.Bytes) " + clause + ")", StringComparison.Ordinal);
        var exception = Assert.Throws<MusoqQueryException>(() => Compile(sql));
        Assert.AreEqual(DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator, exception.PrimaryEnvelope.Code);
    }
}
