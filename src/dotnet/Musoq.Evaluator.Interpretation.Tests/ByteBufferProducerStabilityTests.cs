using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Fixtures;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ByteBufferProducerStabilityTests
{
    [TestMethod]
    [DataRow("UnmarkedBuffer(f.Self)")]
    [DataRow("case when f.Calls >= 0 then UnmarkedBuffer(f.Self) else Fail(f.Content) end")]
    [DataRow("EnumerableToArray(UnmarkedBuffer(f.Self))")]
    public void OpaqueProducerOccurrences_RemainIndependentAcrossRowsAndRuns(string first)
    {
        var entity = new BufferFlowEntity();
        var sql = $"select Length(EnumerableToArray(Take({first}, 0))) as A, Length(EnumerableToArray(Take({first}, 0))) as B from #schema.first() f";
        using var query = InstanceCreator.CompileForExecution(sql, Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider<BufferFlowEntity, BufferFlowLibrary>([entity, entity]),
            new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));
        using var initial = query.Run();
        CollectionAssert.AreEqual(new int?[] { 0, 0 }, initial.Select(row => row.GetValue<int?>(0)).ToArray());
        Assert.AreEqual(4, entity.Calls);
        using var repeated = query.Run();
        CollectionAssert.AreEqual(new int?[] { 0, 0 }, repeated.Select(row => row.GetValue<int?>(1)).ToArray());
        Assert.AreEqual(8, entity.Calls);
        Assert.AreEqual(0, entity.Events.Count);
    }
}
