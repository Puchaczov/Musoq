using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

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
        var context = InitializeRenderContext(plan);
        var session = context.Session;
        session.QueryIdentifier = queryIdentifier;
        session.UseDirectTypedStoredRowsAlias = CanUseGeneratedFinalRowSink(plan, finalTableName);
        var previousUseQueryRunContext = session.UseQueryRunContext;
        if (useQueryRunContext)
            session.UseQueryRunContext = true;

        try
        {
            session.TypedStoredTableResults = CreateTypedStoredTableResults(plan);
            session.IncludeCteIndexResults = PlanUsesCteIndexResults(plan);
            session.IncludeCteRowResults = session.TypedStoredTableResults.Count > 0;
            session.IncludeTableResults = PlanUsesTableResults(plan, session.TypedStoredTableResults);
            session.GeneratedRowVariableTypeNamesByName = CollectGeneratedRowVariableTypeNames(
                plan.Body,
                session.TypedStoredTableResults);
            session.GeneratedRowConstructorUsagesByType = CollectGeneratedRowConstructorUsages(
                plan.Body,
                session.TypedStoredTableResults);
            var usesGeneratedRowCarrier = CanUseGeneratedFinalRowSink(plan, finalTableName);
            session.DirectSortedRowBufferSources = usesGeneratedRowCarrier
                ? plan.Body.Nodes
                    .OfType<ExecutionSortTable>()
                    .ToDictionary(static sort => sort.Target.Name, static sort => sort.Source.Name, StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal);
            var sinkTypeName = usesGeneratedRowCarrier && plan.FinalResult is { } finalResult
                ? finalResult.Shape.TypeName
                : shapeTypeName;
            var finalShapeBufferName = usesGeneratedRowCarrier
                ? null
                : bufferFinalShapes
                    ? "__musoqFinalShapeRows"
                    : null;
            var finalShapeSourceBuffers = CreateFinalShapeSourceBuffers(plan.Body, finalTableName, shapeTypeName, shapeFields);
            session.FinalShapeYieldSink = new FinalShapeYieldSink(
                finalTableName,
                sinkTypeName,
                shapeFields,
                finalShapeBufferName,
                usesGeneratedRowCarrier ? null : finalShapeSourceBuffers,
                usesGeneratedRowCarrier);
            session.SingleKeyAggregateUpdateHelpersByBlock = CollectSingleKeyAggregateUpdateHelpersByBlock(plan.Body);
            session.EnumerableTraversalHelpersByBlock = Enumerable
                .Where<KeyValuePair<ExecutionBlock, EnumerableTraversalHelper>>(CollectEnumerableTraversalHelpersByBlock(plan.Body, context), pair => !CapturesCurrentFinalShapeTargetOrSourceBuffer(pair.Value, context))
                .ToDictionary(static pair => pair.Key, static pair => pair.Value);

            return SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.ParseTypeName($"IEnumerable<{sinkTypeName}>"),
                    SyntaxFactory.Identifier(methodName))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                .WithParameterList(CreateFinalShapeRowsParameterList(useQueryRunContext, includeProfileRecorderParameter))
                .WithBody(RenderFinalShapeRowsMethodBody(
                    plan,
                    queryIdentifier,
                    finalTableName,
                    sinkTypeName,
                    finalShapeBufferName,
                    context));
        }
        finally
        {
            session.UseQueryRunContext = previousUseQueryRunContext;
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
        string? finalShapeBufferName,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        var block = plan.Body;
        session.StoredRowsCacheNames = CreateStoredRowsCacheNames(block);
        session.DeclaredStoredRowsCaches = [];
        session.TableRowShapesByVariableName = CreateTableRowShapeMap(block);
        session.GeneratedRowVariableTypeNamesByName = CollectGeneratedRowVariableTypeNames(
            block,
            session.TypedStoredTableResults);
        session.StoredGeneratedRowsLoopNameCounts = [];
        session.TypedRowBufferVariables = CreateTypedRowBufferVariables(block, finalTableName);
        session.OperatorCatalog = ExecutionPlanOperatorCatalog.Create(plan);
        session.ProfileRecorderInScope = IsInstrumentationEnabled;

            var statements = new List<StatementSyntax>();
            statements.AddRange(CreateQueryRunContextAliasStatements(
                session.UseQueryRunContext,
                queryIdentifier: queryIdentifier));

            statements.AddRange(CreateOpeningPhaseStatements(block, queryIdentifier));

            var tryStatements = new List<StatementSyntax>();
            tryStatements.AddRange(CreateExecutionStateDeclarations(plan, context));
            tryStatements.AddRange(CreateScriptParameterBindingStatements());
            tryStatements.AddRange(CreateScriptVariableBindingStatements());
            tryStatements.AddRange(CollectMethodCallCaches(block)
                .Select(cache => RenderCreateObject(new ExecutionCreateObject(cache))));
            if (finalShapeBufferName != null)
            {
                tryStatements.Add(CreateLocalDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    finalShapeBufferName,
                    SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.ParseTypeName($"List<{context.Session.FinalShapeYieldSink?.ShapeTypeName ?? shapeTypeName}>"))
                        .WithArgumentList(SyntaxFactory.ArgumentList())));
            }

            var bodyStatements = RenderMethodStatements(RemoveFinalTableBoundary(block, finalTableName), context)
                .Select(statement => RewriteRemovedFinalTableCount(statement, finalTableName, finalShapeBufferName))
                .ToList();
            if (finalShapeBufferName != null)
                bodyStatements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(finalShapeBufferName)));

            var operatorProfileUsage = CollectOperatorProfileUsage(bodyStatements);
            tryStatements.AddRange(CreateOperatorHandleDeclarations(operatorProfileUsage, context).Concat(CreateOperatorCounterDeclarations(operatorProfileUsage, context)));
            tryStatements.AddRange(AddOperatorCounterFlushesBeforeTopLevelReturns(bodyStatements, operatorProfileUsage, context, appendAtEnd: true));

            statements.Add(SyntaxFactory.TryStatement(SyntaxFactory.Block(tryStatements), default, SyntaxFactory.FinallyClause(
                SyntaxFactory.Block(CreateClosingPhaseStatements(block, queryIdentifier)))));

            return CreateProfileExceptionBoundaryBlock(statements, context, includeExceptionBoundary: finalShapeBufferName != null);
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

    private bool CapturesCurrentFinalShapeTargetOrSourceBuffer(
        EnumerableTraversalHelper helper,
        ExecutionRenderContext context)
    {
        return helper.Captures.Any(capture => IsCurrentFinalShapeTargetOrSourceBuffer(capture.Name, context));
    }

    private static IReadOnlyDictionary<string, FinalShapeSourceBuffer> CreateFinalShapeSourceBuffers(
        ExecutionBlock block,
        string finalTableName,
        string shapeTypeName,
        IReadOnlyList<FieldBinding> shapeFields)
    {
        var rowShapesByTableName = CreateTableRowShapeMap(block);
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
                !string.Equals(GetGeneratedFieldName(sourceField), GetGeneratedFieldName(shapeField), StringComparison.Ordinal))
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
