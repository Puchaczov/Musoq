using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;
using Musoq.Evaluator.Visitors.Helpers;
namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    public MethodDeclarationSyntax RenderMethod(ExecutionPlan plan, string methodName) => RenderMethod(plan, methodName, methodName);

    public MethodDeclarationSyntax RenderMethod(ExecutionPlan plan, string methodName, string queryIdentifier)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryIdentifier);
        EnsureConstantInSetFields(plan);
        EnsureStaticMetadataFields(plan);
        EnsureAggregateGenerationState(plan);

        var previousIncludeCteIndexResults = _includeCteIndexResults;
        var previousIncludeCteRowResults = _includeCteRowResults;
        var previousIncludeTableResults = _includeTableResults;
        var previousTypedStoredTableResults = _typedStoredTableResults;
        var previousGeneratedRowConstructorUsagesByType = _generatedRowConstructorUsagesByType;
        var previousSingleKeyAggregateUpdateHelpersByBlock = _singleKeyAggregateUpdateHelpersByBlock;
        var previousEnumerableTraversalHelpersByBlock = _enumerableTraversalHelpersByBlock;
        var previousTypedRowBufferVariables = _typedRowBufferVariables;
        _typedStoredTableResults = CreateTypedStoredTableResults(plan);
        _includeCteIndexResults = PlanUsesCteIndexResults(plan);
        _includeCteRowResults = _typedStoredTableResults.Count > 0;
        _includeTableResults = PlanUsesTableResults(plan, _typedStoredTableResults);
        _generatedRowConstructorUsagesByType = CollectGeneratedRowConstructorUsages(plan.Body);
        _typedRowBufferVariables = CreateTypedRowBufferVariables(plan.Body);
        _singleKeyAggregateUpdateHelpersByBlock = CollectSingleKeyAggregateUpdateHelpersByBlock(plan.Body);
        _enumerableTraversalHelpersByBlock = CollectEnumerableTraversalHelpersByBlock(plan.Body);

        try
        {
            return CreateQueryMethod(
                methodName,
                RenderMethodBody(plan, queryIdentifier));
        }
        finally
        {
            _includeCteIndexResults = previousIncludeCteIndexResults;
            _includeCteRowResults = previousIncludeCteRowResults;
            _includeTableResults = previousIncludeTableResults;
            _typedStoredTableResults = previousTypedStoredTableResults;
            _generatedRowConstructorUsagesByType = previousGeneratedRowConstructorUsagesByType;
            _singleKeyAggregateUpdateHelpersByBlock = previousSingleKeyAggregateUpdateHelpersByBlock;
            _enumerableTraversalHelpersByBlock = previousEnumerableTraversalHelpersByBlock;
            _typedRowBufferVariables = previousTypedRowBufferVariables;
        }
    }

    public BlockSyntax RenderBlock(ExecutionBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        if (!_suppressedEnumerableTraversalHelperBlocks.Contains(block) &&
            _enumerableTraversalHelpersByBlock.TryGetValue(block, out var traversalHelper))
        {
            return StatementEmitter.CreateBlock(CreateEnumerableTraversalInvocation(traversalHelper));
        }
        if (!_suppressSingleKeyAggregateUpdateHelpers &&
            _singleKeyAggregateUpdateHelpersByBlock.TryGetValue(block, out var helper))
        {
            return StatementEmitter.CreateBlock(CreateSingleKeyAggregateUpdateInvocation(helper));
        }
        return StatementEmitter.CreateBlock(block.Nodes.SelectMany(RenderNode));
    }

    private BlockSyntax RenderMethodBody(ExecutionPlan plan, string queryIdentifier)
    {
        var block = plan.Body;
        var previousStoredRowsCacheNames = _storedRowsCacheNames;
        var previousDeclaredStoredRowsCaches = _declaredStoredRowsCaches;
        var previousReflectedMemberAccessorNames = _reflectedMemberAccessorNames;
        var previousTableRowShapesByVariableName = _tableRowShapesByVariableName;
        var previousStoredGeneratedRowsLoopNameCounts = _storedGeneratedRowsLoopNameCounts;
        var previousTypedRowBufferVariables = _typedRowBufferVariables;
        var previousOperatorCatalog = _operatorCatalog;
        var previousProfileRecorderInScope = _profileRecorderInScope;
        var reflectedAccessors = CollectReflectedMemberAccessors(plan);
        _storedRowsCacheNames = CreateStoredRowsCacheNames(block);
        _declaredStoredRowsCaches = [];
        _reflectedMemberAccessorNames = reflectedAccessors.ToDictionary(
            static accessor => accessor.Key,
            static accessor => accessor.VariableName,
            StringComparer.Ordinal);
        _tableRowShapesByVariableName = CreateTableRowShapeMap(block);
        _storedGeneratedRowsLoopNameCounts = [];
        _typedRowBufferVariables = CreateTypedRowBufferVariables(block);
        _operatorCatalog = ExecutionPlanOperatorCatalog.Create(plan);
        _profileRecorderInScope = IsInstrumentationEnabled;

        try
        {
            var statements = new List<StatementSyntax>();
            if (_useQueryRunContext)
                statements.AddRange(CreateQueryRunContextAliasStatements());

            statements.AddRange(CreateOpeningPhaseStatements(block, queryIdentifier));
            statements.AddRange(CreateExecutionStateDeclarations(plan));
            statements.AddRange(CreateScriptParameterBindingStatements());
            statements.AddRange(CreateScriptVariableBindingStatements());
            statements.AddRange(reflectedAccessors.Select(CreateReflectedMemberAccessorDeclaration));
            statements.AddRange(CollectMethodCallCaches(block)
                .Select(cache => RenderCreateObject(new ExecutionCreateObject(cache))));

            var bodyStatements = RenderMethodStatements(block);
            var operatorProfileUsage = CollectOperatorProfileUsage(bodyStatements);
            statements.AddRange(CreateOperatorHandleDeclarations(operatorProfileUsage).Concat(CreateOperatorCounterDeclarations(operatorProfileUsage)));

            foreach (var statement in bodyStatements)
            {
                if (statement is ReturnStatementSyntax)
                    statements.AddRange(CreateOperatorCounterFlushStatements(operatorProfileUsage).Concat(CreateClosingPhaseStatements(block, queryIdentifier)));

                statements.Add(statement);
            }

            return CreateProfileExceptionBoundaryBlock(statements);
        }
        finally
        {
            _storedRowsCacheNames = previousStoredRowsCacheNames;
            _declaredStoredRowsCaches = previousDeclaredStoredRowsCaches;
            _reflectedMemberAccessorNames = previousReflectedMemberAccessorNames;
            _tableRowShapesByVariableName = previousTableRowShapesByVariableName;
            _storedGeneratedRowsLoopNameCounts = previousStoredGeneratedRowsLoopNameCounts;
            _typedRowBufferVariables = previousTypedRowBufferVariables;
            _operatorCatalog = previousOperatorCatalog;
            _profileRecorderInScope = previousProfileRecorderInScope;
        }
    }
}
