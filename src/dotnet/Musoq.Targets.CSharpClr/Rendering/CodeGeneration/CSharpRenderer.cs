using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution;
using static Musoq.Targets.CSharpClr.ExecutionSyntaxFactory;
using Musoq.Evaluator.Visitors.Helpers;
using ExecutionCSharpRenderer = Musoq.Targets.CSharpClr.ExecutionCSharpRenderer;

namespace Musoq.Targets.CSharpClr;

public sealed partial class CSharpRenderer(RenderContext context)
{
    private readonly RenderContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public CompilationUnitSyntax RenderCompilationUnit(
        string queryIdentifier,
        int inMemoryTableCount = 0,
        int cteIndexResultCount = 0,
        string? runMethodNameOverride = null)
    {
        var classRenderer = new CompiledQueryClassRenderer(_context);
        return classRenderer.Render(queryIdentifier, inMemoryTableCount, cteIndexResultCount, runMethodNameOverride);
    }

    public ExecutionQueryRenderOutcome TryRenderExecutionQueryMethod(
        ExecutionPlan plan,
        string queryIdentifier)
    {
        var executionRenderer = new ExecutionCSharpRenderer(
            _context.ScriptParameterDefinitions,
            _context.ScriptVariableDefinitions,
            _context.InstrumentationMode);
        var unsupportedReason = executionRenderer.GetUnsupportedReason(plan);
        if (unsupportedReason != null)
            return ExecutionQueryRenderOutcome.Unsupported(unsupportedReason);

        var hasTableViaRowsRenderPlan = TryGetTableViaRowsRenderPlan(plan, out var precomputedRenderPlan);
        AddExecutionClassMembers(
            executionRenderer,
            plan,
            hasTableViaRowsRenderPlan &&
            _context.ResultMode is QueryResultMode.Table or QueryResultMode.TableViaRows or QueryResultMode.TypedEnumerable
                ? precomputedRenderPlan.ResultInfo
                : null);
        if (hasTableViaRowsRenderPlan &&
            executionRenderer.TryGetTableColumnMetadataFieldName(
                plan,
                precomputedRenderPlan.ResultInfo.TableName,
                precomputedRenderPlan.ResultInfo.Columns,
                out var columnsFieldName))
        {
            precomputedRenderPlan = precomputedRenderPlan with
            {
                ResultInfo = precomputedRenderPlan.ResultInfo with { ColumnsFieldName = columnsFieldName }
            };
        }

        if (_context.ResultMode == QueryResultMode.TableViaRows)
            return RenderTableViaRowsExecutionQueryMethod(plan, executionRenderer, queryIdentifier, precomputedRenderPlan);

        if (_context.ResultMode == QueryResultMode.TypedEnumerable)
            return RenderTypedEnumerableExecutionQueryMethod(plan, executionRenderer, queryIdentifier);

        if (_context.ResultMode == QueryResultMode.Table &&
            hasTableViaRowsRenderPlan)
        {
            var tableRenderPlan = precomputedRenderPlan;
            var tableMethodName = QueryMethodNameResolver.ResolveTable(_context, queryIdentifier);
            var rowsMethodName = QueryMethodNameResolver.ResolveRows(_context, queryIdentifier);
            var shapeRowsMethodName = QueryMethodNameResolver.ResolveShapeRows(_context, queryIdentifier);
            var tableResultInfo = tableRenderPlan.ResultInfo;
            var tableDirectRowsMetadata = CreateTableDirectMetadata();
            _context.SetTableViaRowsResult(tableResultInfo);

            if (_context.InstrumentationMode == QueryInstrumentationMode.Disabled &&
                TryCreateTableDirectProjectionMethod(
                    plan,
                    executionRenderer,
                    queryIdentifier,
                    rowsMethodName,
                    tableResultInfo,
                    tableRenderPlan.FinalSinkPlans.TableDirectProjection,
                    out var tableDirectRowsMethod,
                    out tableDirectRowsMetadata))
            {
                return ExecutionQueryRenderOutcome.Rendered(
                    new QueryMethodRenderResult(rowsMethodName, tableDirectRowsMethod, tableDirectRowsMetadata));
            }

            if (TryCreateTableShapeStreamingMethod(
                    plan,
                    executionRenderer,
                    queryIdentifier,
                    shapeRowsMethodName,
                    rowsMethodName,
                    tableResultInfo,
                    _context.InstrumentationMode != QueryInstrumentationMode.Disabled,
                    out var tableShapeStreamingRowsMethod,
                    out var tableShapeStreamingRowsAdapterMethod,
                    out var tableShapeStreamingRowsMetadata))
            {
                _context.AddClassMember(tableShapeStreamingRowsMethod);
                return ExecutionQueryRenderOutcome.Rendered(
                    new QueryMethodRenderResult(rowsMethodName, tableShapeStreamingRowsAdapterMethod, tableShapeStreamingRowsMetadata));
            }

            if (_context.InstrumentationMode != QueryInstrumentationMode.Disabled)
                return ExecutionQueryRenderOutcome.Unsupported("Profiled table output requires final shape-row streaming.");

            _context.AddClassMember(executionRenderer.RenderMethod(plan, tableMethodName, queryIdentifier));
            return ExecutionQueryRenderOutcome.Rendered(
                new QueryMethodRenderResult(
                    rowsMethodName,
                    CreateTableBackedRowsFromTableMethod(rowsMethodName, tableMethodName, tableResultInfo.RowTypeName),
                    tableDirectRowsMetadata));
        }

        var methodName = QueryMethodNameResolver.Resolve(_context, queryIdentifier);
        return ExecutionQueryRenderOutcome.Rendered(
            new QueryMethodRenderResult(
                methodName,
                executionRenderer.RenderMethod(plan, methodName, queryIdentifier),
                CreateTableDirectMetadata()));
    }

