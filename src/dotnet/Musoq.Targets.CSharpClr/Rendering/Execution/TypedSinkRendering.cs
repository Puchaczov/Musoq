using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    internal IDisposable EnterTypedSinkRendering(ExecutionPlan plan)
    {
        return EnterTypedSinkRenderContext(plan);
    }

    internal RenderContextScope EnterTypedSinkRenderContext(ExecutionPlan plan)
    {
        return EnterTypedSinkRenderContext(plan, useQueryRunContext: false);
    }

    internal RenderContextScope EnterTypedSinkRenderContext(
        ExecutionPlan plan,
        bool useQueryRunContext)
    {
        return new RenderContextScope(new TypedSinkRenderingScope(this, plan, useQueryRunContext));
    }

    internal IDisposable EnterQueryRunContextRendering()
    {
        return EnterQueryRunContextRenderContext();
    }

    internal RenderContextScope EnterQueryRunContextRenderContext()
    {
        return new RenderContextScope(new QueryRunContextRenderingScope(this));
    }

    internal ExecutionRenderArtifacts CreateTypedSinkSetupArtifacts(
        ExecutionPlan plan,
        IEnumerable<ExecutionSourceScan> sourceScans,
        IEnumerable<ExecutionNode> setupNodes,
        bool useQueryRunContext)
    {
        using var renderingScope = EnterTypedSinkRenderContext(plan, useQueryRunContext);
        var context = renderingScope.Context;
        var statements = new List<StatementSyntax>();
        statements.AddRange(CreateTypedSinkEntryStatements(plan, context));
        foreach (var sourceScan in sourceScans)
            statements.AddRange(RenderSourceScanForTypedSink(sourceScan, context));
        foreach (var setupNode in setupNodes)
            statements.AddRange(RenderSetupNodeForTypedSink(setupNode, context));

        return new ExecutionRenderArtifacts(context, statements);
    }

    internal IReadOnlyList<StatementSyntax> CreateTypedSinkEntryStatements(ExecutionPlan plan)
    {
        using var scope = EnterTypedSinkRenderContext(plan);
        return CreateTypedSinkEntryStatements(plan, scope.Context);
    }

    internal IReadOnlyList<StatementSyntax> CreateTypedSinkEntryStatements(
        ExecutionPlan plan,
        ExecutionRenderContext context)
    {
        var statements = new List<StatementSyntax>();
        var reflectedAccessors = CollectReflectedMemberAccessors(plan);

        if (context.Session.UseQueryRunContext)
            statements.AddRange(CreateQueryRunContextAliasStatements());

        statements.AddRange(CreateExecutionStateDeclarations(plan, context));
        statements.AddRange(CreateScriptParameterBindingStatements());
        statements.AddRange(CreateScriptVariableBindingStatements());
        statements.AddRange(reflectedAccessors.Select(CreateReflectedMemberAccessorDeclaration));
        statements.AddRange(CollectMethodCallCaches(plan.Body)
            .Select(cache => RenderCreateObject(new ExecutionCreateObject(cache))));

        return statements;
    }

    internal ExpressionSyntax RenderExpressionForTypedSink(ExecutionExpression expression)
    {
        return RenderExpressionForTypedSink(expression, CreateIsolatedRenderContext());
    }

    internal ExpressionSyntax RenderExpressionForTypedSink(
        ExecutionExpression expression,
        ExecutionRenderContext context)
    {
        return RenderExpression(expression, context);
    }

    internal ExpressionSyntax RenderFinalSinkExpression(ExecutionExpression expression)
    {
        return RenderFinalSinkExpression(expression, CreateIsolatedRenderContext());
    }

    internal ExpressionSyntax RenderFinalSinkExpression(
        ExecutionExpression expression,
        ExecutionRenderContext context)
    {
        return RenderExpression(expression, context);
    }

    internal IReadOnlyList<StatementSyntax> RenderSourceScanForTypedSink(ExecutionSourceScan sourceScan)
    {
        return RenderSourceScanForTypedSink(sourceScan, CreateIsolatedRenderContext());
    }

    internal IReadOnlyList<StatementSyntax> RenderSourceScanForTypedSink(
        ExecutionSourceScan sourceScan,
        ExecutionRenderContext context)
    {
        return RenderSourceScan(sourceScan, context);
    }

    internal IReadOnlyList<StatementSyntax> RenderSetupNodeForTypedSink(ExecutionNode node)
    {
        return RenderSetupNodeForTypedSink(node, CreateIsolatedRenderContext());
    }

    internal IReadOnlyList<StatementSyntax> RenderSetupNodeForTypedSink(
        ExecutionNode node,
        ExecutionRenderContext context)
    {
        return node switch
        {
            ExecutionCreateObject createObject => [RenderCreateObject(createObject)],
            _ => throw new InvalidOperationException($"Setup node '{node.GetType().Name}' is not supported by typed sink rendering.")
        };
    }

    internal ObjectCreationExpressionSyntax RenderGeneratedRowCreationForTypedSink(ExecutionAppendRow appendRow)
    {
        return RenderGeneratedRowCreationForTypedSink(appendRow, CreateIsolatedRenderContext());
    }

    internal ObjectCreationExpressionSyntax RenderGeneratedRowCreationForTypedSink(
        ExecutionAppendRow appendRow,
        ExecutionRenderContext context)
    {
        appendRow = NormalizeLazyContextSegments(appendRow);
        return CreateGeneratedRowCreation(appendRow, context);
    }

    internal ObjectCreationExpressionSyntax RenderFinalSinkGeneratedRowCreation(ExecutionAppendRow appendRow)
    {
        return RenderFinalSinkGeneratedRowCreation(appendRow, CreateIsolatedRenderContext());
    }

    internal ObjectCreationExpressionSyntax RenderFinalSinkGeneratedRowCreation(
        ExecutionAppendRow appendRow,
        ExecutionRenderContext context)
    {
        appendRow = NormalizeLazyContextSegments(appendRow);
        return CreateGeneratedRowCreation(appendRow, context);
    }

    internal ParenthesizedLambdaExpressionSyntax RenderOptionalGeneratedRowProjectionForTypedSink(
        ExecutionParallelFilterProjectLoop parallelProject)
    {
        return RenderOptionalGeneratedRowProjectionForTypedSink(
            parallelProject,
            CreateIsolatedRenderContext());
    }

    internal ParenthesizedLambdaExpressionSyntax RenderOptionalGeneratedRowProjectionForTypedSink(
        ExecutionParallelFilterProjectLoop parallelProject,
        ExecutionRenderContext context)
    {
        return CreateParallelProjectionProjector(
            parallelProject,
            appendRow => RenderGeneratedRowCreationForTypedSink(appendRow, context),
            context);
    }

    internal ParenthesizedLambdaExpressionSyntax RenderFinalSinkOptionalGeneratedRowProjection(
        ExecutionParallelFilterProjectLoop parallelProject)
    {
        return RenderFinalSinkOptionalGeneratedRowProjection(
            parallelProject,
            CreateIsolatedRenderContext());
    }

    internal ParenthesizedLambdaExpressionSyntax RenderFinalSinkOptionalGeneratedRowProjection(
        ExecutionParallelFilterProjectLoop parallelProject,
        ExecutionRenderContext context)
    {
        return CreateParallelProjectionProjector(
            parallelProject,
            appendRow => RenderFinalSinkGeneratedRowCreation(appendRow, context),
            context);
    }

    internal sealed class RenderContextScope : IDisposable
    {
        private readonly IRenderContextScope _scope;

        internal RenderContextScope(IRenderContextScope scope)
        {
            _scope = scope;
            Context = scope.Context;
        }

        internal ExecutionRenderContext Context { get; }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }

    internal interface IRenderContextScope : IDisposable
    {
        ExecutionRenderContext Context { get; }
    }

    private sealed class TypedSinkRenderingScope : IRenderContextScope
    {
        public TypedSinkRenderingScope(
            ExecutionCSharpRenderer renderer,
            ExecutionPlan plan,
            bool useQueryRunContext)
        {
            Context = renderer.InitializeRenderContext(plan, useQueryRunContext);
            var session = Context.Session;

            var reflectedAccessors = CollectReflectedMemberAccessors(plan);
            session.TypedStoredTableResults = CreateTypedStoredTableResults(plan);
            session.IncludeCteIndexResults = PlanUsesCteIndexResults(plan);
            session.IncludeCteRowResults = session.TypedStoredTableResults.Count > 0;
            session.IncludeTableResults = PlanUsesTableResults(plan, session.TypedStoredTableResults);
            session.GeneratedRowVariableTypeNamesByName = CollectGeneratedRowVariableTypeNames(plan.Body, session.TypedStoredTableResults);
            session.GeneratedRowConstructorUsagesByType = CollectGeneratedRowConstructorUsages(plan.Body, session.TypedStoredTableResults);
            session.StoredRowsCacheNames = CreateStoredRowsCacheNames(plan.Body);
            session.DeclaredStoredRowsCaches = [];
            session.ReflectedMemberAccessorNames = reflectedAccessors.ToDictionary(
                static accessor => accessor.Key,
                static accessor => accessor.VariableName,
                StringComparer.Ordinal);
            session.TableRowShapesByVariableName = CreateTableRowShapeMap(plan.Body);
            session.GeneratedRowVariableTypeNamesByName = CollectGeneratedRowVariableTypeNames(plan.Body, session.TypedStoredTableResults);
            session.StoredGeneratedRowsLoopNameCounts = [];
            session.TypedRowBufferVariables = CreateTypedRowBufferVariables(plan.Body);
            session.OperatorCatalog = ExecutionPlanOperatorCatalog.Create(plan);
            session.ProfileRecorderInScope = renderer.IsInstrumentationEnabled;
        }

        public ExecutionRenderContext Context { get; }

        public void Dispose()
        {
        }
    }

    private sealed class QueryRunContextRenderingScope : IRenderContextScope
    {
        public QueryRunContextRenderingScope(ExecutionCSharpRenderer renderer)
        {
            Context = renderer.CreateIsolatedRenderContext();
            Context.Session.UseQueryRunContext = true;
        }

        public ExecutionRenderContext Context { get; }

        public void Dispose()
        {
        }
    }
}
