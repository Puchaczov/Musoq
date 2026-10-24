using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class PagingCountExecution
{
    internal static ExecutionVariable Variable(PagingCount count) =>
        new($"__musoqPaging_{count.ParameterName}", typeof(int));

    internal static ExecutionExpression Read(PagingCount count) => count.Constant is { } value
        ? new ExecutionLiteral(value, typeof(int))
        : new ExecutionVariableRead(Variable(count));

    internal static ExecutionPlan AddValidation(PhysicalNode root, ExecutionPlan plan)
    {
        var declarations = new List<ExecutionNode>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (count, clause) in Counts(root))
        {
            if (count.IsConstant || !seen.Add(count.ParameterName!))
                continue;
            var type = count.ParameterType!.ResolveClrType();
            var underlying = Nullable.GetUnderlyingType(type);
            var methodName = underlying == null ? nameof(PagingCountValidator.Validate) : nameof(PagingCountValidator.ValidateNullable);
            var method = typeof(PagingCountValidator).GetMethod(methodName)!.MakeGenericMethod(underlying ?? type);
            var call = new ExecutionMethodCall(method,
                [new ExecutionScriptParameterRead(count.ParameterName!, type),
                 new ExecutionLiteral(clause, typeof(string)), new ExecutionLiteral(count.ParameterName!, typeof(string))],
                null, typeof(int));
            declarations.Add(new ExecutionLet(Variable(count), call)
            {
                DeclarationScope = ExecutionDeclarationScope.Execution
            });
        }
        if (declarations.Count == 0)
            return plan;
        declarations.AddRange(plan.Body.Nodes);
        return plan with { Body = new ExecutionBlock(declarations) };
    }

    private static IEnumerable<(PagingCount Count, string Clause)> Counts(PhysicalNode node)
    {
        switch (node)
        {
            case PhysicalSkipNode skip: yield return (skip.Count, "SKIP"); break;
            case PhysicalTakeNode take: yield return (take.Count, "TAKE"); break;
            case PhysicalTopNNode top: yield return (top.N, "TAKE"); break;
            case PhysicalTopOffsetNode top:
                yield return (top.Skip, "SKIP");
                yield return (top.Take, "TAKE");
                break;
        }
        foreach (var child in node.Children)
            foreach (var count in Counts(child))
                yield return count;
    }

    internal static IReadOnlyList<ExecutionExpression> Reads(ExecutionNode node) => node switch
    {
        ExecutionSkipTable skip => Reads(skip.Count),
        ExecutionTakeTable take => Reads(take.Count),
        ExecutionSliceTable slice => Reads(slice.SkipCount, slice.TakeCount),
        ExecutionTopNTable top => Reads(top.Count),
        ExecutionTopOffsetTable top => Reads(top.SkipCount, top.TakeCount),
        ExecutionCreateBoundedRecordList bounded => Reads(bounded.Selection),
        ExecutionOrderRecordList order => Reads(order.Selection),
        _ => []
    };

    private static IReadOnlyList<ExecutionExpression> Reads(ExecutionOrderRecordSelection selection) => selection switch
    {
        ExecutionTakeOrderRecordSelection take => Reads(take.Count),
        ExecutionSkipTakeOrderRecordSelection slice => Reads(slice.SkipCount, slice.TakeCount),
        _ => []
    };

    private static IReadOnlyList<ExecutionExpression> Reads(PagingCount count) => count.IsConstant ? [] : [Read(count)];

    private static IReadOnlyList<ExecutionExpression> Reads(PagingCount skip, PagingCount take)
    {
        if (skip.IsConstant)
            return Reads(take);
        if (take.IsConstant)
            return Reads(skip);
        return [Read(skip), Read(take)];
    }
}
