using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class AllocationTraversalTests
{
    public static IEnumerable<object[]> NodeCases => AllocationTraversalFixtures.NodeCases();
    public static IEnumerable<object[]> ExpressionCases => AllocationTraversalFixtures.ExpressionCases()
        .Where(static sample => (Type)sample[0] != typeof(ExecutionSubquery) && (Type)sample[0] != typeof(ExecutionUnionMemberRead));

    [TestMethod]
    public void UnionMemberTraversal_ShouldVisitItsReceiverExactlyOnce()
    {
        var receiver = new ExecutionLiteral(null, typeof(object));
        var expression = new ExecutionUnionMemberRead(receiver, "Name", ExecutionClrBindingFactory.FromClr(typeof(string)),
            [new ExecutionUnionMemberCase("Generated.Record", "Name")]);
        var visitor = new ExpressionCollector([]);

        ExecutionIrTraversal.VisitExpressions(expression, ref visitor);

        AssertReferenceSequence(new ExecutionExpression[] { expression, receiver }, visitor.Values);
    }

    [TestMethod]
    public void SubqueryTraversal_ShouldVisitCapturesAndOperandWithoutEnteringTheInnerScope()
    {
        var capture = new ExecutionLiteral(1, typeof(int));
        var operand = new ExecutionLiteral(2, typeof(int));
        var inner = new ExecutionPlan("inner", [], new ExecutionBlock([
            new ExecutionLet(new ExecutionVariable("local", typeof(int)), new ExecutionLiteral(3, typeof(int)))
        ]));
        var expression = new ExecutionSubquery("inner", SubqueryResultKind.In, inner, [capture], operand,
            ExecutionClrBindingFactory.FromClr(typeof(bool?)));
        var visitor = new ExpressionCollector([]);

        ExecutionIrTraversal.VisitExpressions(expression, ref visitor);

        AssertReferenceSequence(new ExecutionExpression[] { expression, capture, operand }, visitor.Values);
    }

    [TestMethod]
    [DynamicData(nameof(NodeCases))]
    public void NodeTraversal_ShouldMatchPreviousPreorder(Type type, bool includeOptional)
    {
        var node = (ExecutionNode)new AllocationTraversalFixtures(includeOptional).Create(type);
        var expected = PreviousExecutionIrAnalysis.FlattenNodes(new ExecutionBlock([node])).ToArray();
        var visitor = new NodeCollector([]);

        ExecutionIrTraversal.VisitNode(node, ref visitor);

        AssertReferenceSequence(expected, visitor.Values);
    }

    [TestMethod]
    [DynamicData(nameof(NodeCases))]
    public void LocalExpressions_ShouldMatchPreviousOrder(Type type, bool includeOptional)
    {
        var node = (ExecutionNode)new AllocationTraversalFixtures(includeOptional).Create(type);
        var expected = PreviousExecutionIrAnalysis.GetNodeExpressions(node).ToArray();

        var actual = ExecutionIrAnalysis.GetNodeExpressions(node).ToArray();

        AssertExpressionSequence(expected, actual);
    }

    [TestMethod]
    [DynamicData(nameof(NodeCases))]
    public void BlockExpressions_ShouldMatchPreviousPreorder(Type type, bool includeOptional)
    {
        var node = (ExecutionNode)new AllocationTraversalFixtures(includeOptional).Create(type);
        var block = new ExecutionBlock([node]);
        var expected = PreviousExecutionIrAnalysis.FlattenExpressions(block).ToArray();
        var visitor = new ExpressionCollector([]);

        ExecutionIrTraversal.VisitExpressions(block, ref visitor);

        AssertExpressionSequence(expected, visitor.Values);
    }

    [TestMethod]
    [DynamicData(nameof(ExpressionCases))]
    public void ExpressionTraversal_ShouldMatchPreviousPreorder(Type type, bool includeOptional)
    {
        var expression = (ExecutionExpression)new AllocationTraversalFixtures(includeOptional).Create(type);
        var expected = PreviousExecutionIrAnalysis.FlattenExpressions(expression).ToArray();
        var visitor = new ExpressionCollector([]);

        ExecutionIrTraversal.VisitExpressions(expression, ref visitor);

        AssertReferenceSequence(expected, visitor.Values);
    }

    [TestMethod]
    public void SharedSubtrees_ShouldRemainRepeatedInPreorder()
    {
        var shared = new ExecutionLiteral(1, typeof(int));
        var expression = new ExecutionBinary(BinaryOpKind.Add, shared, shared, typeof(int));
        var visitor = new ExpressionCollector([]);

        ExecutionIrTraversal.VisitExpressions(expression, ref visitor);

        AssertReferenceSequence(new ExecutionExpression[] { expression, shared, shared }, visitor.Values);
    }

    [TestMethod]
    public void EarlyExit_ShouldStopBeforeTheNextChild()
    {
        var root = new ExecutionBinary(BinaryOpKind.Add, new ExecutionLiteral(1, typeof(int)), new ExecutionLiteral(2, typeof(int)), typeof(int));
        var visitor = new StopVisitor();

        var completed = ExecutionIrTraversal.VisitExpressions(root, ref visitor);

        Assert.AreEqual((false, 1), (completed, visitor.Count));
    }

    [TestMethod]
    public void CancellationFromVisitor_ShouldBeObservedBeforeNextChild()
    {
        using var source = new CancellationTokenSource();
        var expression = new ExecutionBinary(BinaryOpKind.Add, new ExecutionLiteral(1, typeof(int)), new ExecutionLiteral(2, typeof(int)), typeof(int));

        Assert.ThrowsExactly<OperationCanceledException>(() => TraverseCanceled(expression, source));
    }

    [TestMethod]
    public void VisitorException_ShouldNotAffectLaterTraversal()
    {
        var expression = new ExecutionLiteral(1, typeof(int));

        Assert.ThrowsExactly<InvalidOperationException>(() => TraverseThrowing(expression));

        var visitor = new ExpressionCollector([]);
        ExecutionIrTraversal.VisitExpressions(expression, ref visitor);
        AssertReferenceSequence(new ExecutionExpression[] { expression }, visitor.Values);
    }

    [TestMethod]
    public void ConcurrentTraversals_ShouldKeepVisitorStateIndependent()
    {
        var expression = new ExecutionLiteral(1, typeof(int));

        var results = Enumerable.Range(0, 64).AsParallel().Select(_ => Collect(expression)).ToArray();

        Assert.IsTrue(results.All(result => result.Count == 1 && ReferenceEquals(result[0], expression)));
    }

    [TestMethod]
    public void DeepExpression_ShouldMatchPreviousTraversal()
    {
        var expression = DeepExpression();
        var expected = PreviousExecutionIrAnalysis.FlattenExpressions(expression).ToArray();

        var actual = Collect(expression);

        AssertReferenceSequence(expected, actual);
    }

    [TestMethod]
    public void DeepNodePlan_ShouldMatchPreviousPreorder()
    {
        var root = DeepNodePlan();
        var expected = PreviousExecutionIrAnalysis.FlattenNodes(new ExecutionBlock([root])).ToArray();
        var visitor = new NodeCollector([]);

        ExecutionIrTraversal.VisitNode(root, ref visitor);

        AssertReferenceSequence(expected, visitor.Values);
    }

    [TestMethod]
    public void SharedNodes_ShouldRemainRepeatedInPreorder()
    {
        var shared = new ExecutionBreak();
        var block = new ExecutionBlock([shared, shared]);
        var visitor = new NodeCollector([]);

        ExecutionIrTraversal.VisitNodes(block, ref visitor);

        AssertReferenceSequence(new ExecutionNode[] { shared, shared }, visitor.Values);
    }

    [TestMethod]
    public void NodeCancellationFromVisitor_ShouldBeObservedBeforeNextChild()
    {
        using var source = new CancellationTokenSource();
        var root = new ExecutionIf(new ExecutionLiteral(true, typeof(bool)), new ExecutionBlock([new ExecutionBreak()]));
        var visitor = new CancelNodeVisitor(source);

        Assert.ThrowsExactly<OperationCanceledException>(() => ExecutionIrTraversal.VisitNode(root, ref visitor, source.Token));
    }

    [TestMethod]
    public void UnknownNode_ShouldRemainAChildlessPreorderEntry()
    {
        var node = new UnknownNode();
        var visitor = new NodeCollector([]);

        ExecutionIrTraversal.VisitNode(node, ref visitor);

        AssertReferenceSequence(new ExecutionNode[] { node }, visitor.Values);
    }

    [TestMethod]
    public void LeafChildAdapter_ShouldReuseTheEmptyCollection()
    {
        var expression = new ExecutionLiteral(1, typeof(int));

        var children = ExecutionIrAnalysis.GetChildExpressions(expression);

        Assert.AreSame(Array.Empty<ExecutionExpression>(), children);
    }

    [TestMethod]
    public void EmptyLocalAdapter_ShouldReuseTheEmptyCollection()
    {
        var node = new ExecutionBreak();

        var expressions = ExecutionIrAnalysis.GetNodeExpressions(node);

        Assert.AreSame(Array.Empty<ExecutionExpression>(), expressions);
    }

    [TestMethod]
    public void MethodArgumentAdapter_ShouldReuseTheOwnedArgumentList()
    {
        var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!;
        var call = new ExecutionMethodCall(method, [new ExecutionLiteral(1, typeof(int))], null, typeof(int));

        var children = ExecutionIrAnalysis.GetChildExpressions(call);

        Assert.AreSame(call.Arguments, children);
    }

    [TestMethod]
    public void AlreadyCanceledEmptyBlock_ShouldThrowBeforeCompletion()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var visitor = new NodeCollector([]);

        Assert.ThrowsExactly<OperationCanceledException>(() => ExecutionIrTraversal.VisitNodes(ExecutionBlock.Empty, ref visitor, source.Token));
    }

    private sealed record UnknownNode : ExecutionNode;

    private static ExecutionExpression DeepExpression()
    {
        ExecutionExpression current = new ExecutionLiteral(1, typeof(int));
        for (var index = 0; index < 512; index++)
            current = new ExecutionUnary(UnaryOpKind.Negate, current, typeof(int));
        return current;
    }

    private static ExecutionNode DeepNodePlan()
    {
        ExecutionNode current = new ExecutionBreak();
        var condition = new ExecutionLiteral(true, typeof(bool));
        for (var index = 0; index < 512; index++)
            current = new ExecutionIf(condition, new ExecutionBlock([current]));
        return current;
    }

    private static void AssertReferenceSequence<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual) where T : class =>
        Assert.IsTrue(expected.Count == actual.Count && expected.Where((value, index) => !ReferenceEquals(value, actual[index])).Any() == false);

    private static void AssertExpressionSequence(IReadOnlyList<ExecutionExpression> expected,
        IReadOnlyList<ExecutionExpression> actual) => Assert.IsTrue(expected.Count == actual.Count &&
        expected.Select((value, index) => SameExpression(value, actual[index])).All(same => same));

    private static bool SameExpression(ExecutionExpression expected, ExecutionExpression actual) =>
        ReferenceEquals(expected, actual) || EquivalentSynthesizedPagingRead(expected, actual);

    private static bool EquivalentSynthesizedPagingRead(ExecutionExpression expected, ExecutionExpression actual) =>
        expected is ExecutionVariableRead read &&
        read.Variable.Name.StartsWith("__musoqPaging_", StringComparison.Ordinal) && expected == actual;

    private static List<ExecutionExpression> Collect(ExecutionExpression expression)
    {
        var visitor = new ExpressionCollector([]);
        ExecutionIrTraversal.VisitExpressions(expression, ref visitor);
        return visitor.Values;
    }

    private static void TraverseCanceled(ExecutionExpression expression, CancellationTokenSource source)
    {
        var visitor = new CancelVisitor(source);
        ExecutionIrTraversal.VisitExpressions(expression, ref visitor, source.Token);
    }

    private static void TraverseThrowing(ExecutionExpression expression)
    {
        var visitor = new ThrowVisitor();
        ExecutionIrTraversal.VisitExpressions(expression, ref visitor);
    }

    private readonly record struct NodeCollector(List<ExecutionNode> Values) : IExecutionNodeVisitor
    {
        public bool Visit(ExecutionNode node) { Values.Add(node); return true; }
    }

    private readonly record struct ExpressionCollector(List<ExecutionExpression> Values) : IExecutionExpressionVisitor
    {
        public bool Visit(ExecutionExpression expression) { Values.Add(expression); return true; }
    }

    private struct StopVisitor : IExecutionExpressionVisitor
    {
        public int Count;
        public bool Visit(ExecutionExpression expression) { Count++; return false; }
    }

    private readonly record struct CancelVisitor(CancellationTokenSource Source) : IExecutionExpressionVisitor
    {
        public bool Visit(ExecutionExpression expression) { Source.Cancel(); return true; }
    }

    private readonly record struct CancelNodeVisitor(CancellationTokenSource Source) : IExecutionNodeVisitor
    {
        public bool Visit(ExecutionNode node) { Source.Cancel(); return true; }
    }

    private readonly struct ThrowVisitor : IExecutionExpressionVisitor
    {
        public bool Visit(ExecutionExpression expression) => throw new InvalidOperationException("Probe failure.");
    }
}
