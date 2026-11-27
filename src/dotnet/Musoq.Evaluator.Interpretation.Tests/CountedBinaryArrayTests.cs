using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Fixtures;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CountedBinaryArrayTests
{
    [TestMethod]
    [DataRow(0, 4, true, 4)]
    [DataRow(2, 8, true, 8)]
    [DataRow(2, 7, false, 6)]
    [DataRow(int.MaxValue, 7, false, 6)]
    public void CountedPrimitivesPreservePartialReadPosition(int count, int available, bool success, int consumed)
    {
        const string sql =
            "binary Packet { Count: int le, Items: short le[Count] } select p.IsSuccess, p.BytesConsumed, p.ErrorField from #schema.first() f cross apply PartialInterpret<Packet>(f.Content) p";
        var data = new byte[available];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(data, count);
        var provider = LibraryExecutionFixture.CreateProvider<BinaryEntity>([new() { Content = data }]);
        using var query = InstanceCreator.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );

        using var result = query.Run();
        var row = result.Single();
        Assert.AreEqual(success, row.GetValue<bool>(0));
        Assert.AreEqual(consumed, row.GetValue<int>(1));
        Assert.AreEqual(success ? null : "Items", row.GetValue<string?>(2));
    }
}
