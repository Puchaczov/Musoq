using System.Collections.Generic;
using System.Threading;
using Musoq.Schema.Optimization;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.SourcePlanning;

/// <summary>Bounds provider graphs before any recursive predicate consumer runs.</summary>
internal static class SourcePredicateGraphValidator
{
    internal const int MaximumDepth = 256;
    internal const int MaximumOccurrences = 100_000;

    public static bool IsWithinBudget(IrExpression predicate, CancellationToken cancellationToken)
    {
        var pending = new Stack<(IrExpression Node, int Depth)>();
        pending.Push((predicate, 1));
        var occurrences = 0;
        while (pending.TryPop(out var frame))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (frame.Node == null || frame.Depth > MaximumDepth || ++occurrences > MaximumOccurrences)
                return false;
            foreach (var child in IrExpressionTraversal.Children(frame.Node))
                pending.Push((child, frame.Depth + 1));
        }
        return true;
    }

    private readonly record struct Frame(SourcePredicateExpression Node, int Depth, bool Exiting, bool RequiresBoolean);
    private readonly record struct Shape(bool Supported, IEnumerable<Frame> Children);

    public static bool IsValid(SourcePredicateExpression? predicate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (predicate == null) return true;
        var active = new HashSet<SourcePredicateExpression>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Frame>();
        pending.Push(new(predicate, 1, false, true));
        var occurrences = 0;
        while (pending.TryPop(out var frame))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (frame.Exiting)
            {
                active.Remove(frame.Node);
                continue;
            }
            if (frame.Node == null || frame.Depth > MaximumDepth || ++occurrences > MaximumOccurrences || !active.Add(frame.Node))
                return false;
            var shape = DescribeShape(frame);
            if (!shape.Supported) return false;
            pending.Push(frame with { Exiting = true });
            foreach (var child in shape.Children) pending.Push(child);
        }
        return true;
    }

    private static Shape DescribeShape(Frame frame)
    {
        if (frame.RequiresBoolean && frame.Node is SourcePredicateColumn or SourcePredicateEnumLiteral ||
            frame.RequiresBoolean && frame.Node is SourcePredicateLiteral { Value: not bool })
            return new(false, []);
        var depth = frame.Depth + 1;
        return frame.Node switch
        {
            SourcePredicateLogical logical when Enum.IsDefined(logical.Operator) =>
                new(true, [new(logical.Right, depth, false, true), new(logical.Left, depth, false, true)]),
            SourcePredicateComparison comparison when Enum.IsDefined(comparison.Operator) =>
                new(true, [new(comparison.Right, depth, false, false), new(comparison.Left, depth, false, false)]),
            SourcePredicateIn membership when membership.Values != null && membership.Values.Count <= MaximumOccurrences =>
                new(true, MembershipChildren(membership, depth)),
            SourcePredicateNullCheck check => new(true, [new(check.Expression, depth, false, false)]),
            SourcePredicateFlags flags => new(true, [new(flags.Mask, depth, false, false), new(flags.Expression, depth, false, false)]),
            SourcePredicateColumn { Column: { Name: { } name } } when !string.IsNullOrWhiteSpace(name) => new(true, []),
            SourcePredicateLiteral or SourcePredicateEnumLiteral or SourcePredicateStringMatch => new(true, []),
            _ => new(false, [])
        };
    }

    private static IEnumerable<Frame> MembershipChildren(SourcePredicateIn membership, int depth)
    {
        foreach (var value in membership.Values) yield return new(value, depth, false, false);
        yield return new(membership.Expression, depth, false, false);
    }
}