    private ExecutionQueryRenderOutcome RenderTableViaRowsExecutionQueryMethod(
        ExecutionPlan plan,
        ExecutionCSharpRenderer executionRenderer,
        string queryIdentifier,
        TableViaRowsRenderPlan renderPlan)
    {
        if (renderPlan == null)
            return ExecutionQueryRenderOutcome.Unsupported("TableViaRows result mode requires final select-shape result metadata.");

        var tableMethodName = QueryMethodNameResolver.ResolveTable(_context, queryIdentifier);
        var rowsMethodName = QueryMethodNameResolver.ResolveRows(_context, queryIdentifier);
        var shapeRowsMethodName = QueryMethodNameResolver.ResolveShapeRows(_context, queryIdentifier);
        var resultInfo = renderPlan.ResultInfo;
        var tableDirectRowsMetadata = CreateTableDirectMetadata();
        _context.SetTableViaRowsResult(resultInfo);

        if (_context.InstrumentationMode == QueryInstrumentationMode.Disabled &&
            TryCreateTableDirectProjectionMethod(
                plan,
                executionRenderer,
                queryIdentifier,
                rowsMethodName,
                resultInfo,
                renderPlan.FinalSinkPlans.TableDirectProjection,
                out var tableDirectRowsMethod,
                out tableDirectRowsMetadata))
        {
            return ExecutionQueryRenderOutcome.Rendered(
                new QueryMethodRenderResult(rowsMethodName, tableDirectRowsMethod, tableDirectRowsMetadata));
        }

        if (TryCreateTableShapeStreamingMethod(
                plan,
                executionRenderer,
                queryIdentifier,
                shapeRowsMethodName,
                rowsMethodName,
                resultInfo,
                _context.InstrumentationMode != QueryInstrumentationMode.Disabled,
                out var tableShapeStreamingRowsMethod,
                out var tableShapeStreamingRowsAdapterMethod,
                out var tableShapeStreamingRowsMetadata))
        {
            _context.AddClassMember(tableShapeStreamingRowsMethod);
            return ExecutionQueryRenderOutcome.Rendered(
                new QueryMethodRenderResult(rowsMethodName, tableShapeStreamingRowsAdapterMethod, tableShapeStreamingRowsMetadata));
        }

        if (_context.InstrumentationMode != QueryInstrumentationMode.Disabled)
            return ExecutionQueryRenderOutcome.Unsupported("Profiled TableViaRows output requires final shape-row streaming.");

        _context.AddClassMember(executionRenderer.RenderMethod(plan, tableMethodName, queryIdentifier));
        return ExecutionQueryRenderOutcome.Rendered(
            new QueryMethodRenderResult(
                rowsMethodName,
                CreateRowsFromTableMethod(rowsMethodName, tableMethodName, resultInfo.RowTypeName),
                tableDirectRowsMetadata));
    }

