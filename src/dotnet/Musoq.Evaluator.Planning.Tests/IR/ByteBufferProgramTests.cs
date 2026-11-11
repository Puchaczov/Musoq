using System;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Fixtures;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ByteBufferProgramTests
{
    [TestMethod]
    [DataRow("EnumerableToArray(SkipAndTake(f.Content, 1, 5))", 0)]
    [DataRow("EnumerableToArray(Skip(MergeArrays(f.Content, f.Content), 5))", 2)]
    [DataRow("FromBase64(ToBase64(f.Content))", 0)]
    [DataRow("FromHexToBytes(ToHex(f.Content))", 0)]
    public void ProducerFamilies_ShareBoundedSnapshotPrograms(string expression, int segments)
    {
        var sql =
            $"binary Independent {{ First: short le, Second: short be }}; select p.First, p.Second from #schema.first() f cross apply Interpret<Independent>({expression}) p";
        var inspection = Inspect(sql);
        var snapshot = ExecutionIrAnalysis
            .FlattenExpressions(inspection.ExecutionPlan!.Body)
            .OfType<ExecutionByteBufferSnapshot>()
            .Single();
        Assert.AreEqual(4, snapshot.MaximumReadBytes);
        Assert.IsNotNull(snapshot.Program);
        Assert.AreEqual(segments, snapshot.Program.SegmentCount);
        Assert.AreEqual(ExecutionByteBufferResultKind.Snapshot, snapshot.Program.Result);
        Assert.IsFalse(
            ExecutionIrAnalysis
                .FlattenExpressions(inspection.ExecutionPlan.Body)
                .OfType<ExecutionMethodCall>()
                .Any(call =>
                    call.BufferSemantics?.Kind
                        is Musoq.Evaluator.IR.Expressions.ByteBufferCallableKind.Encode
                            or Musoq.Evaluator.IR.Expressions.ByteBufferCallableKind.Decode
                )
        );
    }

    [TestMethod]
    public void FixedScalarAndInterpretation_UseTheSameFourByteRequirement()
    {
        var inspection = Inspect(
            "select FromBytesToInt32(EnumerableToArray(Skip(f.Content, 1))) as Value from #schema.first() f"
        );
        var snapshot = ExecutionIrAnalysis
            .FlattenExpressions(inspection.ExecutionPlan!.Body)
            .OfType<ExecutionByteBufferSnapshot>()
            .Single();
        Assert.AreEqual(4, snapshot.Program!.MaximumReadBytes);
        Assert.AreEqual(ExecutionByteBufferResultKind.Snapshot, snapshot.Program.Result);
    }

    [TestMethod]
    public void LogicalLength_RequiresProducerEvaluationWithoutByteCopies()
    {
        var inspection = Inspect(
            "select Length(EnumerableToArray(Skip(Produce(f.Content), 1))) as Value from #schema.first() f"
        );
        var operation = ExecutionIrAnalysis
            .FlattenExpressions(inspection.ExecutionPlan!.Body)
            .OfType<ExecutionByteBufferOperation>()
            .Single(operation => operation.Program is not null);
        Assert.AreEqual(ExecutionByteBufferResultKind.Length, operation.Program!.Result);
        Assert.AreEqual(0, operation.Program.MaximumReadBytes);
        Assert.AreEqual(
            1,
            ExecutionIrAnalysis
                .FlattenExpressions(inspection.ExecutionPlan.Body)
                .OfType<ExecutionMethodCall>()
                .Count(call => call.IsOpaqueBufferProducer)
        );
    }

    [TestMethod]
    public void ProgramIdentity_IsIndependentOfCurrentNumberFormatting()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var first = Inspect(
                "select FromBytesToInt32(EnumerableToArray(Skip(f.Content, -2))) as First from #schema.first() f"
            );
            var custom = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            custom.NumberFormat.NegativeSign = "minus";
            CultureInfo.CurrentCulture = custom;
            var second = Inspect(
                "select FromBytesToInt32(EnumerableToArray(Skip(f.Content, -2))) as Second from #schema.first() f"
            );
            var firstName = ExecutionIrAnalysis
                .FlattenExpressions(first.ExecutionPlan!.Body)
                .OfType<ExecutionByteBufferSnapshot>()
                .Single()
                .Program!.HelperName;
            var secondName = ExecutionIrAnalysis
                .FlattenExpressions(second.ExecutionPlan!.Body)
                .OfType<ExecutionByteBufferSnapshot>()
                .Single()
                .Program!.HelperName;
            Assert.AreEqual(firstName, secondName);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
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
