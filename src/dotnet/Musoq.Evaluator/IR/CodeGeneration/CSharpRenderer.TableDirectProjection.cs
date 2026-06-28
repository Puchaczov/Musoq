using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Visitors.Helpers;
using ExecutionCSharpRenderer = Musoq.Evaluator.IR.Execution.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.IR.CodeGeneration;

public sealed partial class CSharpRenderer
{
    private static bool TryCreateTableDirectProjectionMethod(
        ExecutionPlan plan,
        ExecutionCSharpRenderer executionRenderer,
        string queryIdentifier,
        string rowsMethodName,
        TableViaRowsResultInfo resultInfo,
        FinalProjectionSinkPlan sinkPlan,
        out MethodDeclarationSyntax rowsMethod,
        out QueryMethodRenderMetadata metadata)
    {
        if (!TryCreateFinalSinkMethod(
            plan,
            executionRenderer,
            sinkPlan,
            ExecutionCSharpRenderer.CreateOpeningPhaseStatements(plan.Body, queryIdentifier),
            setup => CreateTableDirectProjectionMethod(
                rowsMethodName,
                resultInfo,
                executionRenderer,
                setup.ProjectionLoop,
                setup.SourceSetupStatements,
                ExecutionCSharpRenderer.CreateClosingPhaseStatements(plan.Body, queryIdentifier).ToArray()),
            out rowsMethod,
            out metadata))
        {
            return false;
        }

        return true;
    }

