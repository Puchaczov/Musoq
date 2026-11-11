using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Fixtures;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ByteBufferFlowBehaviorTests
{
    private const string Definition = "with bytes as (select EnumerableToArray(Skip(Produce(f.Content), 1)) as Content from #schema.first() f), forwarded as (select b.Content as Data from bytes b) ";

    [TestMethod]
    public void PrivateView_PreservesLengthBytesAndSnapshotTimingAcrossRepeatedRuns()
    {
        var entity = new BufferFlowEntity();
        using var query = Compile("binary Pair { A: byte, B: byte }; " + Definition + "select Length(c.Data) as Size, FromBytesToInt32(c.Data) as Value, p.B from forwarded c cross apply Interpret<Pair>(c.Data) p", entity);
        using var first = query.Run();
        var retained = first.Single();
        Assert.AreEqual(5, retained.GetValue<int?>(0));
        Assert.AreEqual(BitConverter.ToInt32(new byte[] { 2, 3, 4, 5 }), retained.GetValue<int?>(1));
        Assert.AreEqual((byte)3, retained.GetValue<byte>(2));
        entity.Content[1] = 77;
        using var second = query.Run();
        Assert.AreEqual(BitConverter.ToInt32(new byte[] { 77, 3, 4, 5 }), second.Single().GetValue<int?>(1));
        Assert.AreEqual(BitConverter.ToInt32(new byte[] { 2, 3, 4, 5 }), retained.GetValue<int?>(1));
        Assert.AreEqual(2, entity.Calls);
    }

    [TestMethod]
    [DataRow(null, null, null)]
    [DataRow("", 0, 0)]
    [DataRow("01", 0, 0)]
    [DataRow("0102", 1, 2)]
    public void PrivateView_NullEmptyAndShortBuffersKeepLengthAndPadding(string? hex, int? expectedLength, int? expectedValue)
    {
        var entity = new BufferFlowEntity { Content = hex is null ? null! : Convert.FromHexString(hex) };
        using var query = Compile(Definition + "select Length(c.Data) as Size, FromBytesToInt32(c.Data, true) as Value from forwarded c", entity);
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(expectedLength, row.GetValue<int?>(0));
        Assert.AreEqual(expectedValue, row.GetValue<int?>(1));
        Assert.AreEqual(1, entity.Calls);
    }

    [TestMethod]
    public void RetainedOwnedPrefix_PrecedesLaterSourceMutation()
    {
        var entity = new BufferFlowEntity();
        const string sql = "with bytes as (select EnumerableToArray(NextBuffer(f.Self)) as Content from #schema.first() f) select Length(b.Content) as Size, FromBytesToInt32(b.Content) as Value from bytes b";
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider<BufferFlowEntity, BufferFlowLibrary>([entity, entity]),
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        using var result = query.Run();
        CollectionAssert.AreEqual(new int?[] { BitConverter.ToInt32(new byte[] { 1, 2, 3, 4 }), BitConverter.ToInt32(new byte[] { 99, 2, 3, 4 }) }, result.Select(row => row.GetValue<int?>(1)).ToArray());
        Assert.AreEqual((byte)99, entity.Content[0]);
    }

    [TestMethod]
    public async Task PrivateViews_AreLocalToConcurrentQueryExecutions()
    {
        using var first = Compile(Definition + "select Length(c.Data) as Size, FromBytesToInt32(c.Data) as Value from forwarded c", new());
        using var second = Compile(Definition + "select Length(c.Data) as Size, FromBytesToInt32(c.Data) as Value from forwarded c", new() { Content = [8, 7, 6, 5, 4, 3, 2, 1] });
        static int? Read(CompiledQuery query) { using var result = query.Run(); return result.Single().GetValue<int?>(1); }
        var values = await Task.WhenAll(Task.Run(() => Read(first)), Task.Run(() => Read(second)));
        CollectionAssert.AreEqual(new int?[] { BitConverter.ToInt32(new byte[] { 2, 3, 4, 5 }), BitConverter.ToInt32(new byte[] { 7, 6, 5, 4 }) }, values);
    }

    [TestMethod]
    public void CancelledExecution_DoesNotEvaluatePrivateProducers()
    {
        var entity = new BufferFlowEntity();
        using var query = Compile(Definition + "select Length(c.Data) as Size from forwarded c", entity);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => { using var result = query.Run(cancellation.Token); result.ToArray(); });
        Assert.AreEqual(0, entity.Calls);
    }

    [TestMethod]
    [DataRow(0, 5, 0x05040302)]
    [DataRow(2, 3, 0x00060504)]
    [DataRow(-3, 5, 0x05040302)]
    [DataRow(int.MaxValue, 0, 0)]
    public void RangesAcrossPrivateValues_PreserveLogicalClampingAndPadding(int skip, int size, int value)
    {
        using var query = Compile(Definition + $"select Length(EnumerableToArray(Skip(c.Data, {skip}))) as Size, FromBytesToInt32(EnumerableToArray(Skip(c.Data, {skip})), true) as Value from forwarded c", new());
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(size, row.GetValue<int?>(0));
        Assert.AreEqual(value, row.GetValue<int?>(1));
    }

    [TestMethod]
    public void LengthOnlyAfterPrivateRange_DoesNotReadAnUnavailableSuffix()
    {
        using var query = Compile(Definition + "select Length(EnumerableToArray(Skip(c.Data, 2))) as Size from forwarded c", new());
        using var result = query.Run();
        Assert.AreEqual(3, result.Single().GetValue<int?>(0));
    }

    [TestMethod]
    public void ProjectedRange_KeepsItsOwnSnapshotAndLogicalLength()
    {
        using var query = Compile("with bytes as (select EnumerableToArray(f.Content) as Content from #schema.first() f), sliced as (select EnumerableToArray(Skip(b.Content, 2)) as Data from bytes b) select Length(c.Data) as Size, FromBytesToInt32(c.Data, true) as Value from sliced c", new());
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(4, row.GetValue<int?>(0));
        Assert.AreEqual(BitConverter.ToInt32(new byte[] { 3, 4, 5, 6 }), row.GetValue<int?>(1));
    }

    [TestMethod]
    [DataRow("where Length(c.Data) > 0 order by Length(c.Data)")]
    [DataRow("where FromBytesToInt32(c.Data) > 0 order by FromBytesToInt32(c.Data)")]
    public void FilteringAndSorting_PreservePrivateConsumers(string suffix)
    {
        using var query = Compile(Definition + "select Length(c.Data) as Size, FromBytesToInt32(c.Data) as Value from forwarded c " + suffix, new());
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(5, row.GetValue<int?>(0));
        Assert.AreEqual(BitConverter.ToInt32(new byte[] { 2, 3, 4, 5 }), row.GetValue<int?>(1));
    }

    [TestMethod]
    public void JoinedPrivateAliases_CombineAllConsumerRequirements()
    {
        const string sql = "with bytes as (select EnumerableToArray(f.Content) as Content from #schema.first() f) select Length(a.Content) as Size, FromBytesToInt32(b.Content) as Value from bytes a inner join bytes b on Length(a.Content) = Length(b.Content)";
        using var query = Compile(sql, new());
        using var result = query.Run();
        Assert.AreEqual(6, result.Single().GetValue<int?>(0));
        Assert.AreEqual(BitConverter.ToInt32(new byte[] { 1, 2, 3, 4 }), result.Single().GetValue<int?>(1));
    }

    [TestMethod]
    [DataRow("select Length(c.Data) as Size, Count(1) as Total from forwarded c group by Length(c.Data)")]
    [DataRow("select Length(c.Data) as Size, RowNumber() over (order by Length(c.Data)) as Total from forwarded c")]
    public void GroupingAndWindows_KeepConservativeStoredValues(string tail)
    {
        using var query = Compile(Definition + tail, new());
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(5, row.GetValue<int?>(0));
        Assert.AreEqual(1L, row.GetValue<long>(1));
    }

    [TestMethod]
    public void DuplicatePublicBufferNames_KeepBothCompleteAllocations()
    {
        using var query = Compile("select EnumerableToArray(f.Content) as Bytes, EnumerableToArray(Skip(f.Content, 1)) as Bytes from #schema.first() f", new());
        using var result = query.Run();
        var row = result.Single();
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6 }, row.GetValue<byte[]>(0));
        CollectionAssert.AreEqual(new byte[] { 2, 3, 4, 5, 6 }, row.GetValue<byte[]>(1));
    }

    [TestMethod]
    public void RecursiveStoredBuffers_KeepTheirCompleteLogicalValues()
    {
        const string sql = "with recursive bytes as (select 0 as Depth, EnumerableToArray(f.Content) as Content from #schema.first() f union all select b.Depth + 1 as Depth, EnumerableToArray(Skip(b.Content, 1)) as Content from bytes b where b.Depth < 2) select Length(b.Content) as Size, FromBytesToInt32(b.Content, true) as Value from bytes b order by b.Depth";
        using var query = Compile(sql, new());
        using var result = query.Run();
        CollectionAssert.AreEqual(new int?[] { 6, 5, 4 }, result.Select(row => row.GetValue<int?>(0)).ToArray());
        CollectionAssert.AreEqual(new int?[] { 0x04030201, 0x05040302, 0x06050403 }, result.Select(row => row.GetValue<int?>(1)).ToArray());
    }

    [TestMethod]
    public void PartialInterpretation_KeepsDiagnosticsAndOwnedPayloadFields()
    {
        var entity = new BufferFlowEntity();
        const string sql = "binary Checked { Prefix: byte[2], Failed: byte check Failed = 0 }; with bytes as (select EnumerableToArray(f.Content) as Content from #schema.first() f) select Length(b.Content) as Size, p.ParsedFields, p.ErrorField, p.BytesConsumed, p.IsSuccess from bytes b cross apply PartialInterpret<Checked>(b.Content) p";
        using var query = Compile(sql, entity);
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(6, row.GetValue<int?>(0));
        Assert.AreEqual("Failed", row.GetValue<string>(2));
        Assert.AreEqual(3, row.GetValue<int>(3));
        Assert.IsFalse(row.GetValue<bool>(4));
        var payload = (IJsonPayload)row.ReadValue(1)!;
        entity.Content[0] = 99;
        Assert.AreEqual("{\"Prefix\":\"AQI=\"}", new LibraryBase().ToJson(payload));
    }

    [TestMethod]
    public void InterpretAt_KeepsCompleteStorageAndAbsoluteOffset()
    {
        using var query = Compile("binary Pair { A: byte, B: byte }; with bytes as (select EnumerableToArray(f.Content) as Content from #schema.first() f) select Length(b.Content) as Size, p.A, p.B from bytes b cross apply InterpretAt<Pair>(b.Content, 3) p", new());
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(6, row.GetValue<int?>(0));
        Assert.AreEqual((byte)4, row.GetValue<byte>(1));
        Assert.AreEqual((byte)5, row.GetValue<byte>(2));
    }

    [TestMethod]
    public void NestedSchemaAndAbsoluteFieldOffsets_UseTheOriginalBufferOrigin()
    {
        using var query = Compile("binary Child { Value: short be }; binary Parent { Prefix: byte, Nested: Child, Tail: byte at 4 }; with bytes as (select EnumerableToArray(f.Content) as Content from #schema.first() f) select Length(b.Content) as Size, p.Nested.Value as Value, p.Tail from bytes b cross apply Interpret<Parent>(b.Content) p", new());
        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(6, row.GetValue<int?>(0));
        Assert.AreEqual((short)0x0203, row.GetValue<short>(1));
        Assert.AreEqual((byte)5, row.GetValue<byte>(2));
    }

    private static CompiledQuery Compile(string sql, BufferFlowEntity entity) => InstanceCreator.CompileForExecution(sql,
        Guid.NewGuid().ToString(), LibraryExecutionFixture.CreateProvider<BufferFlowEntity, BufferFlowLibrary>([entity]),
        new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
}
