using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    internal IDisposable EnterTypedSinkRendering(ExecutionPlan plan)
    {
        return new TypedSinkRenderingScope(this, plan);
    }

    internal IDisposable EnterQueryRunContextRendering()
    {
        return new QueryRunContextRenderingScope(this);
    }

    internal IReadOnlyList<StatementSyntax> CreateTypedSinkEntryStatements(ExecutionPlan plan)
    {
        var statements = new List<StatementSyntax>();
        var reflectedAccessors = CollectReflectedMemberAccessors(plan);

        if (_useQueryRunContext)
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
        return RenderExpression(expression);
    }

    internal IReadOnlyList<StatementSyntax> RenderSourceScanForTypedSink(ExecutionSourceScan sourceScan)
    {
        return RenderSourceScan(sourceScan);
    }

    internal IReadOnlyList<StatementSyntax> RenderSetupNodeForTypedSink(ExecutionNode node)
    {
        return node switch
        {
            ExecutionCreateObject createObject => [RenderCreateObject(createObject)],
            _ => throw new InvalidOperationException($"Setup node '{node.GetType().Name}' is not supported by typed sink rendering.")
        };
    }

    internal ObjectCreationExpressionSyntax RenderGeneratedRowCreationForTypedSink(ExecutionAppendRow appendRow)
    {
        appendRow = NormalizeLazyContextSegments(appendRow);
        return CreateGeneratedRowCreation(appendRow);
    }

    internal ParenthesizedLambdaExpressionSyntax RenderOptionalGeneratedRowProjectionForTypedSink(
        ExecutionParallelFilterProjectLoop parallelProject)
    {
        return CreateParallelProjectionProjector(
            parallelProject,
            appendRow => RenderGeneratedRowCreationForTypedSink(appendRow));
    }

    private sealed class TypedSinkRenderingScope : IDisposable
    {
        private readonly ExecutionCSharpRenderer _renderer;
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
            renderer.EnsureConstantInSetFields(plan);
            renderer.EnsureStaticMetadataFields(plan);
            renderer.EnsureAggregateGenerationState(plan);

            _previousIncludeCteIndexResults = renderer._includeCteIndexResults;
            _previousIncludeCteRowResults = renderer._includeCteRowResults;
            _previousIncludeTableResults = renderer._includeTableResults;
            _previousTypedStoredTableResults = renderer._typedStoredTableResults;
            _previousGeneratedRowConstructorUsagesByType = renderer._generatedRowConstructorUsagesByType;
            _previousStoredRowsCacheNames = renderer._storedRowsCacheNames;
            _previousDeclaredStoredRowsCaches = renderer._declaredStoredRowsCaches;
            _previousReflectedMemberAccessorNames = renderer._reflectedMemberAccessorNames;
            _previousTableRowShapesByVariableName = renderer._tableRowShapesByVariableName;
            _previousStoredGeneratedRowsLoopNameCounts = renderer._storedGeneratedRowsLoopNameCounts;
            _previousTypedRowBufferVariables = renderer._typedRowBufferVariables;
            _previousOperatorCatalog = renderer._operatorCatalog;
            _previousProfileRecorderInScope = renderer._profileRecorderInScope;

            var reflectedAccessors = CollectReflectedMemberAccessors(plan);
            renderer._typedStoredTableResults = CreateTypedStoredTableResults(plan);
            renderer._includeCteIndexResults = PlanUsesCteIndexResults(plan);
            renderer._includeCteRowResults = renderer._typedStoredTableResults.Count > 0;
            renderer._includeTableResults = PlanUsesTableResults(plan, renderer._typedStoredTableResults);
            renderer._generatedRowConstructorUsagesByType = CollectGeneratedRowConstructorUsages(plan.Body);
            renderer._storedRowsCacheNames = CreateStoredRowsCacheNames(plan.Body);
            renderer._declaredStoredRowsCaches = [];
            renderer._reflectedMemberAccessorNames = reflectedAccessors.ToDictionary(
                static accessor => accessor.Key,
                static accessor => accessor.VariableName,
                StringComparer.Ordinal);
            renderer._tableRowShapesByVariableName = CreateTableRowShapeMap(plan.Body);
            renderer._storedGeneratedRowsLoopNameCounts = [];
            renderer._typedRowBufferVariables = CreateTypedRowBufferVariables(plan.Body);
            renderer._operatorCatalog = ExecutionPlanOperatorCatalog.Create(plan);
            renderer._profileRecorderInScope = renderer.IsInstrumentationEnabled;
        }

        public void Dispose()
        {
            _renderer._includeCteIndexResults = _previousIncludeCteIndexResults;
            _renderer._includeCteRowResults = _previousIncludeCteRowResults;
            _renderer._includeTableResults = _previousIncludeTableResults;
            _renderer._typedStoredTableResults = _previousTypedStoredTableResults;
            _renderer._generatedRowConstructorUsagesByType = _previousGeneratedRowConstructorUsagesByType;
            _renderer._storedRowsCacheNames = _previousStoredRowsCacheNames;
            _renderer._declaredStoredRowsCaches = _previousDeclaredStoredRowsCaches;
            _renderer._reflectedMemberAccessorNames = _previousReflectedMemberAccessorNames;
            _renderer._tableRowShapesByVariableName = _previousTableRowShapesByVariableName;
            _renderer._storedGeneratedRowsLoopNameCounts = _previousStoredGeneratedRowsLoopNameCounts;
            _renderer._typedRowBufferVariables = _previousTypedRowBufferVariables;
            _renderer._operatorCatalog = _previousOperatorCatalog;
            _renderer._profileRecorderInScope = _previousProfileRecorderInScope;
        }
    }

    private sealed class QueryRunContextRenderingScope : IDisposable
    {
        private readonly ExecutionCSharpRenderer _renderer;
        private readonly bool _previousUseQueryRunContext;

        public QueryRunContextRenderingScope(ExecutionCSharpRenderer renderer)
        {
            _renderer = renderer;
            _previousUseQueryRunContext = renderer._useQueryRunContext;
            renderer._useQueryRunContext = true;
        }

        public void Dispose()
        {
            _renderer._useQueryRunContext = _previousUseQueryRunContext;
        }
    }
}