    private static MethodDeclarationSyntax CreateTableDirectProjectionMethod(
        string rowsMethodName,
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        IReadOnlyList<StatementSyntax> sourceSetupStatements,
        IReadOnlyList<StatementSyntax> closingPhaseStatements)
    {
        const string sourceRowsName = "__musoqTableSourceRows";
        var statements = new List<StatementSyntax>(sourceSetupStatements)
        {
            CreateSourceRowsLocalDeclaration(executionRenderer, projectionLoop, sourceRowsName)
        };

        if (projectionLoop.CanUseParallel)
        {
            if (CanUseChunkedParallelProjection(projectionLoop))
                statements.AddRange(CreateTableRowsChunkedParallelReturnStatements(resultInfo, executionRenderer, projectionLoop, sourceRowsName, closingPhaseStatements));

            statements.AddRange(CreateTableRowsParallelReturnStatements(resultInfo, executionRenderer, projectionLoop, sourceRowsName, closingPhaseStatements));
        }
        else
        {
            statements.Add(CreateTableRowsSerialReturnStatement(resultInfo, executionRenderer, projectionLoop, sourceRowsName, closingPhaseStatements));
        }

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName($"IEnumerable<{resultInfo.RowTypeName}>"),
                SyntaxFactory.Identifier(rowsMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(MethodDeclarationHelper.CreateStandardParameterList())
            .WithBody(SyntaxFactory.Block(statements));
    }

    private static bool TryCreateTableShapeStreamingMethod(
        ExecutionPlan plan,
        ExecutionCSharpRenderer executionRenderer,
        string queryIdentifier,
        string shapeRowsMethodName,
        string rowsMethodName,
        TableViaRowsResultInfo resultInfo,
        bool includeProfileRecorderParameter,
        out MethodDeclarationSyntax shapeRowsMethod,
        out MethodDeclarationSyntax rowsAdapterMethod,
        out QueryMethodRenderMetadata metadata)
    {
        if (!CanStreamFinalShapeRows(plan, resultInfo, out metadata))
        {
            shapeRowsMethod = null!;
            rowsAdapterMethod = null!;
            return false;
        }

        var bufferFinalShapes = RequiresBufferedFinalShapeRows(plan, resultInfo);
        shapeRowsMethod = executionRenderer.RenderFinalShapeRowsMethod(
            plan,
            shapeRowsMethodName,
            queryIdentifier,
            resultInfo.TableName,
            resultInfo.ShapeTypeName,
            resultInfo.ShapeFields,
            includeProfileRecorderParameter: includeProfileRecorderParameter,
            bufferFinalShapes: bufferFinalShapes);
        rowsAdapterMethod = CreateTableRowsAdapterMethod(
            rowsMethodName,
            shapeRowsMethodName,
            resultInfo,
            includeProfileRecorderParameter,
            wrapProfiledShapeRows: includeProfileRecorderParameter &&
                                   !bufferFinalShapes &&
                                   executionRenderer.IsFullProfilingEnabledForGeneratedCode);
        return true;
    }

    private static bool TryCreateTypedShapeStreamingMethod(
        ExecutionPlan plan,
        ExecutionCSharpRenderer executionRenderer,
        string queryIdentifier,
        string shapeRowsMethodName,
        string rowsMethodName,
        TypedOutputBinding binding,
        TableViaRowsResultInfo resultInfo,
        out MethodDeclarationSyntax shapeRowsMethod,
        out MethodDeclarationSyntax typedRowsMethod,
        out QueryMethodRenderMetadata metadata)
    {
        if (!CanStreamFinalShapeRows(plan, resultInfo, out metadata))
        {
            shapeRowsMethod = null!;
            typedRowsMethod = null!;
            return false;
        }

        shapeRowsMethod = executionRenderer.RenderFinalShapeRowsMethod(
            plan,
            shapeRowsMethodName,
            queryIdentifier,
            resultInfo.TableName,
            resultInfo.ShapeTypeName,
            resultInfo.ShapeFields,
            useQueryRunContext: true,
            bufferFinalShapes: RequiresBufferedFinalShapeRows(plan, resultInfo));
        typedRowsMethod = CreateTypedRowsAdapterMethod(rowsMethodName, shapeRowsMethodName, binding, resultInfo);
        metadata = new QueryMethodRenderMetadata(
            FinalResultSinkKind.TypedSerialEnumerable,
            QueryResultRowPathKind.DirectRows,
            false);
        return true;
    }

    private static bool CanStreamFinalShapeRows(
        ExecutionPlan plan,
        TableViaRowsResultInfo resultInfo,
        out QueryMethodRenderMetadata metadata)
    {
        metadata = new QueryMethodRenderMetadata(
            FinalResultSinkKind.TableRowsMaterialized,
            QueryResultRowPathKind.DirectRows,
            false);

        var returnTable = plan.Body.Nodes.OfType<ExecutionReturnTable>().LastOrDefault();
        if (returnTable == null || returnTable.Table.Name != resultInfo.TableName)
            return false;

        return CanStreamSerialFinalAppendRows(plan, resultInfo) ||
               HasSupportedFinalShapeWriter(plan.Body, resultInfo.TableName, resultInfo.Columns.Count);
    }

    private static bool CanStreamSerialFinalAppendRows(
        ExecutionPlan plan,
        TableViaRowsResultInfo resultInfo)
    {
        if (!plan.Body.Nodes.OfType<ExecutionCreateTable>().Any(create => create.Table.Name == resultInfo.TableName))
            return false;

        var sourceLoops = plan.Body.Nodes.OfType<ExecutionSourceLoop>().ToArray();
        if (sourceLoops.Length != 1)
            return false;

        var loop = sourceLoops[0];
        if (plan.Body.Nodes.Any(node => !IsAllowedSerialShapeStreamNode(node, resultInfo.TableName, loop)))
            return false;

        if (ExecutionIrAnalysis.CollectNodes<ExecutionParallelFilterProjectLoop>(plan.Body).Any() ||
            ExecutionIrAnalysis.CollectNodes<ExecutionParallelBlock>(plan.Body).Any() ||
            ExecutionIrAnalysis.CollectNodes<ExecutionAppendExistingRow>(plan.Body)
                .Any(append => append.Table.Name == resultInfo.TableName))
        {
            return false;
        }

        if (ExecutionIrAnalysis.CollectNodes<ExecutionEnumerableSource>(loop.Body).Any() ||
            ExecutionIrAnalysis.CollectNodes<ExecutionGetOrAddSingleKeyAggregateGroup>(loop.Body).Any() ||
            ExecutionIrAnalysis.CollectNodes<ExecutionGetOrAddValueTupleAggregateGroup>(loop.Body).Any())
        {
            return false;
        }

        var appendRows = ExecutionIrAnalysis.CollectNodes<ExecutionAppendRow>(loop)
            .Where(append => append.Table.Name == resultInfo.TableName)
            .ToArray();

        return appendRows.Length == 1 &&
               appendRows[0].Values.Count == resultInfo.Columns.Count;
    }

    private static bool IsAllowedSerialShapeStreamNode(
        ExecutionNode node,
        string finalTableName,
        ExecutionSourceLoop loop)
    {
        return node switch
        {
            ExecutionSourceScan => true,
            ExecutionInterpretSource => true,
            ExecutionEnumerableSource => true,
            ExecutionCreateObject => true,
            ExecutionCreateValuesRows => true,
            ExecutionCreateTable createTable => createTable.Table.Name == finalTableName,
            ExecutionEnsureTableCapacity ensureCapacity => ensureCapacity.Table.Name == finalTableName,
            ExecutionReturnTable returnTable => returnTable.Table.Name == finalTableName,
            ExecutionLet => true,
            _ when ReferenceEquals(node, loop) => true,
            _ => false
        };
    }

    private static bool HasSupportedFinalShapeWriter(
        ExecutionBlock block,
        string finalTableName,
        int columnCount)
    {
        if (ExecutionIrAnalysis.CollectNodes<ExecutionAppendRow>(block)
            .Any(append => append.Table.Name == finalTableName && append.Values.Count == columnCount))
        {
            return true;
        }

        if (ExecutionIrAnalysis.CollectNodes<ExecutionAppendExistingRow>(block)
            .Any(append => append.Table.Name == finalTableName))
        {
            return true;
        }

        if (block.Nodes.OfType<ExecutionMaterializeRecordListToTable>()
            .Any(materialize => materialize.Target.Name == finalTableName && materialize.FieldIndexes.Count == columnCount))
        {
            return true;
        }

        if (block.Nodes.OfType<ExecutionSetOperation>().Any(setOperation => setOperation.Target.Name == finalTableName))
            return true;

        return block.Nodes.Any(node => IsSupportedFinalShapePostOperation(node, finalTableName));
    }

    private static bool RequiresBufferedFinalShapeRows(
        ExecutionPlan plan,
        TableViaRowsResultInfo resultInfo)
    {
        return !CanStreamSerialFinalAppendRows(plan, resultInfo);
    }

    private static bool IsSupportedFinalShapePostOperation(ExecutionNode node, string finalTableName)
    {
        return node switch
        {
            ExecutionDistinctTable distinct => distinct.Target.Name == finalTableName,
            ExecutionSortTable sort => sort.Target.Name == finalTableName,
            ExecutionTopNTable topN => topN.Target.Name == finalTableName,
            ExecutionTopOffsetTable topOffset => topOffset.Target.Name == finalTableName,
            ExecutionSkipTable skip => skip.Target.Name == finalTableName,
            ExecutionTakeTable take => take.Target.Name == finalTableName,
            ExecutionSliceTable slice => slice.Target.Name == finalTableName,
            _ => false
        };
    }

    private static IEnumerable<StatementSyntax> CreateTableRowsChunkedParallelReturnStatements(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        IReadOnlyList<StatementSyntax> closingPhaseStatements)
    {
        yield return SyntaxFactory.IfStatement(
            CreateStreamingChunkedRowsCondition(projectionLoop, sourceRowsName),
            SyntaxFactory.Block(SyntaxFactory.ReturnStatement(
                CreateLifecycleTableRowsExpression(
                    resultInfo.RowTypeName,
                    CreateTableRowsChunkedParallelExpression(resultInfo, executionRenderer, projectionLoop, sourceRowsName),
                    closingPhaseStatements))));
    }

    private static IEnumerable<StatementSyntax> CreateTableRowsParallelReturnStatements(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        IReadOnlyList<StatementSyntax> closingPhaseStatements)
    {
        const string parallelRowsName = "__musoqTableParallelRows";
        yield return CreateParallelRowsProbeDeclaration(projectionLoop, sourceRowsName, parallelRowsName);
        yield return SyntaxFactory.ReturnStatement(
            CreateLifecycleTableRowsExpression(
                resultInfo.RowTypeName,
                CreateTableRowsParallelExpression(resultInfo, executionRenderer, projectionLoop, parallelRowsName),
                closingPhaseStatements));
    }

    private static ReturnStatementSyntax CreateTableRowsSerialReturnStatement(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        IReadOnlyList<StatementSyntax> closingPhaseStatements)
    {
        return SyntaxFactory.ReturnStatement(
            CreateLifecycleTableRowsExpression(
                resultInfo.RowTypeName,
                CreateTableRowsSerialExpression(resultInfo, executionRenderer, projectionLoop, sourceRowsName),
                closingPhaseStatements));
    }

    private static ExpressionSyntax CreateTableRowsParallelExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName)
    {
        return projectionLoop.OptionalProjectorLoop == null
            ? CreateRowShardedReturnExpression(resultInfo, executionRenderer, projectionLoop, parallelRowsName)
            : CreateOptionalRowShardedReturnExpression(resultInfo, executionRenderer, projectionLoop, parallelRowsName);
    }

