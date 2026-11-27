using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> CreateWindowAggregateAccumulateStatements(
        ExecutionRenderContext renderContext,
        ExecutionWindowAggregateKernel kernel
    )
    {
        var library =
            kernel.Descriptor.LibraryKernel
            ?? throw new InvalidOperationException("Window accumulation requires a registered typed library kernel.");
        ExecutionExpression[] values = [kernel.Value, .. kernel.LibraryArguments];
        var update = LibraryKernelInvocationSyntax.Set(
            library,
            CreateLibraryWindowStateName(kernel),
            values.Select(windowInput => RenderExpression(windowInput, renderContext)).ToArray()
        );
        if (kernel.FilterPredicate is null)
            return [update];
        return
        [
            StatementEmitter.CreateIf(
                this.RenderBooleanCondition(kernel.FilterPredicate, renderContext),
                StatementEmitter.CreateBlock(update)
            ),
        ];
    }

    private static ExpressionSyntax CreateWindowAggregateValueExpression(ExecutionWindowAggregateKernel kernel) =>
        CreateLibraryWindowResult(kernel);
}
