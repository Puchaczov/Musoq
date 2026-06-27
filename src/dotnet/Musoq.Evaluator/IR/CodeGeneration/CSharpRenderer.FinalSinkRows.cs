using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Runtime;
using ExecutionCSharpRenderer = Musoq.Evaluator.IR.Execution.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.IR.CodeGeneration;

public sealed partial class CSharpRenderer
{
    private static InvocationExpressionSyntax CreateRowShardedReturnExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName)
    {
        return CreateQueryRowsShardInvocation(
            nameof(QueryRows.FromRowShards),
            CreateProjectRowsParallelInvocation(
                resultInfo,
                executionRenderer,
                projectionLoop,
                parallelRowsName));
    }

    private static InvocationExpressionSyntax CreateProjectRowsParallelInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TableRowsParallel,
            CreateSourceTypeSyntax(projectionLoop.Source),
            SyntaxFactory.ParseTypeName(resultInfo.RowTypeName),
            parallelRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop),
            CreateTableProjectionLambda(executionRenderer, projectionLoop),
            projectionLoop.MaxDegreeOfParallelism));
    }

    private static InvocationExpressionSyntax CreateProjectRowsSerialInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TableRowsSerial,
            CreateSourceTypeSyntax(projectionLoop.Source),
            SyntaxFactory.ParseTypeName(resultInfo.RowTypeName),
            sourceRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop),
            CreateTableProjectionLambda(executionRenderer, projectionLoop)));
    }

    private static ParenthesizedLambdaExpressionSyntax CreateTableProjectionLambda(
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop)
    {
        return CreateSourceLambda(
            projectionLoop.Source,
            executionRenderer.RenderGeneratedRowCreationForTypedSink(projectionLoop.AppendRow));
    }
}
