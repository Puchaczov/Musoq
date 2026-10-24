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
        FinalOutputPlan? outputPlan = null,
        bool isSubqueryHelper = false)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(finalTableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeTypeName);
        if (outputPlan == null)
        {
            if (!TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out var renderPlan))
                throw new InvalidOperationException("Final output requires result metadata.");
            outputPlan = renderPlan.FinalSinkPlans.TableOutput;
        }
        var context = InitializeRenderContext(plan);
        var session = context.Session;
        session.IsSubqueryHelper = isSubqueryHelper;
        session.QueryIdentifier = queryIdentifier;
        session.ReuseStoredRows = outputPlan.ReuseStoredRows;
        var previousUseQueryRunContext = session.UseQueryRunContext;
        if (useQueryRunContext)
            session.UseQueryRunContext = true;

        try
        {
            session.TypedStoredTableResults = CreateTypedStoredTableResults(plan);
            session.ColumnarStoredTableResultsByTableName = CreateColumnarStoredTableResultsByTableName(
                plan,
                session.TypedStoredTableResults);
            session.IncludeCteIndexResults = PlanUsesCteIndexResults(plan);
            session.IncludeCteRowResults = session.TypedStoredTableResults.Count > 0;
            session.IncludeTableResults = PlanUsesTableResults(plan, session.TypedStoredTableResults);
            session.GeneratedRowVariableTypeNamesByName = CollectGeneratedRowVariableTypeNames(
                plan.Body,
                session.TypedStoredTableResults);
            session.GeneratedRowConstructorUsagesByType = CollectGeneratedRowConstructorUsages(
                plan.Body,
                session.TypedStoredTableResults);
            var sinkTypeName = outputPlan.UsesGeneratedRow ? outputPlan.Result.RowTypeName : shapeTypeName;
            session.DirectSortedRowBufferSources = outputPlan.SortedRowBufferSources;
            var finalShapeBufferName = outputPlan.BufferFinalRows ? "__musoqFinalShapeRows" : null;
            session.FinalShapeYieldSink = new FinalShapeYieldSink(
                finalTableName,
                sinkTypeName,
                shapeFields,
                finalShapeBufferName,
                CreateFinalShapeSourceBuffers(outputPlan, shapeFields),
                outputPlan.UsesGeneratedRow);
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
        session.RegexMatchTimeout = plan.RegexMatchTimeout;
        session.OperatorCatalog = ExecutionPlanOperatorCatalog.Create(plan);
        session.ProfileRecorderInScope = IsInstrumentationEnabled;

            var statements = new List<StatementSyntax>();
            if (!session.IsSubqueryHelper)
                statements.AddRange(CreateQueryRunContextAliasStatements(
                    session.UseQueryRunContext,
                    queryIdentifier: queryIdentifier));

            statements.AddRange(CreateOpeningPhaseStatements(block, queryIdentifier));

            var tryStatements = new List<StatementSyntax>();
            tryStatements.AddRange(CreateExecutionStateDeclarations(plan, context));
            if (!session.IsSubqueryHelper)
            {
                tryStatements.AddRange(CreateScriptParameterBindingStatements(plan));
                tryStatements.AddRange(CreateScriptVariableBindingStatements());
            }
            tryStatements.AddRange(CreateLikeMatcherStateDeclarations(plan, context));
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

            var bodyStatements = RenderMethodStatements(FinalShapeBoundaryProjection.Apply(block, finalTableName, finalShapeBufferName), context)
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


    private bool CapturesCurrentFinalShapeTargetOrSourceBuffer(
        EnumerableTraversalHelper helper,
        ExecutionRenderContext context)
    {
        return helper.Captures.Any(capture => IsCurrentFinalShapeTargetOrSourceBuffer(capture.Name, context));
    }

    private static IReadOnlyDictionary<string, FinalShapeSourceBuffer> CreateFinalShapeSourceBuffers(
        FinalOutputPlan output,
        IReadOnlyList<FieldBinding> fields)
    {
        return output.SourceBuffers.ToDictionary(
            static pair => pair.Key,
            pair => new FinalShapeSourceBuffer(pair.Value, fields),
            StringComparer.Ordinal);
    }

}
