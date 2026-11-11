using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Fixtures;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ByteBufferBoundaryTests
{
    [TestMethod]
    [DataRow("Length(EnumerableToArray(Take(Fail(f.Content), 0)))")]
    [DataRow("FromBytesToInt32(EnumerableToArray(Take(Fail(f.Content), 0)), true)")]
    public void ZeroByteRead_PreservesOpaqueProducerFailure(string expression)
    {
        var entity = new BufferFlowEntity();
        using var query = Compile($"select {expression} as Value from #schema.first() f", entity);
        using var result = query.Run();
        var exception = Assert.ThrowsExactly<QueryExecutionException>(() => result.ToArray());
        Assert.IsInstanceOfType<InvalidOperationException>(exception.GetBaseException());
        Assert.AreEqual("buffer producer failure", exception.GetBaseException().Message);
        CollectionAssert.AreEqual(new[] { "fail" }, entity.Events);
    }

    [TestMethod]
    [DataRow("Length(EnumerableToArray(Take(MergeArrays(Produce(f.Content), NullBuffer()), 0)))")]
    [DataRow("FromBytesToInt32(EnumerableToArray(Take(MergeArrays(Produce(f.Content), NullBuffer()), 4)))")]
    public void Concatenation_ValidatesUnusedNullSegmentAfterEvaluatingEveryArgument(string expression)
    {
        var entity = new BufferFlowEntity();
        using var query = Compile($"select {expression} as Value from #schema.first() f", entity);
        using var result = query.Run();
        var exception = Assert.ThrowsExactly<QueryExecutionException>(() => result.ToArray());
        Assert.IsInstanceOfType<NullReferenceException>(exception.GetBaseException());
        CollectionAssert.AreEqual(new[] { "produce", "null" }, entity.Events);
        Assert.AreEqual(1, entity.Calls);
    }

    [TestMethod]
    public void Concatenation_PreservesThrowingSuffixEvaluation()
    {
        var entity = new BufferFlowEntity();
        using var query = Compile(
            "select Length(EnumerableToArray(Take(MergeArrays(Produce(f.Content), Fail(f.Content)), 0))) as Value from #schema.first() f",
            entity
        );
        using var result = query.Run();
        var exception = Assert.ThrowsExactly<QueryExecutionException>(() => result.ToArray());
        Assert.IsInstanceOfType<InvalidOperationException>(exception.GetBaseException());
        Assert.AreEqual("buffer producer failure", exception.GetBaseException().Message);
        CollectionAssert.AreEqual(new[] { "produce", "fail" }, entity.Events);
    }

    [TestMethod]
    [DataRow("EnumerableToArray(f.Content)", 0, 0)]
    [DataRow("EnumerableToArray(Take(f.Content, 0))", 0, 0)]
    [DataRow("FromBase64(ToBase64(f.Content))", null, null)]
    [DataRow("FromBase64(ToBase64(f.Content, 0, 0))", null, null)]
    [DataRow("FromHexToBytes(ToHex(f.Content))", null, null)]
    public void EmptyMaterialization_PreservesNullStateLengthAndPadding(string expression, int? length, int? value)
    {
        var entity = new BufferFlowEntity { Content = [] };
        using var query = Compile(
            $"select Length({expression}) as Size, FromBytesToInt32({expression}, true) as Value from #schema.first() f",
            entity
        );
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(length, row.GetValue<int?>(0));
        Assert.AreEqual(value, row.GetValue<int?>(1));
    }

    [TestMethod]
    public void SuccessiveExcessiveSkips_ClampWithoutOverflow()
    {
        var entity = new BufferFlowEntity();
        using var query = Compile(
            "select Length(EnumerableToArray(Skip(Skip(f.Content, 2147483647), 2147483647))) as Value from #schema.first() f",
            entity
        );
        using var result = query.Run();
        Assert.AreEqual(0, result.Single().GetValue<int?>(0));
    }

    [TestMethod]
    public void PublicSnapshots_PreserveDistinctAllocationIdentities()
    {
        var entity = new BufferFlowEntity();
        using var query = Compile(
            "select EnumerableToArray(f.Content) as First, EnumerableToArray(f.Content) as Second from #schema.first() f",
            entity
        );
        using var result = query.Run();
        var row = result.Single();
        Assert.AreNotSame(row.GetValue<byte[]>(0), row.GetValue<byte[]>(1));
        Assert.AreNotSame(entity.Content, row.GetValue<byte[]>(0));
    }

    [TestMethod]
    public void DirectCteArrayProjection_PreservesOriginalSharedReference()
    {
        var entity = new BufferFlowEntity();
        using var query = Compile(
            "with bytes as (select f.Content as Content from #schema.first() f) select b.Content as Bytes from bytes b",
            entity
        );
        using var result = query.Run();
        var bytes = result.Single().GetValue<byte[]>(0);
        Assert.AreSame(entity.Content, bytes);
        entity.Content[0] = 99;
        Assert.AreEqual((byte)99, bytes[0]);
    }

    [TestMethod]
    [DataRow("EnumerableToArray(Skip(EnumerableToArray(Produce(f.Content)), Mutate(f.Content)))")]
    [DataRow("EnumerableToArray(Skip(MergeArrays(Produce(f.Content), f.Content), Mutate(f.Content)))")]
    public void EffectfulRangeBound_PreservesEarlierOwnedMaterialization(string expression)
    {
        var entity = new BufferFlowEntity();
        using var query = Compile($"select FromBytesToInt32({expression}) as Value from #schema.first() f", entity);
        using var result = query.Run();
        Assert.AreEqual(0x04030201, result.Single().GetValue<int?>(0));
        CollectionAssert.AreEqual(new[] { "produce", "mutate" }, entity.Events);
        Assert.AreEqual((byte)99, entity.Content[0]);
    }

    [TestMethod]
    public void InvalidConcatenation_PrecedesEffectfulLaterRangeBound()
    {
        var entity = new BufferFlowEntity();
        using var query = Compile(
            "select Length(EnumerableToArray(Skip(MergeArrays(Produce(f.Content), NullBuffer()), Mutate(f.Content)))) as Value from #schema.first() f",
            entity
        );
        using var result = query.Run();
        var exception = Assert.ThrowsExactly<QueryExecutionException>(() => result.ToArray());
        Assert.IsInstanceOfType<NullReferenceException>(exception.GetBaseException());
        CollectionAssert.AreEqual(new[] { "produce", "null" }, entity.Events);
        Assert.AreEqual((byte)1, entity.Content[0]);
    }

    [TestMethod]
    [DataRow("FromBase64(ToBase64(f.Content, null, 1))")]
    [DataRow("FromBase64(ToBase64(f.Content, 0, null))")]
    public void NullableSliceArgument_PreservesNullResult(string expression)
    {
        using var query = Compile($"select Length({expression}) as Value from #schema.first() f", new());
        using var result = query.Run();
        Assert.IsNull(result.Single().GetValue<int?>(0));
    }

    [TestMethod]
    public void Concatenation_ValidatesCheckedOverflowEvenWhenNoBytesAreRead()
    {
        var entity = new BufferFlowEntity { Content = new byte[8 * 1024 * 1024] };
        var arguments = string.Join(", ", Enumerable.Repeat("f.Content", 256));
        using var query = Compile(
            $"select FromBytesToInt32(EnumerableToArray(Take(MergeArrays({arguments}), 0))) as Value from #schema.first() f",
            entity
        );
        using var result = query.Run();
        var exception = Assert.ThrowsExactly<QueryExecutionException>(() => result.ToArray());
        Assert.IsInstanceOfType<OverflowException>(exception.GetBaseException());
    }

    [TestMethod]
    public void UnmarkedOpaqueProducer_ExecutesOnceForEveryInputAndRepeatedExecution()
    {
        var entity = new BufferFlowEntity();
        var provider = LibraryExecutionFixture.CreateProvider<BufferFlowEntity, BufferFlowLibrary>([entity, entity]);
        using var query = InstanceCreator.CompileForExecution(
            "select Length(EnumerableToArray(Take(UnmarkedBuffer(f.Self), 0))) as Value from #schema.first() f",
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
        using var first = query.Run();
        CollectionAssert.AreEqual(new int?[] { 0, 0 }, first.Select(row => row.GetValue<int?>(0)).ToArray());
        Assert.AreEqual(2, entity.Calls);
        using var second = query.Run();
        CollectionAssert.AreEqual(new int?[] { 0, 0 }, second.Select(row => row.GetValue<int?>(0)).ToArray());
        Assert.AreEqual(4, entity.Calls);
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
