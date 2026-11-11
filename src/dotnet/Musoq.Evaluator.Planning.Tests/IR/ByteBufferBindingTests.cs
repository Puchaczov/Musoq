using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Rewriting;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Fixtures;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ByteBufferBindingTests
{
    [TestMethod]
    [DataRow("EnumerableToArray(Skip(f.Content, 1))", "Skip", "Skip", 0)]
    [DataRow("EnumerableToArray(Take(f.Content, 1))", "Take", "Take", 0)]
    [DataRow("EnumerableToArray(SkipAndTake(f.Content, 1, 2))", "SkipAndTake", "SkipAndTake", 0)]
    [DataRow("EnumerableToArray(f.Content)", "EnumerableToArray", "Materialize", 0)]
    [DataRow("MergeArrays(f.Content, f.Content)", "MergeArrays", "Concatenate", 0)]
    [DataRow("Length(f.Content)", "Length", "Length", 0)]
    [DataRow("FromBytesToInt16(f.Content)", "FromBytesToInt16", "FixedWidthRead", 2)]
    [DataRow("FromBytesToInt32(f.Content, true)", "FromBytesToInt32", "FixedWidthRead", 4)]
    [DataRow("FromBytesToDouble(f.Content)", "FromBytesToDouble", "FixedWidthRead", 8)]
    [DataRow("ToBase64(f.Content)", "ToBase64", "Encode", 0)]
    [DataRow("FromBase64(ToBase64(f.Content))", "FromBase64", "Decode", 0)]
    [DataRow("ToHex(f.Content)", "ToHex", "Encode", 0)]
    public void BoundByteDeclarations_CarryTypedSemanticsThroughExecutableLowering(string expression,
        string method, string kind, int width)
    {
        var inspection = Inspect(expression);
        var call = BoundCalls(inspection.LogicalPlan).Single(call => call.Method.Name == method);
        Assert.IsNotNull(call.BufferSemantics);
        Assert.AreEqual(kind, call.BufferSemantics.Kind.ToString());
        Assert.AreEqual(width, call.BufferSemantics.ReadWidth);
    }

    [TestMethod]
    public void NonByteGenericOverload_RemainsOpaque()
    {
        var library = new LibraryBase();
        Func<IEnumerable<int>?, int, IEnumerable<int>?> callable = library.Skip<int>;
        Assert.IsNull(ByteBufferMethodBinding.Describe(callable.Method));
    }

    [TestMethod]
    public void ShadowedMethod_RemainsOpaque()
    {
        var library = new ShadowBufferLibrary();
        Func<byte[]?, string?> callable = library.ToBase64;
        Assert.IsNull(ByteBufferMethodBinding.Describe(callable.Method));
    }

    [TestMethod]
    public void UnboundedReader_RemainsOpaque()
    {
        var library = new LibraryBase();
        Func<byte[], string> callable = library.FromBytesToString;
        Assert.IsNull(ByteBufferMethodBinding.Describe(callable.Method));
    }

    [TestMethod]
    public void InitialMaterialization_ExecutesTheCompleteOriginalCallable()
    {
        var inspection = Inspect("EnumerableToArray(Sequence(f.Content))");
        var operation = ExecutionIrAnalysis.FlattenExpressions(inspection.ExecutionPlan!.Body)
            .OfType<ExecutionByteBufferOperation>().Single(operation => operation.Operation == ExecutionByteBufferOperationKind.Materialize);
        Assert.AreEqual(ExecutionByteBufferOperationKind.Materialize, operation.Operation);
        Assert.AreEqual(ExecutionByteBufferMaterialization.OriginalCallable, operation.Materialization);
        Assert.IsNull(operation.MaximumReadBytes);
        Assert.IsFalse(ExpressionStabilityAnalyzer.IsStable(operation));
        Assert.AreEqual("expr.byte-buffer.operation", ExecutionOperationCatalog.Resolve(operation).Value);
    }

    [TestMethod]
    public void ArrayRange_RepresentsBorrowRangeAndMaterializationWithoutChangingCalls()
    {
        var inspection = Inspect("EnumerableToArray(RelaySequence(Skip(f.Content, 1)))");
        var operations = ExecutionIrAnalysis.FlattenExpressions(inspection.ExecutionPlan!.Body)
            .OfType<ExecutionByteBufferOperation>().ToArray();
        CollectionAssert.AreEquivalent(new[] { ExecutionByteBufferOperationKind.BorrowArray, ExecutionByteBufferOperationKind.Range,
            ExecutionByteBufferOperationKind.Materialize }, operations.Select(operation => operation.Operation).ToArray());
        Assert.AreEqual(3, operations.Select(operation => operation.ValueId).Distinct().Count());
        Assert.IsTrue(operations.All(operation => operation.Materialization == ExecutionByteBufferMaterialization.OriginalCallable));
    }

    [TestMethod]
    public void ConsumerRequirements_CombinePrefixAndLogicalLength()
    {
        var combined = ByteBufferRequirement.Prefix(2).Combine(ByteBufferRequirement.Length)
            .Combine(ByteBufferRequirement.Prefix(8));
        Assert.AreEqual(8, combined.MaximumReadBytes);
        Assert.IsTrue(combined.ObservesLength);
        Assert.IsFalse(combined.RequiresCompleteArray);
    }

    [TestMethod]
    public void CompleteConsumer_DominatesBoundedReadRequirements()
    {
        var combined = ByteBufferRequirement.Prefix(4).Combine(ByteBufferRequirement.Complete);
        Assert.IsNull(combined.MaximumReadBytes);
        Assert.IsTrue(combined.RequiresCompleteArray);
    }

    [TestMethod]
    public void BufferPlan_FreezesArgumentsAtItsEvaluationPoint()
    {
        IrExpression[] arguments = [new Literal(null, typeof(byte[]))];
        var region = new ScalarEvaluationRegion("test", ScalarEvaluationRegionKind.Materialization, false, false);
        var plan = new ByteBufferValuePlan(ByteBufferOperationKind.Snapshot,
            new(7, ByteBufferOwnership.Owned, ByteBufferNullState.Unknown, region, 7), arguments,
            ByteBufferRequirement.Prefix(4));
        arguments[0] = new Literal("changed", typeof(string));
        Assert.AreEqual(typeof(byte[]), plan.Arguments[0].ReturnType);
        Assert.AreEqual(7, plan.Facts.MaterializationPoint);
        Assert.IsFalse(plan.Facts.Region.AllowsHoisting);
    }

    private static QueryInspectionResult Inspect(string expression) => InstanceCreator.CompileForInspection(
        $"select {expression} as Value from #schema.first() f", Guid.NewGuid().ToString(),
        LibraryExecutionFixture.CreateProvider<BufferFlowEntity, BufferFlowLibrary>([new()]),
        new TestsLoggerResolver(), new CompilationOptions(usePrimitiveTypeValidation: false));

    private static IEnumerable<MethodCall> BoundCalls(LogicalNode root)
    {
        var collector = new BoundCallCollector();
        Visit(root);
        return collector.Calls;

        void Visit(LogicalNode node)
        {
            LogicalPlanRewriter.RewriteLocalExpressions(node, collector.Visit);
            foreach (var child in node.Children)
                Visit(child);
        }
    }

    private sealed class BoundCallCollector : IrExpressionRewriter
    {
        internal List<MethodCall> Calls { get; } = [];
        protected override IrExpression VisitMethodCall(MethodCall node)
        {
            Calls.Add(node);
            return base.VisitMethodCall(node);
        }
    }
}
