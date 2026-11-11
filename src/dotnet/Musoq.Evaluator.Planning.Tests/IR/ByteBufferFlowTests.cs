using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Fixtures;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ByteBufferFlowTests
{
    [TestMethod]
    [DataRow("EnumerableToArray(Skip(f.Content, 1))")]
    [DataRow("MergeArrays(f.Content, f.Content)")]
    [DataRow("FromBase64(ToBase64(f.Content))")]
    public void PrivateAliases_CombineLengthAndIndependentReaderWidths(string producer)
    {
        var inspection = Inspect(
            $"binary Pair {{ A: byte, B: byte }}; with bytes as (select {producer} as Content from #schema.first() f), forwarded as (select b.Content as Data from bytes b) select Length(c.Data) as Size, FromBytesToInt64(c.Data, true) as Value, p.B from forwarded c cross apply Interpret<Pair>(c.Data) p"
        );
        var program = ExecutionIrAnalysis
            .FlattenExpressions(inspection.ExecutionPlan!.Body)
            .OfType<ExecutionByteBufferOperation>()
            .Single(operation => operation.Program is not null)
            .Program!;
        Assert.AreEqual(ExecutionByteBufferResultKind.View, program.Result);
        Assert.AreEqual(8, program.MaximumReadBytes);
        var shape = inspection.ExecutionPlan.Shapes.OfType<GeneratedRecordShape>().Single();
        Assert.IsTrue(shape.EmitAsValueType);
        CollectionAssert.AreEqual(
            new[] { "Storage", "Offset", "AvailableBytes", "LogicalLength" },
            shape.Fields.Select(field => field.Name).ToArray()
        );
        Assert.AreEqual(
            1,
            ExecutionIrAnalysis
                .FlattenExpressions(inspection.ExecutionPlan.Body)
                .OfType<ExecutionByteBufferOperation>()
                .Count(operation => operation.ViewAccess == ExecutionByteBufferViewAccess.LogicalLength)
        );
    }

    [TestMethod]
    public void PrivateLengthOnly_PreservesLogicalLengthWithoutStoredBytes()
    {
        var inspection = Inspect(
            "with bytes as (select EnumerableToArray(Skip(f.Content, 1)) as Content from #schema.first() f) select Length(b.Content) as Size from bytes b"
        );
        var program = ExecutionIrAnalysis
            .FlattenExpressions(inspection.ExecutionPlan!.Body)
            .OfType<ExecutionByteBufferOperation>()
            .Single(operation => operation.Program is not null)
            .Program!;
        Assert.AreEqual(ExecutionByteBufferResultKind.View, program.Result);
        Assert.AreEqual(0, program.MaximumReadBytes);
    }

    [TestMethod]
    [DataRow("b.Content")]
    [DataRow("FromBytesToString(b.Content)")]
    public void PublicOrOpaqueConsumer_RequiresCompleteOwnedStorage(string consumer)
    {
        var inspection = Inspect(
            $"with bytes as (select EnumerableToArray(Skip(f.Content, 1)) as Content from #schema.first() f) select Length(b.Content) as Size, {consumer} as Value from bytes b"
        );
        Assert.IsFalse(
            inspection
                .ExecutionPlan!.Shapes.OfType<GeneratedRecordShape>()
                .Any(shape => shape.TypeName == ByteBufferViewShape.TypeName)
        );
        Assert.IsNull(
            ExecutionIrAnalysis
                .FlattenExpressions(inspection.ExecutionPlan.Body)
                .OfType<ExecutionByteBufferSnapshot>()
                .Single()
                .MaximumReadBytes
        );
    }

    [TestMethod]
    public void RangesAcrossPrivateCtes_PropagateSaturatingPrefixRequirements()
    {
        var inspection = Inspect(
            "with bytes as (select EnumerableToArray(f.Content) as Content from #schema.first() f), sliced as (select EnumerableToArray(Skip(b.Content, 2)) as Data from bytes b) select Length(c.Data) as Size, FromBytesToInt32(c.Data, true) as Value from sliced c"
        );
        var programs = ExecutionIrAnalysis
            .FlattenExpressions(inspection.ExecutionPlan!.Body)
            .Select(expression =>
                expression switch
                {
                    ExecutionByteBufferOperation operation => operation.Program,
                    ExecutionByteBufferSnapshot snapshot => snapshot.Program,
                    _ => null,
                }
            )
            .Where(program => program is not null)
            .ToArray();
        CollectionAssert.AreEquivalent(
            new int?[] { 6, 4 },
            programs.Select(program => program!.MaximumReadBytes).ToArray()
        );
        Assert.AreEqual(1, programs.Count(program => program!.InputIsView));
    }

    [TestMethod]
    public void LengthAfterPrivateRange_RequiresLogicalLengthAndNoStoredBytes()
    {
        var inspection = Inspect(
            "with bytes as (select EnumerableToArray(f.Content) as Content from #schema.first() f) select Length(EnumerableToArray(Skip(b.Content, 2))) as Size from bytes b"
        );
        var programs = ExecutionIrAnalysis
            .FlattenExpressions(inspection.ExecutionPlan!.Body)
            .OfType<ExecutionByteBufferOperation>()
            .Where(operation => operation.Program is not null)
            .Select(operation => operation.Program!)
            .ToArray();
        Assert.AreEqual(2, programs.Length);
        Assert.IsTrue(programs.All(program => program.MaximumReadBytes == 0));
        Assert.AreEqual(1, programs.Count(program => program.InputIsView));
    }

    private static QueryInspectionResult Inspect(string sql) =>
        InstanceCreator.CompileForInspection(
            sql,
            Guid.NewGuid().ToString(),
            LibraryExecutionFixture.CreateProvider<BufferFlowEntity, BufferFlowLibrary>([new()]),
            new TestsLoggerResolver(),
            new CompilationOptions(usePrimitiveTypeValidation: false)
        );
}
