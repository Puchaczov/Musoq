using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    internal MethodDeclarationSyntax RenderFinalShapeRowsMethod(
        ExecutionPlan plan,
        string methodName,
        string queryIdentifier,
        string finalTableName,
        string shapeTypeName,
        IReadOnlyList<FieldBinding> shapeFields,
        bool useQueryRunContext = false,
        bool includeProfileRecorderParameter = false,
        bool bufferFinalShapes = false)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(finalTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeTypeName);
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
        var previousFinalShapeYieldSink = _finalShapeYieldSink;
        using var queryRunContextScope = useQueryRunContext
            ? EnterQueryRunContextRendering()
            : null;

        _typedStoredTableResults = CreateTypedStoredTableResults(plan);
        _includeCteIndexResults = ExecutionCSharpRenderer.PlanUsesCteIndexResults(plan);
        _includeCteRowResults = _typedStoredTableResults.Count > 0;
        _includeTableResults = ExecutionCSharpRenderer.PlanUsesTableResults(plan, _typedStoredTableResults);
        _generatedRowConstructorUsagesByType = ExecutionCSharpRenderer.CollectGeneratedRowConstructorUsages(plan.Body);
        var finalShapeBufferName = bufferFinalShapes ? "__musoqFinalShapeRows" : null;
        var finalShapeSourceBuffers = CreateFinalShapeSourceBuffers(plan.Body, finalTableName, shapeTypeName, shapeFields);
        _finalShapeYieldSink = new FinalShapeYieldSink(
            finalTableName,
            shapeTypeName,
            shapeFields,
            finalShapeBufferName,
            finalShapeSourceBuffers);
        _singleKeyAggregateUpdateHelpersByBlock = CollectSingleKeyAggregateUpdateHelpersByBlock(plan.Body);
        _enumerableTraversalHelpersByBlock = Enumerable
            .Where<KeyValuePair<ExecutionBlock, ExecutionCSharpRenderer.EnumerableTraversalHelper>>(CollectEnumerableTraversalHelpersByBlock(plan.Body), pair => !CapturesCurrentFinalShapeTargetOrSourceBuffer(pair.Value))
            .ToDictionary(static pair => pair.Key, static pair => pair.Value);

        try
        {
            return SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.ParseTypeName($"IEnumerable<{shapeTypeName}>"),
                    SyntaxFactory.Identifier(methodName))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithParameterList(CreateFinalShapeRowsParameterList(useQueryRunContext, includeProfileRecorderParameter))
                .WithBody(RenderFinalShapeRowsMethodBody(plan, queryIdentifier, finalTableName, shapeTypeName, finalShapeBufferName));
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
            _finalShapeYieldSink = previousFinalShapeYieldSink;
        }
    }

    private static ParameterListSyntax CreateFinalShapeRowsParameterList(
        bool useQueryRunContext,
        bool includeProfileRecorderParameter)
    {
        var parameterList = useQueryRunContext
            ? MethodDeclarationHelper.CreateTypedRunContextParameterList()
            : MethodDeclarationHelper.CreateStandardParameterList();

        return includeProfileRecorderParameter
            ? parameterList.AddParameters(CreateProfileRecorderParameter())
            : parameterList;
    }

    private BlockSyntax RenderFinalShapeRowsMethodBody(
        ExecutionPlan plan,
        string queryIdentifier,
        string finalTableName,
        string shapeTypeName,
        string? finalShapeBufferName)
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
        var reflectedAccessors = ExecutionCSharpRenderer.CollectReflectedMemberAccessors(plan);
        _storedRowsCacheNames = ExecutionCSharpRenderer.CreateStoredRowsCacheNames(block);
        _declaredStoredRowsCaches = [];
        _reflectedMemberAccessorNames = reflectedAccessors.ToDictionary(
            static accessor => accessor.Key,
            static accessor => accessor.VariableName,
            StringComparer.Ordinal);
        _tableRowShapesByVariableName = ExecutionCSharpRenderer.CreateTableRowShapeMap(block);
        _storedGeneratedRowsLoopNameCounts = [];
        _typedRowBufferVariables = CreateTypedRowBufferVariables(block, finalTableName);
        _operatorCatalog = ExecutionPlanOperatorCatalog.Create(plan);
        _profileRecorderInScope = IsInstrumentationEnabled;

        try
        {
            var statements = new List<StatementSyntax>();
            if (_useQueryRunContext)
                statements.AddRange(ExecutionCSharpRenderer.CreateQueryRunContextAliasStatements());

            statements.AddRange(ExecutionCSharpRenderer.CreateOpeningPhaseStatements(block, queryIdentifier));

            var tryStatements = new List<StatementSyntax>();
            tryStatements.AddRange(CreateExecutionStateDeclarations(plan));
            tryStatements.AddRange(CreateScriptParameterBindingStatements());
            tryStatements.AddRange(CreateScriptVariableBindingStatements());
            tryStatements.AddRange(reflectedAccessors.Select(ExecutionCSharpRenderer.CreateReflectedMemberAccessorDeclaration));
            tryStatements.AddRange(ExecutionCSharpRenderer.CollectMethodCallCaches(block)
                .Select(cache => ExecutionCSharpRenderer.RenderCreateObject(new ExecutionCreateObject(cache))));
            if (finalShapeBufferName != null)
            {
                tryStatements.Add(CreateLocalDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    finalShapeBufferName,
                    SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.ParseTypeName($"List<{shapeTypeName}>"))
                        .WithArgumentList(SyntaxFactory.ArgumentList())));
            }

            var bodyStatements = RenderMethodStatements(RemoveFinalTableBoundary(block, finalTableName))
                .Select(statement => RewriteRemovedFinalTableCount(statement, finalTableName, finalShapeBufferName))
                .ToList();
            if (finalShapeBufferName != null)
                bodyStatements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(finalShapeBufferName)));

            var operatorProfileUsage = CollectOperatorProfileUsage(bodyStatements);
            tryStatements.AddRange(CreateOperatorHandleDeclarations(operatorProfileUsage).Concat(CreateOperatorCounterDeclarations(operatorProfileUsage)));
            tryStatements.AddRange(AddOperatorCounterFlushesBeforeTopLevelReturns(bodyStatements, operatorProfileUsage, appendAtEnd: true));

            statements.Add(SyntaxFactory.TryStatement(SyntaxFactory.Block(tryStatements), default, SyntaxFactory.FinallyClause(
                SyntaxFactory.Block(ExecutionCSharpRenderer.CreateClosingPhaseStatements(block, queryIdentifier)))));

            return CreateProfileExceptionBoundaryBlock(statements, includeExceptionBoundary: finalShapeBufferName != null);
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

    private static StatementSyntax RewriteRemovedFinalTableCount(
        StatementSyntax statement,
        string finalTableName,
        string? finalShapeBufferName)
    {
        return finalShapeBufferName == null
            ? statement
            : (StatementSyntax)new RemovedFinalTableCountRewriter(finalTableName, finalShapeBufferName).Visit(statement)!;
    }

    private sealed class RemovedFinalTableCountRewriter(
        string finalTableName,
        string finalShapeBufferName) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Expression is IdentifierNameSyntax identifier &&
                string.Equals(identifier.Identifier.ValueText, finalTableName, StringComparison.Ordinal) &&
                string.Equals(node.Name.Identifier.ValueText, nameof(IReadOnlyCollection<object>.Count), StringComparison.Ordinal))
            {
                return node.WithExpression(SyntaxFactory.IdentifierName(finalShapeBufferName));
            }

            return base.VisitMemberAccessExpression(node);
        }
    }

    private static ExecutionBlock RemoveFinalTableBoundary(ExecutionBlock block, string finalTableName)
    {
        return new ExecutionBlock(block.Nodes
            .Where(node => !IsFinalTableBoundaryNode(node, finalTableName))
            .ToArray());
    }

    private bool CapturesCurrentFinalShapeTargetOrSourceBuffer(ExecutionCSharpRenderer.EnumerableTraversalHelper helper)
    {
        return helper.Captures.Any(capture => IsCurrentFinalShapeTargetOrSourceBuffer(capture.Name));
    }

    private static IReadOnlyDictionary<string, FinalShapeSourceBuffer> CreateFinalShapeSourceBuffers(
        ExecutionBlock block,
        string finalTableName,
        string shapeTypeName,
        IReadOnlyList<FieldBinding> shapeFields)
    {
        var rowShapesByTableName = ExecutionCSharpRenderer.CreateTableRowShapeMap(block);
        Dictionary<string, FinalShapeSourceBuffer>? buffers = null;

        var requiredTargets = new HashSet<string>(StringComparer.Ordinal)
        {
            finalTableName
        };

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var node in block.Nodes)
            {
                if (!TryGetPostOperationSourceAndTarget(node, out var source, out var target) ||
                    !requiredTargets.Contains(target.Name) ||
                    !rowShapesByTableName.TryGetValue(source.Name, out var sourceShape) ||
                    !CanUseFinalShapeSourceBuffer(sourceShape, shapeFields))
                {
                    continue;
                }

                buffers ??= new Dictionary<string, FinalShapeSourceBuffer>(StringComparer.Ordinal);
                if (buffers.TryAdd(source.Name, new FinalShapeSourceBuffer(shapeTypeName, shapeFields)))
                    changed |= requiredTargets.Add(source.Name);
            }
        }

        return buffers ?? (IReadOnlyDictionary<string, FinalShapeSourceBuffer>)
            new Dictionary<string, FinalShapeSourceBuffer>(StringComparer.Ordinal);
    }

    private static bool TryGetPostOperationSourceAndTarget(
        ExecutionNode node,
        out ExecutionVariable source,
        out ExecutionVariable target)
    {
        switch (node)
        {
            case ExecutionDistinctTable distinct:
                source = distinct.Source;
                target = distinct.Target;
                return true;
            case ExecutionSortTable sort:
                source = sort.Source;
                target = sort.Target;
                return true;
            case ExecutionTopNTable topN:
                source = topN.Source;
                target = topN.Target;
                return true;
            case ExecutionTopOffsetTable topOffset:
                source = topOffset.Source;
                target = topOffset.Target;
                return true;
            case ExecutionSkipTable skip:
                source = skip.Source;
                target = skip.Target;
                return true;
            case ExecutionTakeTable take:
                source = take.Source;
                target = take.Target;
                return true;
            case ExecutionSliceTable slice:
                source = slice.Source;
                target = slice.Target;
                return true;
            default:
                source = null!;
                target = null!;
                return false;
        }
    }

    private static bool CanUseFinalShapeSourceBuffer(
        GeneratedRowShape sourceShape,
        IReadOnlyList<FieldBinding> shapeFields)
    {
        if (sourceShape.Fields.Count != shapeFields.Count)
            return false;

        for (var index = 0; index < shapeFields.Count; index++)
        {
            var sourceField = sourceShape.Fields[index];
            var shapeField = shapeFields[index];
            if (sourceField.Type != shapeField.Type ||
                !string.Equals(ExecutionCSharpRenderer.GetGeneratedFieldName(sourceField), ExecutionCSharpRenderer.GetGeneratedFieldName(shapeField), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsFinalTableBoundaryNode(ExecutionNode node, string finalTableName)
    {
        return node switch
        {
            ExecutionCreateTable createTable when createTable.Table.Name == finalTableName => true,
            ExecutionEnsureTableCapacity ensureCapacity when ensureCapacity.Table.Name == finalTableName => true,
            ExecutionReturnTable returnTable when returnTable.Table.Name == finalTableName => true,
            _ => false
        };
    }
}