    private static ExpressionSyntax CreateTableRowsChunkedParallelExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName)
    {
        return projectionLoop.OptionalProjectorLoop == null
            ? CreateProjectRowsChunkedParallelInvocation(resultInfo, executionRenderer, projectionLoop, sourceRowsName)
            : CreateProjectOptionalRowsChunkedParallelInvocation(resultInfo, executionRenderer, projectionLoop, sourceRowsName);
    }

    private static ExpressionSyntax CreateTableRowsSerialExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName)
    {
        return projectionLoop.OptionalProjectorLoop == null
            ? CreateProjectRowsSerialInvocation(resultInfo, executionRenderer, projectionLoop, sourceRowsName)
            : CreateProjectOptionalRowsSerialInvocation(resultInfo, executionRenderer, projectionLoop, sourceRowsName);
    }

    private static ObjectCreationExpressionSyntax CreateLifecycleTableRowsExpression(
        string rowTypeName,
        ExpressionSyntax rowsExpression,
        IReadOnlyList<StatementSyntax> closingPhaseStatements)
    {
        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.GenericName("QueryTableEnumerable")
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.ParseTypeName(rowTypeName)))))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.ParenthesizedLambdaExpression(rowsExpression)
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("_")))))),
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")),
                SyntaxFactory.Argument(CreateClosingAction(closingPhaseStatements))
                    .WithNameColon(SyntaxFactory.NameColon("onCompleted")),
                SyntaxFactory.Argument(CreateClosingAction(closingPhaseStatements))
                    .WithNameColon(SyntaxFactory.NameColon("onDisposed"))
            ])));
    }

    private static InvocationExpressionSyntax CreateShapeShardedReturnExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName)
    {
        return CreateQueryRowsShardInvocation(
            nameof(QueryRows.FromShards),
            CreateProjectShapeRowsParallelInvocation(
                resultInfo,
                executionRenderer,
                projectionLoop,
                parallelRowsName));
    }

    private static InvocationExpressionSyntax CreateProjectShapeRowsParallelInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TypedValuesParallel,
            CreateSourceTypeSyntax(projectionLoop.Source),
            SyntaxFactory.ParseTypeName(resultInfo.ShapeTypeName),
            parallelRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop),
            CreateShapeProjectionLambda(resultInfo, executionRenderer, projectionLoop),
            projectionLoop.MaxDegreeOfParallelism));
    }

    private static InvocationExpressionSyntax CreateProjectRowsChunkedParallelInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TableChunkedRowsParallel,
            CreateSourceTypeSyntax(projectionLoop.Source),
            SyntaxFactory.ParseTypeName(resultInfo.RowTypeName),
            sourceRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop),
            CreateTableProjectionLambda(executionRenderer, projectionLoop),
            projectionLoop.MaxDegreeOfParallelism));
    }

    private static InvocationExpressionSyntax CreateProjectShapeRowsSerialInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TypedValuesSerial,
            CreateSourceTypeSyntax(projectionLoop.Source),
            SyntaxFactory.ParseTypeName(resultInfo.ShapeTypeName),
            sourceRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop),
            CreateShapeProjectionLambda(resultInfo, executionRenderer, projectionLoop)));
    }

    private static InvocationExpressionSyntax CreateOptionalRowShardedReturnExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName)
    {
        return CreateQueryRowsShardInvocation(
            nameof(QueryRows.FromRowShards),
            CreateProjectOptionalRowsParallelInvocation(
                resultInfo,
                executionRenderer,
                projectionLoop,
                parallelRowsName));
    }

    private static InvocationExpressionSyntax CreateProjectOptionalRowsParallelInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName)
    {
        var optionalProjectorLoop = projectionLoop.OptionalProjectorLoop ??
            throw new InvalidOperationException("Optional row projection requires a parallel projector loop.");

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.GenericName(nameof(EvaluationHelper.ProjectRowsParallel))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                        [
                            CreateSourceTypeSyntax(projectionLoop.Source),
                            SyntaxFactory.ParseTypeName(resultInfo.RowTypeName)
                        ])))))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.IdentifierName(parallelRowsName),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(projectionLoop.MaxDegreeOfParallelism)),
                executionRenderer.RenderOptionalGeneratedRowProjectionForTypedSink(optionalProjectorLoop),
                SyntaxFactory.IdentifierName("token")));
    }

    private static InvocationExpressionSyntax CreateProjectOptionalRowsChunkedParallelInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName)
    {
        var optionalProjectorLoop = projectionLoop.OptionalProjectorLoop ??
            throw new InvalidOperationException("Optional row projection requires a parallel projector loop.");

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.GenericName(nameof(EvaluationHelper.ProjectChunkedRowsParallel))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                        [
                            CreateSourceTypeSyntax(projectionLoop.Source),
                            SyntaxFactory.ParseTypeName(resultInfo.RowTypeName)
                        ])))))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.IdentifierName(sourceRowsName),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(projectionLoop.MaxDegreeOfParallelism)),
                executionRenderer.RenderOptionalGeneratedRowProjectionForTypedSink(optionalProjectorLoop),
                SyntaxFactory.IdentifierName("token")));
    }

    private static InvocationExpressionSyntax CreateProjectOptionalRowsSerialInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName)
    {
        var optionalProjectorLoop = projectionLoop.OptionalProjectorLoop ??
            throw new InvalidOperationException("Optional row projection requires a parallel projector loop.");

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(TableProjectionRows)),
                    SyntaxFactory.GenericName(nameof(TableProjectionRows.ProjectOptionalRowsSerial))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                        [
                            CreateSourceTypeSyntax(projectionLoop.Source),
                            SyntaxFactory.ParseTypeName(resultInfo.RowTypeName)
                        ])))))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.IdentifierName(sourceRowsName),
                executionRenderer.RenderOptionalGeneratedRowProjectionForTypedSink(optionalProjectorLoop),
                SyntaxFactory.IdentifierName("token")));
    }

    private static ParenthesizedLambdaExpressionSyntax CreateShapeProjectionLambda(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop)
    {
        var values = projectionLoop.AppendRow.Values
            .Select(value => executionRenderer.RenderExpressionForTypedSink(value.Value))
            .Select(SyntaxFactory.Argument)
            .ToArray();
        var creation = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(resultInfo.ShapeTypeName))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(values)));

        return CreateSourceLambda(projectionLoop.Source, creation);
    }

    private static MethodDeclarationSyntax CreateTableRowsAdapterMethod(
        string rowsMethodName,
        string shapeRowsMethodName,
        TableViaRowsResultInfo resultInfo,
        bool includeProfileRecorderParameter = false,
        bool wrapProfiledShapeRows = false)
    {
        const string shapeRowName = "__musoqShapeRow";
        var shapeRowsCall = includeProfileRecorderParameter
            ? $"{shapeRowsMethodName}(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token, profileRecorder)"
            : $"{shapeRowsMethodName}(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token)";
        ExpressionSyntax shapeRowsExpression = SyntaxFactory.ParseExpression(shapeRowsCall);
        if (wrapProfiledShapeRows)
        {
            shapeRowsExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.GenericName(nameof(ProfiledOperatorEnumerable<object>))
                            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.ParseTypeName(resultInfo.ShapeTypeName)))),
                        SyntaxFactory.IdentifierName(nameof(ProfiledOperatorEnumerable<object>.Create))))
                .WithArgumentList(CreateArgumentList(
                    shapeRowsExpression,
                    SyntaxFactory.IdentifierName("profileRecorder"),
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.CoalesceExpression,
                        SyntaxFactory.ConditionalAccessExpression(
                            SyntaxFactory.IdentifierName("profileRecorder"),
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberBindingExpression(
                                    SyntaxFactory.IdentifierName(nameof(QueryProfileRecorder.GetCurrentOperatorScopeDepth))))),
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(0)))));
        }

        var rowArguments = resultInfo.ShapeFields
            .Select(field => SyntaxFactory.Argument(SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(shapeRowName),
                SyntaxFactory.IdentifierName(EscapeIdentifier(GeneratedRowNamingPolicy.GetGeneratedFieldName(field))))))
            .ToArray();
        var yieldRow = SyntaxFactory.YieldStatement(
            SyntaxKind.YieldReturnStatement,
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(resultInfo.RowTypeName))
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(rowArguments))));
        var body = SyntaxFactory.Block(SyntaxFactory.ForEachStatement(
            SyntaxFactory.IdentifierName("var"),
            SyntaxFactory.Identifier(shapeRowName),
            shapeRowsExpression,
            SyntaxFactory.Block(yieldRow)));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName($"IEnumerable<{resultInfo.RowTypeName}>"),
                SyntaxFactory.Identifier(rowsMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(CreateTableRowsAdapterParameterList(includeProfileRecorderParameter))
            .WithBody(body);
    }

    private static ParameterListSyntax CreateTableRowsAdapterParameterList(bool includeProfileRecorderParameter)
    {
        var parameterList = MethodDeclarationHelper.CreateStandardParameterList();
        return includeProfileRecorderParameter
            ? parameterList.AddParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("profileRecorder"))
                .WithType(SyntaxFactory.IdentifierName("QueryProfileRecorder")))
            : parameterList;
    }

    private static MethodDeclarationSyntax CreateTypedRowsAdapterMethod(
        string rowsMethodName,
        string shapeRowsMethodName,
        TypedOutputBinding binding,
        TableViaRowsResultInfo resultInfo)
    {
        const string shapeRowName = "__musoqShapeRow";
        var shapeRowsCall = $"{shapeRowsMethodName}(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, queryContext)";
        var values = resultInfo.ShapeFields
            .Select(field => (ExpressionSyntax)SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(shapeRowName),
                SyntaxFactory.IdentifierName(EscapeIdentifier(GeneratedRowNamingPolicy.GetGeneratedFieldName(field)))))
            .ToArray();
        var yieldRow = SyntaxFactory.YieldStatement(
            SyntaxKind.YieldReturnStatement,
            binding.CreateOutputExpression(values));
        var body = SyntaxFactory.Block(SyntaxFactory.ForEachStatement(
            SyntaxFactory.IdentifierName("var"),
            SyntaxFactory.Identifier(shapeRowName),
            SyntaxFactory.ParseExpression(shapeRowsCall),
            SyntaxFactory.Block(yieldRow)));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.GenericName(nameof(IEnumerable<object>))
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            ExecutionSyntaxFactory.CreateTypeSyntax(binding.OutputType)))),
                SyntaxFactory.Identifier(rowsMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(MethodDeclarationHelper.CreateTypedRunContextParameterList())
            .WithBody(body);
    }

    private static ObjectCreationExpressionSyntax CreateLifecycleRowsExpression(
        string rowTypeName,
        ExpressionSyntax rowsExpression,
        IReadOnlyList<StatementSyntax> closingPhaseStatements)
    {
        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.GenericName("QueryEnumerable")
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.ParseTypeName(rowTypeName)))))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.ParenthesizedLambdaExpression(rowsExpression)
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("_")))))),
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")),
                SyntaxFactory.Argument(CreateClosingAction(closingPhaseStatements))
                    .WithNameColon(SyntaxFactory.NameColon("onCompleted")),
                SyntaxFactory.Argument(CreateClosingAction(closingPhaseStatements))
                    .WithNameColon(SyntaxFactory.NameColon("onDisposed"))
            ])));
    }

    private static ParenthesizedLambdaExpressionSyntax CreateClosingAction(
        IReadOnlyList<StatementSyntax> closingPhaseStatements)
    {
        return SyntaxFactory.ParenthesizedLambdaExpression()
            .WithParameterList(SyntaxFactory.ParameterList())
            .WithBlock(SyntaxFactory.Block(closingPhaseStatements));
    }
}
