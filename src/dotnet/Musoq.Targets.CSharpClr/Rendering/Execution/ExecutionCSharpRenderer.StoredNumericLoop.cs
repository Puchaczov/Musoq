using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private StatementSyntax? RenderStoredNumericConsumer(
        ExecutionForEach loop,
        ExecutionVariable rowsVariable,
        string index,
        StatementSyntax item,
        ExecutionRenderContext context
    )
    {
        if (
            loop.Source is not ExecutionStoredTableRows stored
            || !TryGetTypedStoredTableResult(stored.TableIndex, context, out var slot)
            || slot.NumericConsumer == null
            || IsOperatorProfilingEnabledFor(context)
        )
            return null;
        var rows = rowsVariable.Name;
        var start = index + "Start";
        var length = index + "Length";
        var body = SyntaxFactory.Block();
        if (context.Session.EmitChunkLoopCancellationChecks)
            body = body.AddStatements(
                CreatePeriodicCancellationCheck(start, context.Session.SkipInitialLoopCancellationCheck)
            );
        body = body.AddStatements(
            SyntaxFactory.ParseStatement($"int {length} = Math.Min(64, {rows}.Count - {start});")
        );
        switch (slot.NumericConsumer)
        {
            case StoredNumericFilter filter:
                using (EnterGeneratedRowVariableType(context, loop.Item.Name, rowsVariable.GeneratedRowTypeName!))
                    body = body.AddStatements([
                        .. CreateMatchedRows(
                            rows,
                            start,
                            length,
                            index,
                            item,
                            RenderBlock(filter.MatchedBody, context)
                        ).Statements,
                    ]);
                break;
            case StoredNumericAggregates { GlobalGroup: not null } aggregates:
                body = body.AddStatements([
                    .. CreateStoredReductions(rows, start, length, aggregates, context).Statements,
                ]);
                break;
            default:
                return null;
        }
        var batch = (ForStatementSyntax)
            SyntaxFactory.ParseStatement($"for(int {start}=0; {start} < {rows}.Count;) {{ }}");
        return batch.WithStatement(body.AddStatements(SyntaxFactory.ParseStatement($"{start} += {length};")));
    }

    private static BlockSyntax CreateMatchedRows(
        string rows,
        string start,
        string length,
        string index,
        StatementSyntax item,
        BlockSyntax matched
    )
    {
        var remaining = index + "Matches";
        var consume = (WhileStatementSyntax)SyntaxFactory.ParseStatement($"while ({remaining} != 0) {{ }}");
        var selected = SyntaxFactory
            .Block(
                SyntaxFactory.ParseStatement(
                    $"int {index} = {start} + System.Numerics.BitOperations.TrailingZeroCount({remaining});"
                ),
                SyntaxFactory.ParseStatement($"{remaining} &= {remaining} - 1;"),
                item
            )
            .AddStatements([.. matched.Statements]);
        return SyntaxFactory.Block(
            SyntaxFactory.ParseStatement($"ulong {remaining} = {rows}.Match({start}, {length});"),
            consume.WithStatement(selected)
        );
    }
}
