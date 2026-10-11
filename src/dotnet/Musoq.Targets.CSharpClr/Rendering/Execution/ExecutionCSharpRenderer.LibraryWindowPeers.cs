using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private StatementSyntax CreateLibraryWindowPeerLoop(ExecutionWindowAggregateKernel kernel,
        string indices, string start, string count)
    {
        var current = new ExecutionVariable($"{kernel.Results.Name}CurrentIndex", typeof(int));
        var items = string.Join(Environment.NewLine,
            CreateIndexedItemDeclarations(kernel.Item, kernel.Buffer, current, kernel.RowAccessMode)
                .Select(static statement => statement.NormalizeWhitespace().ToFullString()));
        var accumulate = string.Join(Environment.NewLine, CreateWindowAggregateAccumulateStatements(kernel)
            .Select(static statement => statement.NormalizeWhitespace().ToFullString()));
        var keys = ResolveAggregateOrderKeyArray(kernel) ??
            throw new InvalidOperationException("Running peer evaluation requires concrete ordering keys.");
        return WindowRangeFrameSyntax.CreateStreamingPeerRowsLoop(kernel.Results.Name,
            CreateWindowAggregateValueExpression(kernel).NormalizeWhitespace().ToFullString(),
            keys.Variable.Name, indices, start, count, current.Name, items, accumulate);
    }
}
