using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    internal IDisposable EnterTypedSinkRendering(ExecutionPlan plan)
    {
        return EnterTypedSinkRenderContext(plan);
    }

    internal RenderContextScope EnterTypedSinkRenderContext(ExecutionPlan plan)
    {
        return new RenderContextScope(new TypedSinkRenderingScope(this, plan));
    }

    internal IDisposable EnterQueryRunContextRendering()
    {
        return EnterQueryRunContextRenderContext();
    }

    internal RenderContextScope EnterQueryRunContextRenderContext()
    {
        return new RenderContextScope(new QueryRunContextRenderingScope(this));
    }

    internal IReadOnlyList<StatementSyntax> CreateTypedSinkEntryStatements(ExecutionPlan plan)
    {
        return CreateTypedSinkEntryStatements(plan, new ExecutionRenderContext(_renderOptions, RenderSession));
    }

    internal IReadOnlyList<StatementSyntax> CreateTypedSinkEntryStatements(
        ExecutionPlan plan,
        ExecutionRenderContext context)
    {
        var statements = new List<StatementSyntax>();
        var reflectedAccessors = CollectReflectedMemberAccessors(plan);

        if (context.Session.UseQueryRunContext)
            statements.AddRange(CreateQueryRunContextAliasStatements());

        statements.AddRange(CreateExecutionStateDeclarations(plan));
        statements.AddRange(CreateScriptParameterBindingStatements());
        statements.AddRange(CreateScriptVariableBindingStatements());
        statements.AddRange(reflectedAccessors.Select(CreateReflectedMemberAccessorDeclaration));
        statements.AddRange(CollectMethodCallCaches(plan.Body)
            .Select(cache => RenderCreateObject(new ExecutionCreateObject(cache))));

        return statements;
    }

    internal ExpressionSyntax RenderExpressionForTypedSink(ExecutionExpression expression)
    {
        return RenderExpressionForTypedSink(expression, new ExecutionRenderContext(_renderOptions, RenderSession));
    }

    internal ExpressionSyntax RenderExpressionForTypedSink(
        ExecutionExpression expression,
        ExecutionRenderContext context)
    {
        return RenderExpression(expression, context);
    }

    internal IReadOnlyList<StatementSyntax> RenderSourceScanForTypedSink(ExecutionSourceScan sourceScan)
    {
        return RenderSourceScanForTypedSink(sourceScan, new ExecutionRenderContext(_renderOptions, RenderSession));
    }

    internal IReadOnlyList<StatementSyntax> RenderSourceScanForTypedSink(
        ExecutionSourceScan sourceScan,
        ExecutionRenderContext context)
    {
        return RenderSourceScan(sourceScan);
    }

    internal IReadOnlyList<StatementSyntax> RenderSetupNodeForTypedSink(ExecutionNode node)
    {
        return RenderSetupNodeForTypedSink(node, new ExecutionRenderContext(_renderOptions, RenderSession));
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
        return RenderGeneratedRowCreationForTypedSink(appendRow, new ExecutionRenderContext(_renderOptions, RenderSession));
    }

    internal ObjectCreationExpressionSyntax RenderGeneratedRowCreationForTypedSink(
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
            new ExecutionRenderContext(_renderOptions, RenderSession));
    }

    internal ParenthesizedLambdaExpressionSyntax RenderOptionalGeneratedRowProjectionForTypedSink(
        ExecutionParallelFilterProjectLoop parallelProject,
        ExecutionRenderContext context)
    {
        return CreateParallelProjectionProjector(
            parallelProject,
            appendRow => RenderGeneratedRowCreationForTypedSink(appendRow, context));
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
        private readonly ExecutionCSharpRenderer _renderer;
        private readonly ExecutionRenderSession? _previousRenderSession;
        private readonly bool _previousIncludeCteIndexResults;
        private readonly bool _previousIncludeCteRowResults;
        private readonly bool _previousIncludeTableResults;
        private readonly IReadOnlyDictionary<int, TypedStoredTableResult> _previousTypedStoredTableResults;
        private readonly IReadOnlyDictionary<string, IReadOnlySet<GeneratedRowContextConstructor>> _previousGeneratedRowConstructorUsagesByType;
        private readonly IReadOnlyDictionary<int, string> _previousStoredRowsCacheNames;
        private readonly HashSet<int> _previousDeclaredStoredRowsCaches;
        private readonly IReadOnlyDictionary<string, string> _previousReflectedMemberAccessorNames;
        private readonly IReadOnlyDictionary<string, GeneratedRowShape> _previousTableRowShapesByVariableName;
        private readonly Dictionary<int, int> _previousStoredGeneratedRowsLoopNameCounts;
        private readonly IReadOnlyDictionary<string, GeneratedRowShape> _previousTypedRowBufferVariables;
        private readonly ExecutionPlanOperatorCatalog _previousOperatorCatalog;
        private readonly bool _previousProfileRecorderInScope;

        public TypedSinkRenderingScope(ExecutionCSharpRenderer renderer, ExecutionPlan plan)
        {
            _renderer = renderer;
            _previousRenderSession = RenderSessionSlot.Value;
            renderer.InitializeRenderSession(plan);
            Context = new ExecutionRenderContext(renderer._renderOptions, renderer.RenderSession);

            _previousIncludeCteIndexResults = renderer.RenderSession.IncludeCteIndexResults;
            _previousIncludeCteRowResults = renderer.RenderSession.IncludeCteRowResults;
            _previousIncludeTableResults = renderer.RenderSession.IncludeTableResults;
            _previousTypedStoredTableResults = renderer.RenderSession.TypedStoredTableResults;
            _previousGeneratedRowConstructorUsagesByType = renderer.RenderSession.GeneratedRowConstructorUsagesByType;
            _previousStoredRowsCacheNames = renderer.RenderSession.StoredRowsCacheNames;
            _previousDeclaredStoredRowsCaches = renderer.RenderSession.DeclaredStoredRowsCaches;
            _previousReflectedMemberAccessorNames = renderer.RenderSession.ReflectedMemberAccessorNames;
            _previousTableRowShapesByVariableName = renderer.RenderSession.TableRowShapesByVariableName;
            _previousStoredGeneratedRowsLoopNameCounts = renderer.RenderSession.StoredGeneratedRowsLoopNameCounts;
            _previousTypedRowBufferVariables = renderer.RenderSession.TypedRowBufferVariables;
            _previousOperatorCatalog = renderer.RenderSession.OperatorCatalog;
            _previousProfileRecorderInScope = renderer.RenderSession.ProfileRecorderInScope;

            var reflectedAccessors = CollectReflectedMemberAccessors(plan);
            renderer.RenderSession.TypedStoredTableResults = CreateTypedStoredTableResults(plan);
            renderer.RenderSession.IncludeCteIndexResults = PlanUsesCteIndexResults(plan);
            renderer.RenderSession.IncludeCteRowResults = renderer.RenderSession.TypedStoredTableResults.Count > 0;
            renderer.RenderSession.IncludeTableResults = PlanUsesTableResults(plan, renderer.RenderSession.TypedStoredTableResults);
            renderer.RenderSession.GeneratedRowConstructorUsagesByType = CollectGeneratedRowConstructorUsages(plan.Body);
            renderer.RenderSession.StoredRowsCacheNames = CreateStoredRowsCacheNames(plan.Body);
            renderer.RenderSession.DeclaredStoredRowsCaches = [];
            renderer.RenderSession.ReflectedMemberAccessorNames = reflectedAccessors.ToDictionary(
                static accessor => accessor.Key,
                static accessor => accessor.VariableName,
                StringComparer.Ordinal);
            renderer.RenderSession.TableRowShapesByVariableName = CreateTableRowShapeMap(plan.Body);
            renderer.RenderSession.StoredGeneratedRowsLoopNameCounts = [];
            renderer.RenderSession.TypedRowBufferVariables = CreateTypedRowBufferVariables(plan.Body);
            renderer.RenderSession.OperatorCatalog = ExecutionPlanOperatorCatalog.Create(plan);
            renderer.RenderSession.ProfileRecorderInScope = renderer.IsInstrumentationEnabled;
        }

        public ExecutionRenderContext Context { get; }

        public void Dispose()
        {
            _renderer.RenderSession.IncludeCteIndexResults = _previousIncludeCteIndexResults;
            _renderer.RenderSession.IncludeCteRowResults = _previousIncludeCteRowResults;
            _renderer.RenderSession.IncludeTableResults = _previousIncludeTableResults;
            _renderer.RenderSession.TypedStoredTableResults = _previousTypedStoredTableResults;
            _renderer.RenderSession.GeneratedRowConstructorUsagesByType = _previousGeneratedRowConstructorUsagesByType;
            _renderer.RenderSession.StoredRowsCacheNames = _previousStoredRowsCacheNames;
            _renderer.RenderSession.DeclaredStoredRowsCaches = _previousDeclaredStoredRowsCaches;
            _renderer.RenderSession.ReflectedMemberAccessorNames = _previousReflectedMemberAccessorNames;
            _renderer.RenderSession.TableRowShapesByVariableName = _previousTableRowShapesByVariableName;
            _renderer.RenderSession.StoredGeneratedRowsLoopNameCounts = _previousStoredGeneratedRowsLoopNameCounts;
            _renderer.RenderSession.TypedRowBufferVariables = _previousTypedRowBufferVariables;
            _renderer.RenderSession.OperatorCatalog = _previousOperatorCatalog;
            _renderer.RenderSession.ProfileRecorderInScope = _previousProfileRecorderInScope;
            RenderSessionSlot.Value = _previousRenderSession;
        }
    }

    private sealed class QueryRunContextRenderingScope : IRenderContextScope
    {
        private readonly ExecutionCSharpRenderer _renderer;
        private readonly bool _previousUseQueryRunContext;

        public QueryRunContextRenderingScope(ExecutionCSharpRenderer renderer)
        {
            _renderer = renderer;
            _previousUseQueryRunContext = renderer.RenderSession.UseQueryRunContext;
            renderer.RenderSession.UseQueryRunContext = true;
            Context = new ExecutionRenderContext(renderer._renderOptions, renderer.RenderSession);
        }

        public ExecutionRenderContext Context { get; }

        public void Dispose()
        {
            _renderer.RenderSession.UseQueryRunContext = _previousUseQueryRunContext;
        }
    }
}
