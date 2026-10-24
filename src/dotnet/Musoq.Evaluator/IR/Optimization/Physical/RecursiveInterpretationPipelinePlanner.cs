using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal static class RecursiveInterpretationPipelinePlanner
{
    public static PhysicalNode Normalize(PhysicalNode member, string recursiveName)
    {
        if (member is not PhysicalMultiStatementNode { Statements.Length: >= 3 } statements ||
            statements.Statements[0] is not PhysicalProjectNode { IsDistinct: false,
                Input: PhysicalNestedLoopApplyNode { Kind: ApplyKind.Cross,
                    Left: PhysicalCteRefNode first, Right: PhysicalInterpretSourceNode initial } } producer ||
            first.CteName != recursiveName || !initial.Arguments.All(ExpressionStabilityAnalyzer.IsStable))
            return member;

        var names = IntermediateSourceProducers.Resolve(statements);
        for (var index = 1; index < statements.Statements.Length; index++)
        {
            if (statements.Statements[index] is not PhysicalProjectNode { IsDistinct: false } consumer)
                return member;
            var next = Inline(producer, consumer, names, index - 1);
            if (next == null)
                return member;
            producer = next;
        }

        PhysicalNode input;
        if (producer.Input is PhysicalFilterNode filter)
            input = new PhysicalFilterNode(filter.Predicate, CanonicalizeGuards(filter.Input));
        else
            input = CanonicalizeGuards(producer.Input);
        return new PhysicalMultiStatementNode([new PhysicalProjectNode(producer.Fields, input)]);
    }

    private static PhysicalProjectNode? Inline(PhysicalProjectNode producer, PhysicalProjectNode consumer,
        IReadOnlyDictionary<string, int> names, int producerIndex)
    {
        if (producer.Input is PhysicalFilterNode || producer.Fields.Any(static field =>
                field.Expression is not ColumnRef || !ExpressionStabilityAnalyzer.IsStable(field.Expression)))
            return null;
        var filter = consumer.Input as PhysicalFilterNode;
        var source = filter?.Input ?? consumer.Input;
        var reference = source switch
        {
            PhysicalCteRefNode direct => direct,
            PhysicalNestedLoopApplyNode { Kind: ApplyKind.Cross, Left: PhysicalCteRefNode left,
                Right: PhysicalInterpretSourceNode } => left,
            _ => null
        };
        if (reference == null || !names.TryGetValue(reference.CteName, out var index) || index != producerIndex)
            return null;
        if (source is PhysicalNestedLoopApplyNode { Right: PhysicalInterpretSourceNode interpretation } &&
            !interpretation.Arguments.All(ExpressionStabilityAnalyzer.IsStable))
            return null;

        return RewriteConsumer(producer, consumer, reference);
    }

    private static PhysicalProjectNode? RewriteConsumer(PhysicalProjectNode producer, PhysicalProjectNode consumer,
        PhysicalCteRefNode reference)
    {
        var fields = new Dictionary<string, ColumnRef>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in producer.Fields)
            if (!fields.TryAdd(field.OutputName, (ColumnRef)field.Expression))
                return null;
        var complete = true;
        var rewriter = new ApplyPredicateAliasRewriter(column =>
        {
            if (!string.Equals(column.Alias, reference.Alias, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(column.Alias, reference.CteName, StringComparison.OrdinalIgnoreCase))
                return null;
            if (fields.TryGetValue(column.ColumnName, out var replacement))
                return replacement;
            complete = false;
            return null;
        });
        var rewrittenFields = consumer.Fields.Select(field => field with { Expression = rewriter.Rewrite(field.Expression) }).ToArray();
        var filter = consumer.Input as PhysicalFilterNode;
        var rewrittenInput = RewriteSource(filter?.Input ?? consumer.Input, producer.Input, rewriter);
        if (filter != null)
            rewrittenInput = new PhysicalFilterNode(rewriter.Rewrite(filter.Predicate), rewrittenInput);
        if (!complete)
            return null;
        return new PhysicalProjectNode(rewrittenFields, rewrittenInput);
    }

    private static PhysicalNode RewriteSource(PhysicalNode source, PhysicalNode producer, ApplyPredicateAliasRewriter rewriter)
    {
        if (source is not PhysicalNestedLoopApplyNode apply)
            return producer;
        var right = (PhysicalInterpretSourceNode)apply.Right;
        return new PhysicalNestedLoopApplyNode(apply.Kind, producer,
            right with { Arguments = right.Arguments.Select(rewriter.Rewrite).ToArray() }, apply.WithOrdinality)
        {
            ApplyPredicateMovementPlans = apply.ApplyPredicateMovementPlans.Select(plan => RewriteGuard(plan, rewriter)).ToArray()
        };
    }

    private static ApplyPredicateMovementPlan RewriteGuard(ApplyPredicateMovementPlan plan, ApplyPredicateAliasRewriter rewriter)
    {
        var predicate = rewriter.Rewrite(plan.Predicate);
        var text = IrExpressionPrinter.Print(predicate);
        return plan with
        {
            Predicate = predicate,
            PredicateText = text,
            ResidualPredicateText = text,
            Aliases = AliasRefExtractor.Extract(predicate).ToArray()
        };
    }

    private static PhysicalNode CanonicalizeGuards(PhysicalNode node)
    {
        if (node is not PhysicalNestedLoopApplyNode apply)
            return node;
        return new PhysicalNestedLoopApplyNode(apply.Kind, CanonicalizeGuards(apply.Left), apply.Right, apply.WithOrdinality)
        {
            ApplyPredicateMovementPlans = apply.ApplyPredicateMovementPlans.Select(static plan => plan with
                { ResidualPredicateText = plan.PredicateText }).ToArray()
        };
    }
}
