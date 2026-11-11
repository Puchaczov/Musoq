using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Fixtures;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class BinaryInputSnapshotTests
{
    [TestMethod]
    [DataRow("FromBase64(ToBase64(f.Content))")]
    [DataRow("FromBase64(ToBase64(f.Content, 0, Length(f.Content)))")]
    [DataRow("FromHexToBytes(ToHex(f.Content))")]
    public void OrdinaryApply_LosslessComposition_ShouldPlanOwnedBoundedInputAndPreserveValues(string input)
    {
        var sql = $"binary Independent {{ Count: byte, Items: short le[Count] }} select p.Count, p.Items from #schema.first() f cross apply Interpret<Independent>({input}) p";
        var provider = Provider([2, 1, 0, 2, 0, 99, 99]);
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), provider,
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        var snapshot = ExecutionIrAnalysis.FlattenExpressions(inspection.ExecutionPlan!.Body)
            .OfType<ExecutionByteBufferSnapshot>().Single();
        Assert.AreEqual(511, snapshot.MaximumReadBytes);
        Assert.Contains("ByteBufferSnapshot.Copy", inspection.GeneratedCSharpCode);
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider,
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual((byte)2, row.GetValue<byte>(0));
        CollectionAssert.AreEqual(new short[] { 1, 2 }, row.GetValue<short[]>(1));
    }

    [TestMethod]
    [DataRow("001F056363", "Small", (byte)5)]
    [DataRow("011F00086363", "Wide", (byte)8)]
    public void BoundedSwitch_ShouldUseLargestBranchAndReadFollowingField(string input, string branch, byte tail)
    {
        const string sql = """
            binary Payload { Value: short le };
            binary Tagged { Kind: byte, Body: switch Kind { 0 => Small: byte, _ => Wide: Payload }, Tail: byte };
            select p.Body.Case as Branch, p.Tail
            from #schema.first() f cross apply Interpret<Tagged>(FromBase64(ToBase64(f.Content))) p
            """;
        var provider = Provider(Convert.FromHexString(input));
        var options = new CompilationOptions(usePrimitiveTypeValidation: false);
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), provider,
            new TestsLoggerResolver(), options);
        var snapshot = ExecutionIrAnalysis.FlattenExpressions(inspection.ExecutionPlan!.Body)
            .OfType<ExecutionByteBufferSnapshot>().Single();
        Assert.AreEqual(4, snapshot.MaximumReadBytes);

        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider,
            new TestsLoggerResolver(), options);
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(branch, row.GetValue<string>(0));
        Assert.AreEqual(tail, row.GetValue<byte>(1));
    }

    [TestMethod]
    public void EscapingComposition_ShouldKeepCompleteIndependentSnapshot()
    {
        byte[] input = [7, 8, 9];
        const string sql = "binary Prefix { Value: byte } select FromBase64(ToBase64(f.Content)) as Bytes, p.Value from #schema.first() f cross apply Interpret<Prefix>(FromBase64(ToBase64(f.Content))) p";
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), Provider(input),
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        using var result = query.Run();
        var copy = result.Single().GetValue<byte[]>(0);
        input[0] = 99;
        CollectionAssert.AreEqual(new byte[] { 7, 8, 9 }, copy);
    }

    private static ISchemaProvider Provider(byte[] data) =>
        LibraryExecutionFixture.CreateProvider<BinaryEntity>([new() { Content = data }]);

    [TestMethod]
    public void ShadowedBuiltIn_ShouldRetainTheBoundPluginImplementation()
    {
        var provider = LibraryExecutionFixture.CreateProvider<BinaryEntity, ShadowBufferLibrary>(
            [new() { Content = [99] }]);
        const string sql = "binary Prefix { Value: byte } select p.Value from #schema.first() f cross apply Interpret<Prefix>(FromBase64(ToBase64(f.Content))) p";
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), provider,
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        Assert.IsFalse(ExecutionIrAnalysis.FlattenExpressions(inspection.ExecutionPlan!.Body)
            .Any(expression => expression is ExecutionByteBufferSnapshot));
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider,
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        using var result = query.Run();
        Assert.AreEqual((byte)7, result.Single().GetValue<byte>(0));
    }

    [TestMethod]
    [DataRow("FromBase64(ToBase64(f.Content))", true)]
    [DataRow("FromBase64(ToBase64(f.Content, case when f.Name = '' then null else 0 end, 1))", false)]
    public void TolerantApply_WithNullBufferOrNullableRange_ShouldRetainTheLeftRow(string input, bool nullBuffer)
    {
        var sql = $"binary Prefix {{ Value: byte }} select p.Value from #schema.first() f outer apply TryInterpret<Prefix>({input}) p";
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), Provider(nullBuffer ? null! : [7]),
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        using var result = query.Run();
        Assert.HasCount(1, result);
        Assert.IsNull(result.Single().GetValue<byte?>(0));
    }

    [TestMethod]
    [DataRow("Capture(f.Content)", false)]
    [DataRow("FromBase64(ToBase64(Capture(f.Content)))", true)]
    public void UnknownProducer_ShouldExecuteOncePerInputAndRetainItsEffects(string input, bool expectSnapshot)
    {
        var entity = new BufferProducerEntity { Content = [7, 8, 9] };
        var provider = LibraryExecutionFixture.CreateProvider<BufferProducerEntity, BufferProducerLibrary>([entity]);
        var sql = $"binary Prefix {{ Value: byte }} select p.Value from #schema.first() f cross apply Interpret<Prefix>({input}) p";
        var inspection = InstanceCreator.CompileForInspection(sql, Guid.NewGuid().ToString(), provider,
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        Assert.AreEqual(expectSnapshot, ExecutionIrAnalysis.FlattenExpressions(inspection.ExecutionPlan!.Body)
            .Any(expression => expression is ExecutionByteBufferSnapshot));
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(), provider,
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        using var first = query.Run();
        Assert.AreEqual((byte)7, first.Single().GetValue<byte>(0));
        Assert.AreEqual(1, entity.Calls);
        using var second = query.Run();
        Assert.AreEqual((byte)7, second.Single().GetValue<byte>(0));
        Assert.AreEqual(2, entity.Calls);
    }
}
