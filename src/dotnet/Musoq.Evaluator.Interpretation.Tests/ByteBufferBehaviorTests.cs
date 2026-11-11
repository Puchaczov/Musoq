using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Fixtures;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ByteBufferBehaviorTests
{
    [TestMethod]
    [DataRow(-2, 3, "010203")]
    [DataRow(2, 2, "0304")]
    [DataRow(99, 2, "")]
    [DataRow(1, -3, "")]
    [DataRow(0, int.MaxValue, "010203040506")]
    public void ArrayRange_PublicOutputPreservesClampingAndOwnedSnapshot(int skip, int take, string expected)
    {
        var entity = new BufferFlowEntity();
        using var query = Compile(
            $"select EnumerableToArray(SkipAndTake(Produce(f.Content), {skip}, {take})) as Bytes from #schema.first() f",
            entity
        );
        using var result = query.Run();
        var bytes = result.Single().GetValue<byte[]>(0);
        CollectionAssert.AreEqual(Convert.FromHexString(expected), bytes);
        Assert.AreEqual(1, entity.Calls);
        Assert.AreNotSame(entity.Content, bytes);
        entity.Content[0] = 88;
        CollectionAssert.AreEqual(Convert.FromHexString(expected), bytes);
    }

    [TestMethod]
    public void Materialization_PrecedesLaterArgumentMutation()
    {
        var entity = new BufferFlowEntity();
        using var query = Compile(
            "select ReadAfter(EnumerableToArray(Produce(f.Content)), Mutate(f.Content)) as Value from #schema.first() f",
            entity
        );
        using var result = query.Run();
        Assert.AreEqual((byte)1, result.Single().GetValue<byte>(0));
        CollectionAssert.AreEqual(new[] { "produce", "mutate" }, entity.Events);
        Assert.AreEqual((byte)99, entity.Content[0]);
    }

    [TestMethod]
    public void Length_OfMaterializedOpaqueSequencePreservesEnumerationEffects()
    {
        var entity = new BufferFlowEntity();
        using var query = Compile(
            "select Length(EnumerableToArray(Skip(Sequence(f.Content), 2))) as Value from #schema.first() f",
            entity
        );
        using var result = query.Run();
        Assert.AreEqual(4, result.Single().GetValue<int?>(0));
        Assert.AreEqual(6, entity.Enumerated);
    }

    [TestMethod]
    public void ConditionalProducer_RetainsBranchAndExceptionOrdering()
    {
        var entity = new BufferFlowEntity();
        using var query = Compile(
            "select case when f.Calls = 0 then Length(EnumerableToArray(Take(Produce(f.Content), 2))) else Length(Fail(f.Content)) end as Value from #schema.first() f",
            entity
        );
        using var result = query.Run();
        Assert.AreEqual(2, result.Single().GetValue<int?>(0));
        CollectionAssert.AreEqual(new[] { "produce" }, entity.Events);
    }

    [TestMethod]
    [DataRow("Length(EnumerableToArray(Skip(f.Content, 2)))")]
    [DataRow("FromBytesToInt32(EnumerableToArray(Skip(f.Content, 2)))")]
    public void NullBuffer_PreservesNullableConsumerResult(string expression)
    {
        var entity = new BufferFlowEntity { Content = null! };
        using var query = Compile($"select {expression} as Value from #schema.first() f", entity);
        using var result = query.Run();
        Assert.IsNull(result.Single().GetValue<int?>(0));
    }

    [TestMethod]
    [DataRow(false, null)]
    [DataRow(true, 513)]
    public void ShortScalarBuffer_PreservesPadding(bool padding, int? expected)
    {
        var entity = new BufferFlowEntity { Content = [1, 2] };
        using var query = Compile(
            $"select FromBytesToInt32(EnumerableToArray(Take(f.Content, 2)), {padding.ToString().ToLowerInvariant()}) as Value from #schema.first() f",
            entity
        );
        using var result = query.Run();
        Assert.AreEqual(expected, result.Single().GetValue<int?>(0));
    }

    [TestMethod]
    public void MultipleCteConsumers_RetainCompleteLogicalLengthAndOwnedBytesAcrossRuns()
    {
        var entity = new BufferFlowEntity();
        const string sql =
            "binary Pair { First: byte, Second: byte }; with bytes as (select EnumerableToArray(Skip(Produce(f.Content), 1)) as Content from #schema.first() f) select Length(b.Content) as Size, b.Content as Bytes, p.Second as Value from bytes b cross apply Interpret<Pair>(b.Content) p";
        using var query = Compile(sql, entity);
        using var first = query.Run();
        var retained = first.Single().GetValue<byte[]>(1);
        Assert.AreEqual(5, first.Single().GetValue<int?>(0));
        Assert.AreEqual((byte)3, first.Single().GetValue<byte>(2));
        entity.Content[1] = 77;
        using var second = query.Run();
        var refreshed = second.Single().GetValue<byte[]>(1);
        Assert.AreEqual(2, entity.Calls);
        CollectionAssert.AreEqual(new byte[] { 2, 3, 4, 5, 6 }, retained);
        CollectionAssert.AreEqual(new byte[] { 77, 3, 4, 5, 6 }, refreshed);
    }

    private static CompiledQuery Compile(string sql, BufferFlowEntity entity) =>
        InstanceCreator.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider<BufferFlowEntity, BufferFlowLibrary>([entity]),
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
}
