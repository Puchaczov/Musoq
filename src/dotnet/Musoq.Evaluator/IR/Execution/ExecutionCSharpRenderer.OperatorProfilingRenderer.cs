using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderOperatorProfiledNode(
        ExecutionNode node,
        IEnumerable<StatementSyntax> statements)
    {
        var renderedStatements = statements.ToArray();
        if (!ShouldProfileOperatorNode(node) ||
            !_operatorCatalog.TryGetDescriptor(node, out var descriptor))
        {
            return renderedStatements;
        }

        if (OperatorProfileCounterFacts.IsCounterOnlyNode(node))
        {
            return
            [
                ..OperatorProfileCounterFacts.CreateCounterInputRowStatements(descriptor, node),
                ..renderedStatements,
                ..OperatorProfileCounterFacts.CreateCounterOutputRowStatements(descriptor, node)
            ];
        }

        return CanWrapOperatorStatementsInTryFinally(node)
            ?
            [
                ..CreateOperatorRowCounterDeclarations(descriptor, node),
                CreateOperatorScopeDeclaration(descriptor),
                CreateOperatorTryFinallyStatement(descriptor, node, renderedStatements)
            ]
            :
            [
                ..CreateOperatorRowCounterDeclarations(descriptor, node),
                CreateOperatorScopeDeclaration(descriptor),
                ..OperatorProfileCounterFacts.CreateScopeInputRowStatements(descriptor, node),
                ..renderedStatements,
                ..OperatorProfileCounterFacts.CreateScopeOutputRowStatements(descriptor, node),
                ..CreateOperatorFinallyStatements(descriptor, node)
            ];
    }

    private static LocalDeclarationStatementSyntax CreateOperatorScopeDeclaration(
        ExecutionPlanOperatorDescriptor descriptor)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            OperatorProfileCounterFacts.CreateScopeVariableName(descriptor),
            SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                SyntaxFactory.ConditionalAccessExpression(
                    SyntaxFactory.IdentifierName(ProfileRecorderVariableName),
                    SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberBindingExpression(
                                SyntaxFactory.IdentifierName(nameof(QueryProfileRecorder.BeginOperatorValue))))
                    .WithArgumentList(CreateArgumentList(
                        SyntaxFactory.IdentifierName(OperatorProfileCounterFacts.CreateHandleVariableName(descriptor))))),
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(OperatorProfileValueScope)),
                    SyntaxFactory.IdentifierName(nameof(OperatorProfileValueScope.None)))));
    }

    private IEnumerable<StatementSyntax> CreateOperatorHandleDeclarations(OperatorProfileUsage usage)
    {
        if (!IsOperatorProfilingEnabled)
            return [];

        return _operatorCatalog.NodeOperators
            .Where(descriptor =>
                usage.Contains(OperatorProfileCounterFacts.CreateHandleVariableName(descriptor)) ||
                usage.Contains(OperatorProfileCounterFacts.CreateInputRowsVariableName(descriptor)) ||
                usage.Contains(OperatorProfileCounterFacts.CreateOutputRowsVariableName(descriptor)))
            .Select(CreateOperatorHandleDeclaration)
            .ToArray();
    }

    private IEnumerable<StatementSyntax> CreateOperatorCounterDeclarations(OperatorProfileUsage usage)
    {
        if (!IsOperatorProfilingEnabled)
            return [];

        return _operatorCatalog.NodeOperators
            .Where(OperatorProfileCounterFacts.IsCounterOnlyDescriptor)
            .SelectMany(descriptor => CreateOperatorCounterDeclarations(descriptor, usage))
            .ToArray();
    }

    private static LocalDeclarationStatementSyntax CreateOperatorHandleDeclaration(
        ExecutionPlanOperatorDescriptor descriptor)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            OperatorProfileCounterFacts.CreateHandleVariableName(descriptor),
            SyntaxFactory.BinaryExpression(
                SyntaxKind.CoalesceExpression,
                SyntaxFactory.ConditionalAccessExpression(
                    SyntaxFactory.IdentifierName(ProfileRecorderVariableName),
                    SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberBindingExpression(
                                SyntaxFactory.IdentifierName(nameof(QueryProfileRecorder.GetOperatorHandle))))
                        .WithArgumentList(CreateArgumentList(
                            CreateStringLiteral(descriptor.Id),
                            CreateStringLiteral(descriptor.NodeKind)))),
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(OperatorProfileHandle)),
                    SyntaxFactory.IdentifierName(nameof(OperatorProfileHandle.None)))));
    }

    private static StatementSyntax CreateOperatorScopeDisposeStatement(
        ExecutionPlanOperatorDescriptor descriptor)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(OperatorProfileCounterFacts.CreateScopeVariableName(descriptor)),
                    SyntaxFactory.IdentifierName(nameof(IDisposable.Dispose)))));
    }

    private static TryStatementSyntax CreateOperatorTryFinallyStatement(
        ExecutionPlanOperatorDescriptor descriptor,
        ExecutionNode node,
        IReadOnlyList<StatementSyntax> renderedStatements)
    {
        return SyntaxFactory.TryStatement()
            .WithBlock(StatementEmitter.CreateBlock(
            [
                ..OperatorProfileCounterFacts.CreateScopeInputRowStatements(descriptor, node),
                ..renderedStatements,
                ..OperatorProfileCounterFacts.CreateScopeOutputRowStatements(descriptor, node)
            ]))
            .WithFinally(SyntaxFactory.FinallyClause(StatementEmitter.CreateBlock(
                CreateOperatorFinallyStatements(descriptor, node))));
    }

    private static bool CanWrapOperatorStatementsInTryFinally(ExecutionNode node)
    {
        return node is
            ExecutionForEach or
            ExecutionForEachWithOrdinality or
            ExecutionForEachIndexed or
            ExecutionParallelBlock or
            ExecutionParallelFilterProjectLoop or
            ExecutionParallelSingleKeyAggregateLoop or
            ExecutionIf or
            ExecutionEnsureTableCapacity or
            ExecutionAsOfProbe or
            ExecutionRangeProbe or
            ExecutionStoreTable;
    }

    private static IEnumerable<StatementSyntax> CreateOperatorRowCounterDeclarations(
        ExecutionPlanOperatorDescriptor descriptor,
        ExecutionNode node)
    {
        return OperatorProfileCounterFacts.UsesLoopRowCounters(node)
            ?
            [
                OperatorProfileCounterFacts.CreateLongCounterDeclaration(OperatorProfileCounterFacts.CreateInputRowsVariableName(descriptor)),
                OperatorProfileCounterFacts.CreateLongCounterDeclaration(OperatorProfileCounterFacts.CreateOutputRowsVariableName(descriptor))
            ]
            : [];
    }

    private static IEnumerable<StatementSyntax> CreateOperatorCounterDeclarations(
        ExecutionPlanOperatorDescriptor descriptor,
        OperatorProfileUsage usage)
    {
        var inputRowsVariableName = OperatorProfileCounterFacts.CreateInputRowsVariableName(descriptor);
        if (usage.Contains(inputRowsVariableName))
            yield return OperatorProfileCounterFacts.CreateLongCounterDeclaration(inputRowsVariableName);

        var outputRowsVariableName = OperatorProfileCounterFacts.CreateOutputRowsVariableName(descriptor);
        if (usage.Contains(outputRowsVariableName))
            yield return OperatorProfileCounterFacts.CreateLongCounterDeclaration(outputRowsVariableName);
    }

    private static IEnumerable<StatementSyntax> CreateOperatorFinallyStatements(
        ExecutionPlanOperatorDescriptor descriptor,
        ExecutionNode node)
    {
        return OperatorProfileCounterFacts.UsesLoopRowCounters(node)
            ?
            [
                OperatorProfileCounterFacts.CreateScopeRowsStatement(
                    OperatorProfileCounterFacts.CreateScopeVariableName(descriptor),
                    nameof(OperatorProfileScope.AddInputRows),
                    SyntaxFactory.IdentifierName(OperatorProfileCounterFacts.CreateInputRowsVariableName(descriptor))),
                OperatorProfileCounterFacts.CreateScopeRowsStatement(
                    OperatorProfileCounterFacts.CreateScopeVariableName(descriptor),
                    nameof(OperatorProfileScope.AddOutputRows),
                    SyntaxFactory.IdentifierName(OperatorProfileCounterFacts.CreateOutputRowsVariableName(descriptor))),
                CreateOperatorScopeDisposeStatement(descriptor)
            ]
            : [CreateOperatorScopeDisposeStatement(descriptor)];
    }

    private bool ShouldProfileOperatorNode(ExecutionNode node)
    {
        return IsOperatorProfilingEnabled &&
               node is not ExecutionReturnTable and not ExecutionReturnDesc and not ExecutionContinue and not ExecutionBreak;
    }


    private IEnumerable<StatementSyntax> CreateOperatorCounterFlushStatements(OperatorProfileUsage usage)
    {
        if (!IsOperatorProfilingEnabled)
            return [];

        return _operatorCatalog.NodeOperators
            .Where(OperatorProfileCounterFacts.IsCounterOnlyDescriptor)
            .SelectMany(descriptor => CreateOperatorCounterFlushStatements(descriptor, usage))
            .ToArray();
    }

    private static IEnumerable<StatementSyntax> CreateOperatorCounterFlushStatements(
        ExecutionPlanOperatorDescriptor descriptor,
        OperatorProfileUsage usage)
    {
        var inputRowsVariableName = OperatorProfileCounterFacts.CreateInputRowsVariableName(descriptor);
        if (usage.Contains(inputRowsVariableName))
        {
            yield return OperatorProfileCounterFacts.CreateCounterFlushStatement(
                ProfileRecorderVariableName,
                nameof(QueryProfileRecorder.AddOperatorInputRows),
                descriptor,
                inputRowsVariableName);
        }

        var outputRowsVariableName = OperatorProfileCounterFacts.CreateOutputRowsVariableName(descriptor);
        if (usage.Contains(outputRowsVariableName))
        {
            yield return OperatorProfileCounterFacts.CreateCounterFlushStatement(
                ProfileRecorderVariableName,
                nameof(QueryProfileRecorder.AddOperatorOutputRows),
                descriptor,
                outputRowsVariableName);
        }
    }

    private static OperatorProfileUsage CollectOperatorProfileUsage(IEnumerable<StatementSyntax> statements)
    {
        var collector = new OperatorProfileUsageCollector();
        foreach (var statement in statements)
            collector.Visit(statement);

        return new OperatorProfileUsage(collector.Identifiers);
    }

    private sealed class OperatorProfileUsage(IReadOnlySet<string> identifiers)
    {
        public bool Contains(string identifier) => identifiers.Contains(identifier);
    }

    private sealed class OperatorProfileUsageCollector : CSharpSyntaxWalker
    {
        private readonly HashSet<string> _identifiers = new(StringComparer.Ordinal);

        public IReadOnlySet<string> Identifiers => _identifiers;

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            _identifiers.Add(node.Identifier.ValueText);
            base.VisitIdentifierName(node);
        }
    }
}