    private static bool TryGetTableViaRowsRenderPlan(ExecutionPlan plan, out TableViaRowsRenderPlan renderPlan)
    {
        return TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out renderPlan);
    }

    private void AddExecutionClassMembers(
        ExecutionCSharpRenderer executionRenderer,
        ExecutionPlan plan,
        TableViaRowsResultInfo? finalShapeResultInfo)
    {
        foreach (var classMember in executionRenderer.RenderClassMembers(
                     plan,
                     finalShapeResultInfo?.TableName,
                     finalShapeResultInfo?.ShapeTypeName,
                     finalShapeResultInfo?.ShapeFields))
        {
            if (classMember is ClassDeclarationSyntax classDeclaration && ContainsClassMember(classDeclaration.Identifier.ValueText))
                continue;

            _context.AddClassMember(classMember);
        }
    }

    private static MethodDeclarationSyntax CreateRowsFromTableMethod(
        string rowsMethodName,
        string tableMethodName,
        string rowTypeName)
    {
        var tableCall = $"{tableMethodName}(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token)";
        var rowsExpression = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("__musoqRowsTable"),
            SyntaxFactory.IdentifierName("Rows"));
        var yieldRow = SyntaxFactory.YieldStatement(
            SyntaxKind.YieldReturnStatement,
            SyntaxFactory.CastExpression(
                SyntaxFactory.ParseTypeName(rowTypeName),
                SyntaxFactory.IdentifierName("__musoqRow")));
        var body = SyntaxFactory.Block(
            SyntaxFactory.ParseStatement($"var __musoqRowsTable = {tableCall};"),
            SyntaxFactory.ForEachStatement(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.Identifier("__musoqRow"),
                    rowsExpression,
                    SyntaxFactory.Block(yieldRow)));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName($"IEnumerable<{rowTypeName}>"),
                SyntaxFactory.Identifier(rowsMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(MethodDeclarationHelper.CreateStandardParameterList())
            .WithBody(body);
    }

    private static MethodDeclarationSyntax CreateTableBackedRowsFromTableMethod(
        string rowsMethodName,
        string tableMethodName,
        string rowTypeName)
    {
        var tableCall = $"{tableMethodName}(provider, sourceRuntimeSettingsBySourceContextId, sourceExecutionPlans, logger, token)";
        var body = SyntaxFactory.Block(SyntaxFactory.ReturnStatement(
            SyntaxFactory.ParseExpression($"QueryRows.FromTable<{rowTypeName}>({tableCall})")));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.ParseTypeName($"IEnumerable<{rowTypeName}>"),
                SyntaxFactory.Identifier(rowsMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(MethodDeclarationHelper.CreateStandardParameterList())
            .WithBody(body);
    }

    private ExecutionQueryRenderOutcome RenderTypedEnumerableExecutionQueryMethod(
        ExecutionPlan plan,
        ExecutionCSharpRenderer executionRenderer,
        string queryIdentifier)
    {
        if (_context.InstrumentationMode != QueryInstrumentationMode.Disabled)
            return ExecutionQueryRenderOutcome.Unsupported("Typed enumerable result mode does not support profiling instrumentation yet.");

        var outputType = _context.OutputType;
        if (outputType == null)
            return ExecutionQueryRenderOutcome.Unsupported("Typed enumerable result mode requires an output type.");

        if (!TryGetTableViaRowsRenderPlan(plan, out var renderPlan))
            return ExecutionQueryRenderOutcome.Unsupported("Typed enumerable result mode requires final select-shape result metadata.");

        var resultInfo = renderPlan.ResultInfo;
        var binding = TypedOutputBinding.Create(outputType, resultInfo.Columns);
        var rowsMethodName = QueryMethodNameResolver.ResolveRows(_context, queryIdentifier);
        var shapeRowsMethodName = QueryMethodNameResolver.ResolveShapeRows(_context, queryIdentifier);

        if (TryCreateTypedDirectProjectionMethod(
                plan,
                executionRenderer,
                rowsMethodName,
                binding,
                resultInfo,
                renderPlan.FinalSinkPlans.TypedDirectProjection,
                useQueryRunContext: true,
                out var typedDirectMethod,
                out var typedDirectMetadata))
        {
            return ExecutionQueryRenderOutcome.Rendered(
                new QueryMethodRenderResult(rowsMethodName, typedDirectMethod, typedDirectMetadata));
        }

        if (TryCreateTypedPostOperationRowsMethod(
                plan,
                executionRenderer,
                rowsMethodName,
                binding,
                resultInfo,
                renderPlan.FinalSinkPlans.TypedPostOperations,
                useQueryRunContext: true,
                out var typedPostOperationMethod,
                out var typedPostOperationMetadata))
        {
            return ExecutionQueryRenderOutcome.Rendered(
                new QueryMethodRenderResult(rowsMethodName, typedPostOperationMethod, typedPostOperationMetadata));
        }

        if (TryCreateTypedShapeStreamingMethod(
                plan,
                executionRenderer,
                queryIdentifier,
                shapeRowsMethodName,
                rowsMethodName,
                binding,
                resultInfo,
                out var typedShapeStreamingRowsMethod,
                out var typedShapeStreamingAdapterMethod,
                out var typedShapeStreamingMetadata))
        {
            _context.AddClassMember(typedShapeStreamingRowsMethod);
            return ExecutionQueryRenderOutcome.Rendered(
                new QueryMethodRenderResult(rowsMethodName, typedShapeStreamingAdapterMethod, typedShapeStreamingMetadata));
        }

        var rejectionReason = typedPostOperationMetadata.FinalSinkRejectionReason ??
                              typedDirectMetadata.FinalSinkRejectionReason ??
                              "the final query shape is not supported by the direct typed output renderer.";
        return ExecutionQueryRenderOutcome.Unsupported(
            $"Typed enumerable result mode requires direct typed output: {rejectionReason}");
    }

    private bool ContainsClassMember(string className)
    {
        return _context.ClassMembers
            .OfType<ClassDeclarationSyntax>()
            .Any(member => member.Identifier.ValueText == className);
    }
}
