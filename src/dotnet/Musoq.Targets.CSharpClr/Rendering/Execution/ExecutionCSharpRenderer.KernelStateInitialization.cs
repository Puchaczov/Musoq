using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static IEnumerable<StatementSyntax> RenderInitializeAggregateState(ExecutionInitializeAggregateState initialize)
        => LibraryKernelInvocationSyntax.Initialize(initialize);
    private static StatementSyntax CreateAggregateAccumulatorResetStatement(AggregateAccumulatorField accumulator)
        => SyntaxFactory.ExpressionStatement(CreateKernelStateResetInvocation(accumulator.Kernel.ResetMethod,
            CreateAggregateAccumulatorAccess(SyntaxFactory.ThisExpression(), accumulator, false)));

    private static InvocationExpressionSyntax CreateKernelStateResetInvocation(MethodInfo reset, ExpressionSyntax state)
        => LibraryKernelInvocationSyntax.Reset(reset, state);
}
