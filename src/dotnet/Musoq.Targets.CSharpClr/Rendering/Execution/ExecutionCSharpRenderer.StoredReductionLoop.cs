using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private BlockSyntax CreateStoredReductions(
        string rows,
        string index,
        string length,
        StoredNumericAggregates aggregates,
        ExecutionRenderContext context
    )
    {
        var body = SyntaxFactory.Block(RenderEnsureAggregateGroup(aggregates.GlobalGroup!, context));
        for (int n = 0; n < aggregates.Reductions.Count; n++)
        {
            var reduction = aggregates.Reductions[n];
            var state = CreateAggregateAccumulatorAccess(reduction.Set.Group, reduction.Set.Accumulator);
            var call = $"{rows}.{StoredAggregateReductionSyntax.MethodName(n)}({index}, {length})";
            if (reduction.Kind == AggregateInlineKind.CountAll)
            {
                var count = reduction.Filter == null ? length : $"System.Numerics.BitOperations.PopCount({call})";
                body = body.AddStatements(
                    SyntaxFactory.ParseStatement($"{state}.Count = checked({state}.Count + (long){count});")
                );
            }
            else
            {
                var op = reduction.Kind == AggregateInlineKind.Min ? "Min" : "Max";
                var value = index + StoredAggregateReductionSyntax.MethodName(n);
                body = body.AddStatements(
                    SyntaxFactory.ParseStatement(
                        $"{{ var {value} = {call}; {state}.Value = {state}.HasValue ? Math.{op}({state}.Value,{value}) : {value}; {state}.HasValue = true; }}"
                    )
                );
            }
        }
        return body;
    }
}
