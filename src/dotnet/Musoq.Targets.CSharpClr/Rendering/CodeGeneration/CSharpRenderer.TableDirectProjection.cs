using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;

namespace Musoq.Targets.CSharpClr;

public sealed partial class CSharpRenderer
{
    private static bool TryCreateTableDirectProjectionMethod(
        ExecutionPlan plan,
        ExecutionCSharpRenderer executionRenderer,
        string queryIdentifier,
        string rowsMethodName,
        TableViaRowsResultInfo resultInfo,
        FinalProjectionSinkPlan sinkPlan,
        bool useQueryRunContext,
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
                plan.Body,
                queryIdentifier,
                ExecutionCSharpRenderer.CreateClosingPhaseStatements(plan.Body, queryIdentifier).ToArray(),
                setup.RenderContext,
                setup.EntryStatementCount,
                useQueryRunContext),
            useQueryRunContext,
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
        ExecutionBlock planBody,
        string queryIdentifier,
        IReadOnlyList<StatementSyntax> closingPhaseStatements,
        ExecutionRenderContext renderContext,
        int entryStatementCount,
        bool useQueryRunContext)
    {
        const string sourceRowsName = "__musoqTableSourceRows";
        var statements = new List<StatementSyntax>(sourceSetupStatements);
        statements.InsertRange(
            Math.Clamp(entryStatementCount, 0, statements.Count),
            CreateDirectProjectionPhaseStatements(
                planBody,
                queryIdentifier,
                QueryPhase.Begin,
                QueryPhase.From,
                useInstanceHandler: !useQueryRunContext));
        statements.Add(CreateSourceRowsLocalDeclaration(executionRenderer, projectionLoop, sourceRowsName, renderContext));
        statements.AddRange(
            CreateDirectProjectionPhaseStatements(
                planBody,
                queryIdentifier,
                QueryPhase.Where,
                QueryPhase.GroupBy,
                QueryPhase.Select,
                useInstanceHandler: !useQueryRunContext));

        if (projectionLoop.CanUseParallel)
        {
            if (CanUseChunkedParallelProjection(projectionLoop))
                statements.AddRange(CreateTableRowsChunkedParallelReturnStatements(resultInfo, executionRenderer, projectionLoop, sourceRowsName, closingPhaseStatements, renderContext));

            statements.AddRange(CreateTableRowsParallelReturnStatements(resultInfo, executionRenderer, projectionLoop, sourceRowsName, closingPhaseStatements, renderContext));
        }
        else
        {
            statements.Add(CreateTableRowsSerialReturnStatement(resultInfo, executionRenderer, projectionLoop, sourceRowsName, closingPhaseStatements, renderContext));
        }

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName($"IEnumerable<{resultInfo.RowTypeName}>"),
                SyntaxFactory.Identifier(rowsMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(useQueryRunContext
                ? MethodDeclarationHelper.CreateTypedRunContextParameterList()
                : MethodDeclarationHelper.CreateStandardParameterList())
            .WithBody(SyntaxFactory.Block(statements));
    }

    private static IEnumerable<StatementSyntax> CreateDirectProjectionPhaseStatements(
        ExecutionBlock block,
        string queryIdentifier,
        QueryPhase firstPhase,
        QueryPhase? secondPhase = null,
        QueryPhase? thirdPhase = null,
        bool useInstanceHandler = true)
    {
        foreach (var boundary in block.Nodes.OfType<ExecutionPhaseBoundary>())
        {
            if (boundary.Phase != firstPhase &&
                boundary.Phase != secondPhase &&
                boundary.Phase != thirdPhase)
                continue;

            yield return QueryEmitter.GeneratePhaseChangeStatement(
                    queryIdentifier + boundary.QueryIdSuffix,
                    boundary.Phase,
                    useInstanceHandler);
        }
    }

    private static bool TryCreateTableShapeStreamingMethod(
        ExecutionPlan plan,
        ExecutionCSharpRenderer executionRenderer,
        string queryIdentifier,
        string shapeRowsMethodName,
        string rowsMethodName,
        TableViaRowsResultInfo resultInfo,
        bool useQueryRunContext,
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
        var usesGeneratedRowCarrier = ExecutionCSharpRenderer.CanUseGeneratedFinalRowSink(plan, resultInfo.TableName);
        shapeRowsMethod = executionRenderer.RenderFinalShapeRowsMethod(
            plan,
            shapeRowsMethodName,
            queryIdentifier,
            resultInfo.TableName,
            resultInfo.ShapeTypeName,
            resultInfo.ShapeFields,
            useQueryRunContext: useQueryRunContext,
            includeProfileRecorderParameter: includeProfileRecorderParameter,
            bufferFinalShapes: bufferFinalShapes);
        rowsAdapterMethod = usesGeneratedRowCarrier
            ? CreateTableRowsForwardingMethod(rowsMethodName, shapeRowsMethodName, resultInfo.RowTypeName, useQueryRunContext, includeProfileRecorderParameter)
            : CreateTableRowsAdapterMethod(
                rowsMethodName,
                shapeRowsMethodName,
                resultInfo,
                useQueryRunContext,
                includeProfileRecorderParameter,
                wrapProfiledShapeRows: includeProfileRecorderParameter &&
                                       !bufferFinalShapes &&
                                       executionRenderer.IsFullProfilingEnabledForGeneratedCode);
        return true;
    }

    private static MethodDeclarationSyntax CreateTableRowsForwardingMethod(
        string rowsMethodName,
        string shapeRowsMethodName,
        string rowTypeName,
        bool useQueryRunContext,
        bool includeProfileRecorderParameter)
    {
        var contextArgument = useQueryRunContext ? "queryContext" : "token";
        var shapeRowsCall = includeProfileRecorderParameter
            ? $"{shapeRowsMethodName}(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, {contextArgument}, profileRecorder)"
            : $"{shapeRowsMethodName}(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, {contextArgument})";

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName($"IEnumerable<{rowTypeName}>"),
                SyntaxFactory.Identifier(rowsMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(CreateTableRowsAdapterParameterList(useQueryRunContext, includeProfileRecorderParameter))
            .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.ParseExpression(shapeRowsCall))));
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
            ExecutionPhaseBoundary => true,
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
        IReadOnlyList<StatementSyntax> closingPhaseStatements,
        ExecutionRenderContext renderContext)
    {
        yield return SyntaxFactory.IfStatement(
            CreateStreamingChunkedRowsCondition(projectionLoop, sourceRowsName),
            SyntaxFactory.Block(SyntaxFactory.ReturnStatement(
                CreateLifecycleTableRowsExpression(
                    resultInfo.RowTypeName,
                    CreateTableRowsChunkedParallelExpression(resultInfo, executionRenderer, projectionLoop, sourceRowsName, renderContext),
                    closingPhaseStatements))));
    }

    private static IEnumerable<StatementSyntax> CreateTableRowsParallelReturnStatements(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        IReadOnlyList<StatementSyntax> closingPhaseStatements,
        ExecutionRenderContext renderContext)
    {
        const string parallelRowsName = "__musoqTableParallelRows";
        yield return CreateParallelRowsProbeDeclaration(projectionLoop, sourceRowsName, parallelRowsName);
        yield return SyntaxFactory.ReturnStatement(
            CreateLifecycleTableRowsExpression(
                resultInfo.RowTypeName,
                CreateTableRowsParallelExpression(resultInfo, executionRenderer, projectionLoop, parallelRowsName, renderContext),
                closingPhaseStatements));
    }

    private static ReturnStatementSyntax CreateTableRowsSerialReturnStatement(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        IReadOnlyList<StatementSyntax> closingPhaseStatements,
        ExecutionRenderContext renderContext)
    {
        return SyntaxFactory.ReturnStatement(
            CreateLifecycleTableRowsExpression(
                resultInfo.RowTypeName,
                CreateTableRowsSerialExpression(resultInfo, executionRenderer, projectionLoop, sourceRowsName, renderContext),
                closingPhaseStatements));
    }

    private static ExpressionSyntax CreateTableRowsParallelExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName,
        ExecutionRenderContext renderContext)
    {
        return projectionLoop.OptionalProjectionBody == null
            ? CreateRowShardedReturnExpression(resultInfo, executionRenderer, projectionLoop, parallelRowsName, renderContext)
            : CreateOptionalRowShardedReturnExpression(resultInfo, executionRenderer, projectionLoop, parallelRowsName, renderContext);
    }

    private static ExpressionSyntax CreateTableRowsChunkedParallelExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext renderContext)
    {
        return projectionLoop.OptionalProjectionBody == null
            ? CreateProjectRowsChunkedParallelInvocation(resultInfo, executionRenderer, projectionLoop, sourceRowsName, renderContext)
            : CreateProjectOptionalRowsChunkedParallelInvocation(resultInfo, executionRenderer, projectionLoop, sourceRowsName, renderContext);
    }

    private static ExpressionSyntax CreateTableRowsSerialExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext renderContext)
    {
        return projectionLoop.OptionalProjectionBody == null
            ? CreateProjectRowsSerialInvocation(resultInfo, executionRenderer, projectionLoop, sourceRowsName, renderContext)
            : CreateProjectOptionalRowsSerialInvocation(resultInfo, executionRenderer, projectionLoop, sourceRowsName, renderContext);
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
                SyntaxFactory.Argument(CreateExceptionClosingAction(closingPhaseStatements))
                    .WithNameColon(SyntaxFactory.NameColon("onException")),
                SyntaxFactory.Argument(CreateClosingAction(closingPhaseStatements))
                    .WithNameColon(SyntaxFactory.NameColon("onDisposed"))
            ])));
    }

    private static InvocationExpressionSyntax CreateShapeShardedReturnExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName,
        ExecutionRenderContext? renderContext = null)
    {
        return CreateQueryRowsShardInvocation(
            nameof(QueryRows.FromShards),
            CreateProjectShapeRowsParallelInvocation(
                resultInfo,
                executionRenderer,
                projectionLoop,
                parallelRowsName,
                renderContext));
    }

    private static InvocationExpressionSyntax CreateProjectShapeRowsParallelInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName,
        ExecutionRenderContext? renderContext = null)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TypedValuesParallel,
            CreateSourceTypeSyntax(projectionLoop.Source),
            SyntaxFactory.ParseTypeName(resultInfo.ShapeTypeName),
            parallelRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop, renderContext),
            CreateShapeProjectionLambda(resultInfo, executionRenderer, projectionLoop, renderContext),
            projectionLoop.MaxDegreeOfParallelism));
    }

    private static InvocationExpressionSyntax CreateProjectRowsChunkedParallelInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext? renderContext = null)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TableChunkedRowsParallel,
            CreateSourceTypeSyntax(projectionLoop.Source),
            SyntaxFactory.ParseTypeName(resultInfo.RowTypeName),
            sourceRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop, renderContext),
            CreateTableProjectionLambda(executionRenderer, projectionLoop, renderContext),
            projectionLoop.MaxDegreeOfParallelism));
    }

    private static InvocationExpressionSyntax CreateProjectShapeRowsSerialInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext? renderContext = null)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TypedValuesSerial,
            CreateSourceTypeSyntax(projectionLoop.Source),
            SyntaxFactory.ParseTypeName(resultInfo.ShapeTypeName),
            sourceRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop, renderContext),
            CreateShapeProjectionLambda(resultInfo, executionRenderer, projectionLoop, renderContext)));
    }

    private static InvocationExpressionSyntax CreateOptionalRowShardedReturnExpression(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName,
        ExecutionRenderContext renderContext)
    {
        return CreateQueryRowsShardInvocation(
            nameof(QueryRows.FromRowShards),
            CreateProjectOptionalRowsParallelInvocation(
                resultInfo,
                executionRenderer,
                projectionLoop,
                parallelRowsName,
                renderContext));
    }

    private static InvocationExpressionSyntax CreateProjectOptionalRowsParallelInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName,
        ExecutionRenderContext renderContext)
    {
        var optionalProjectionBody = projectionLoop.OptionalProjectionBody ??
            throw new InvalidOperationException("Optional row projection requires an optional projection body.");

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
                RenderFinalSinkOptionalGeneratedRowProjection(
                    executionRenderer,
                    optionalProjectionBody,
                    projectionLoop.Source,
                    renderContext),
                SyntaxFactory.IdentifierName("token")));
    }

    private static InvocationExpressionSyntax CreateProjectOptionalRowsChunkedParallelInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext renderContext)
    {
        var optionalProjectionBody = projectionLoop.OptionalProjectionBody ??
            throw new InvalidOperationException("Optional row projection requires an optional projection body.");

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
                RenderFinalSinkOptionalGeneratedRowProjection(
                    executionRenderer,
                    optionalProjectionBody,
                    projectionLoop.Source,
                    renderContext),
                SyntaxFactory.IdentifierName("token")));
    }

    private static InvocationExpressionSyntax CreateProjectOptionalRowsSerialInvocation(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext renderContext)
    {
        var optionalProjectionBody = projectionLoop.OptionalProjectionBody ??
            throw new InvalidOperationException("Optional row projection requires an optional projection body.");

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
                RenderFinalSinkOptionalGeneratedRowProjection(
                    executionRenderer,
                    optionalProjectionBody,
                    projectionLoop.Source,
                    renderContext),
                SyntaxFactory.IdentifierName("token")));
    }

    private static ParenthesizedLambdaExpressionSyntax CreateShapeProjectionLambda(
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        ExecutionRenderContext? renderContext = null)
    {
        var values = projectionLoop.AppendRow.Values
            .Select(value => RenderFinalSinkExpression(executionRenderer, value.Value, renderContext))
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
        bool useQueryRunContext,
        bool includeProfileRecorderParameter = false,
        bool wrapProfiledShapeRows = false)
    {
        const string shapeRowName = "__musoqShapeRow";
        var contextArgument = useQueryRunContext ? "queryContext" : "token";
        var shapeRowsCall = includeProfileRecorderParameter
            ? $"{shapeRowsMethodName}(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, {contextArgument}, profileRecorder)"
            : $"{shapeRowsMethodName}(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, {contextArgument})";
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
            .WithParameterList(CreateTableRowsAdapterParameterList(useQueryRunContext, includeProfileRecorderParameter))
            .WithBody(body);
    }

    private static ParameterListSyntax CreateTableRowsAdapterParameterList(
        bool useQueryRunContext,
        bool includeProfileRecorderParameter)
    {
        var parameterList = useQueryRunContext
            ? MethodDeclarationHelper.CreateTypedRunContextParameterList()
            : MethodDeclarationHelper.CreateStandardParameterList();
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
                            CreateTypeSyntax(binding.OutputType)))),
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
                SyntaxFactory.Argument(CreateExceptionClosingAction(closingPhaseStatements))
                    .WithNameColon(SyntaxFactory.NameColon("onException")),
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

    private static ParenthesizedLambdaExpressionSyntax CreateExceptionClosingAction(
        IReadOnlyList<StatementSyntax> closingPhaseStatements)
    {
        return SyntaxFactory.ParenthesizedLambdaExpression()
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("_"))
                    .WithType(SyntaxFactory.IdentifierName(nameof(Exception))))))
            .WithBlock(SyntaxFactory.Block(closingPhaseStatements));
    }
}
