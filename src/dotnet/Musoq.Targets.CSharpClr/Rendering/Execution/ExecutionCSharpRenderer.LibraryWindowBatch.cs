using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{


    private void AddLibraryWindowBatchValues(ExecutionWindowAggregateKernel kernel, List<StatementSyntax> statements)
    {
        if (!kernel.Descriptor.CaptureArguments)
            return;
        ExecutionExpression[] expressions = [kernel.Value, ..kernel.LibraryArguments];
        var index = new ExecutionVariable($"{kernel.Results.Name}CaptureIndex", typeof(int));
        var items = CreateIndexedItemDeclarations(kernel.Item, kernel.Buffer, index, kernel.RowAccessMode);
        var filter = kernel.FilterPredicate is null ? null :
            this.RenderBooleanCondition(kernel.FilterPredicate, CreateIsolatedRenderContext());
        statements.AddRange(LibraryWindowInputCaptureSyntax.Create(kernel, index,
            expressions.Select(RenderExpression).ToArray(), items, filter, CreateBufferCountExpression(kernel.Buffer)));
    }

    private StatementSyntax CreateLibraryWindowBatchPartition(ExecutionWindowAggregateKernel kernel,
        string indices, string start, string count)
        => LibraryWindowBatchSyntax.CreatePartition(kernel, new WindowPartitionPositionSyntax(indices, start, count),
            CreateLibraryWindowFrameLoop(kernel, indices, start, count));
}
