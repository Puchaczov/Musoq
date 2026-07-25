using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;
namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    public MethodDeclarationSyntax RenderMethod(ExecutionPlan plan, string methodName) => RenderMethod(plan, methodName, methodName);

    public MethodDeclarationSyntax RenderMethod(ExecutionPlan plan, string methodName, string queryIdentifier)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryIdentifier);
        var context = InitializeRenderContext(plan);
        return RenderMethod(plan, methodName, queryIdentifier, context);
    }

    internal MethodDeclarationSyntax RenderMethod(
        ExecutionPlan plan,
        string methodName,
        string queryIdentifier,
        ExecutionRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryIdentifier);
        ArgumentNullException.ThrowIfNull(context);
        var session = context.Session;

        session.TypedStoredTableResults = CreateTypedStoredTableResults(plan);
        session.IncludeCteIndexResults = PlanUsesCteIndexResults(plan);
        session.IncludeCteRowResults = session.TypedStoredTableResults.Count > 0;
        session.IncludeTableResults = PlanUsesTableResults(plan, session.TypedStoredTableResults);
        session.GeneratedRowVariableTypeNamesByName = CollectGeneratedRowVariableTypeNames(plan.Body, session.TypedStoredTableResults);
        session.GeneratedRowConstructorUsagesByType = CollectGeneratedRowConstructorUsages(plan.Body, session.TypedStoredTableResults);
        session.TypedRowBufferVariables = CreateTypedRowBufferVariables(plan.Body);
        session.SingleKeyAggregateUpdateHelpersByBlock = CollectSingleKeyAggregateUpdateHelpersByBlock(plan.Body);
        session.EnumerableTraversalHelpersByBlock = CollectEnumerableTraversalHelpersByBlock(plan.Body, context);

        return CreateQueryMethod(
            methodName,
            RenderMethodBody(plan, queryIdentifier, context),
            context);
    }

    public BlockSyntax RenderBlock(ExecutionBlock block)
    {
        return RenderBlock(block, CreateIsolatedRenderContext());
    }

    private BlockSyntax RenderBlock(ExecutionBlock block, ExecutionRenderSession session)
    {
        return RenderBlock(block, new ExecutionRenderContext(_renderOptions, session));
    }

    private BlockSyntax RenderBlock(ExecutionBlock block, ExecutionRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(block);
        var session = context.Session;
        if (!session.SuppressedEnumerableTraversalHelperBlocks.Contains(block) &&
            session.EnumerableTraversalHelpersByBlock.TryGetValue(block, out var traversalHelper))
        {
            return StatementEmitter.CreateBlock(CreateEnumerableTraversalInvocation(traversalHelper));
        }
        if (!session.SuppressSingleKeyAggregateUpdateHelpers &&
            session.SingleKeyAggregateUpdateHelpersByBlock.TryGetValue(block, out var helper))
        {
            return StatementEmitter.CreateBlock(CreateSingleKeyAggregateUpdateInvocation(helper));
        }
        return StatementEmitter.CreateBlock(RenderBlockNodes(block.Nodes, context));
    }

    private BlockSyntax RenderMethodBody(
        ExecutionPlan plan,
        string queryIdentifier,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        var block = plan.Body;
        var reflectedAccessors = CollectReflectedMemberAccessors(plan);
        session.StoredRowsCacheNames = CreateStoredRowsCacheNames(block);
        session.DeclaredStoredRowsCaches = [];
        session.ReflectedMemberAccessorNames = reflectedAccessors.ToDictionary(
            static accessor => accessor.Key,
            static accessor => accessor.VariableName,
            StringComparer.Ordinal);
        session.TableRowShapesByVariableName = CreateTableRowShapeMap(block);
        session.GeneratedRowVariableTypeNamesByName = CollectGeneratedRowVariableTypeNames(block, session.TypedStoredTableResults);
        session.StoredGeneratedRowsLoopNameCounts = [];
        session.TypedRowBufferVariables = CreateTypedRowBufferVariables(block);
        session.OperatorCatalog = ExecutionPlanOperatorCatalog.Create(plan);
        session.ProfileRecorderInScope = IsInstrumentationEnabled;

        var statements = new List<StatementSyntax>();
        if (session.UseQueryRunContext)
            statements.AddRange(CreateQueryRunContextAliasStatements());

        statements.AddRange(CreateOpeningPhaseStatements(block, queryIdentifier));
        statements.AddRange(CreateExecutionStateDeclarations(plan, context));
        statements.AddRange(CreateScriptParameterBindingStatements());
        statements.AddRange(CreateScriptVariableBindingStatements());
        statements.AddRange(reflectedAccessors.Select(CreateReflectedMemberAccessorDeclaration));
        statements.AddRange(CollectMethodCallCaches(block)
            .Select(cache => RenderCreateObject(new ExecutionCreateObject(cache))));

        var bodyStatements = RenderMethodStatements(block, context);
        var operatorProfileUsage = CollectOperatorProfileUsage(bodyStatements);
        statements.AddRange(CreateOperatorHandleDeclarations(operatorProfileUsage, context).Concat(CreateOperatorCounterDeclarations(operatorProfileUsage, context)));

        foreach (var statement in bodyStatements)
        {
            if (statement is ReturnStatementSyntax)
                statements.AddRange(CreateOperatorCounterFlushStatements(operatorProfileUsage, context).Concat(CreateClosingPhaseStatements(block, queryIdentifier)));

            statements.Add(statement);
        }

        return CreateProfileExceptionBoundaryBlock(statements, context);
    }
}
