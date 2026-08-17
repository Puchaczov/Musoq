using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Runtime;

namespace Musoq.Targets.CSharpClr;

public sealed partial class CSharpRenderer
{
    private static InvocationExpressionSyntax CreateRowShardedReturnExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName,
        ExecutionRenderContext? renderContext = null)
    {
        return CreateQueryRowsShardInvocation(
            nameof(QueryRows.FromRowShards),
            CreateProjectRowsParallelInvocation(
                resultInfo,
                executionRenderer,
                projectionLoop,
                parallelRowsName,
                renderContext));
    }

    private static InvocationExpressionSyntax CreateProjectRowsParallelInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName,
        ExecutionRenderContext? renderContext = null)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TableRowsParallel,
            CreateSourceTypeSyntax(projectionLoop.Source),
            SyntaxFactory.ParseTypeName(resultInfo.RowTypeName),
            parallelRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop, renderContext),
            CreateTableProjectionLambda(executionRenderer, projectionLoop, renderContext),
            projectionLoop.MaxDegreeOfParallelism));
    }

    private static InvocationExpressionSyntax CreateProjectRowsSerialInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext? renderContext = null)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TableRowsSerial,
            CreateSourceTypeSyntax(projectionLoop.Source),
            SyntaxFactory.ParseTypeName(resultInfo.RowTypeName),
            sourceRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop, renderContext),
            CreateTableProjectionLambda(executionRenderer, projectionLoop, renderContext)));
    }

    private static ParenthesizedLambdaExpressionSyntax CreateTableProjectionLambda(
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        ExecutionRenderContext? renderContext = null)
    {
        return CreateSourceLambda(
            projectionLoop.Source,
            renderContext == null
                ? executionRenderer.RenderFinalSinkGeneratedRowCreation(projectionLoop.AppendRow)
                : executionRenderer.RenderFinalSinkGeneratedRowCreation(projectionLoop.AppendRow, renderContext));
    }
}
