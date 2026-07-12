using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

internal static class OperatorProfileCounterFacts
{
    public static IEnumerable<StatementSyntax> CreateScopeInputRowStatements(
        ExecutionPlanOperatorDescriptor descriptor,
        ExecutionNode node)
    {
        var expression = CreateInputRowsExpression(node);
        return expression == null
            ? []
            : [CreateScopeRowsStatement(CreateScopeVariableName(descriptor), nameof(OperatorProfileScope.AddInputRows), expression)];
    }

    public static IEnumerable<StatementSyntax> CreateScopeOutputRowStatements(
        ExecutionPlanOperatorDescriptor descriptor,
        ExecutionNode node)
    {
        var expression = CreateOutputRowsExpression(node);
        return expression == null
            ? []
            : [CreateScopeRowsStatement(CreateScopeVariableName(descriptor), nameof(OperatorProfileScope.AddOutputRows), expression)];
    }

    public static IEnumerable<StatementSyntax> CreateCounterInputRowStatements(
        ExecutionPlanOperatorDescriptor descriptor,
        ExecutionNode node)
    {
        var expression = CreateInputRowsExpression(node);
        return expression == null
            ? []
            : [CreateCounterRowsStatement(CreateInputRowsVariableName(descriptor), expression)];
    }

    public static IEnumerable<StatementSyntax> CreateCounterOutputRowStatements(
        ExecutionPlanOperatorDescriptor descriptor,
        ExecutionNode node)
    {
        var expression = CreateOutputRowsExpression(node);
        return expression == null
            ? []
            : [CreateCounterRowsStatement(CreateOutputRowsVariableName(descriptor), expression)];
    }

    public static ExpressionSyntax? CreateInputRowsExpression(ExecutionNode node)
    {
        return ExecutionNodeFacts.TryGetTablePostOperation(node, out var tablePostOperation)
            ? CreateCountExpression(tablePostOperation.Source.Name)
            : node switch
            {
                ExecutionOrderRecordList orderRecords => CreateCountExpression(orderRecords.Source.Name),
                ExecutionParallelFilterProjectLoop parallelProject => TryCreateCountExpression(parallelProject.SourceRows),
                ExecutionParallelSingleKeyAggregateLoop parallelAggregate => TryCreateCountExpression(parallelAggregate.SourceRows),
                ExecutionHashProbe => CreateLiteralOne(),
                ExecutionKeySetProbe => CreateLiteralOne(),
                ExecutionGetOrAddSingleKeyAggregateGroup => CreateLiteralOne(),
                ExecutionGetOrAddValueTupleAggregateGroup => CreateLiteralOne(),
                _ => null
            };
    }

    public static ExpressionSyntax? CreateOutputRowsExpression(ExecutionNode node)
    {
        return ExecutionNodeFacts.TryGetTablePostOperation(node, out var tablePostOperation)
            ? CreateCountExpression(tablePostOperation.Target.Name)
            : node switch
            {
                ExecutionAppendRow => CreateLiteralOne(),
                ExecutionAppendExistingRow => CreateLiteralOne(),
                ExecutionAppendRecord => CreateLiteralOne(),
                ExecutionCreateValuesRows valuesRows => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(valuesRows.Rows.Name),
                    SyntaxFactory.IdentifierName("Length")),
                ExecutionMaterializeList materialize => CreateCountExpression(materialize.Buffer.Name),
                ExecutionMaterializeFilteredList materialize => CreateCountExpression(materialize.Buffer.Name),
                ExecutionMaterializeExpandoList materialize => CreateCountExpression(materialize.Buffer.Name),
                ExecutionOrderRecordList orderRecords => CreateCountExpression(orderRecords.Source.Name),
                ExecutionParallelFilterProjectLoop parallelProject => CreateCountExpression(parallelProject.AppendRow.Table.Name),
                ExecutionParallelSingleKeyAggregateLoop parallelAggregate => CreateCountExpression(parallelAggregate.GroupsToFinalize.Name),
                ExecutionStoreTable store => CreateCountExpression(store.Table.Name),
                ExecutionHashAdd => CreateLiteralOne(),
                ExecutionKeySetAdd => CreateLiteralOne(),
                ExecutionAggregateSet => CreateLiteralOne(),
                ExecutionAggregateCapturedValueSet => CreateLiteralOne(),
                ExecutionGetOrAddSingleKeyAggregateGroup => CreateLiteralOne(),
                ExecutionGetOrAddValueTupleAggregateGroup => CreateLiteralOne(),
                _ => null
            };
    }

    public static LocalDeclarationStatementSyntax CreateLongCounterDeclaration(string name)
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword)))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(name)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(0L)))))));
    }

    public static StatementSyntax CreateCounterFlushStatement(
        string profileRecorderVariableName,
        string methodName,
        ExecutionPlanOperatorDescriptor descriptor,
        string rowsVariableName)
    {
        return StatementEmitter.CreateIf(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.GreaterThanExpression,
                SyntaxFactory.IdentifierName(rowsVariableName),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(0L))),
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.ConditionalAccessExpression(
                    SyntaxFactory.IdentifierName(profileRecorderVariableName),
                    SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberBindingExpression(
                                SyntaxFactory.IdentifierName(methodName)))
                        .WithArgumentList(CreateArgumentList(
                            SyntaxFactory.IdentifierName(CreateHandleVariableName(descriptor)),
                            SyntaxFactory.IdentifierName(rowsVariableName))))));
    }

    public static StatementSyntax CreateScopeRowsStatement(
        string scopeVariableName,
        string methodName,
        ExpressionSyntax rowsExpression)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(scopeVariableName),
                        SyntaxFactory.IdentifierName(methodName)))
                .WithArgumentList(CreateArgumentList(rowsExpression)));
    }

    public static StatementSyntax CreateCounterRowsStatement(
        string counterName,
        ExpressionSyntax rowsExpression)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.AddAssignmentExpression,
                SyntaxFactory.IdentifierName(counterName),
                rowsExpression));
    }

    public static bool UsesLoopRowCounters(ExecutionNode node) =>
        node is ExecutionForEach or ExecutionForEach or ExecutionForEachWithOrdinality or ExecutionForEachWithOrdinality or ExecutionForEachIndexed;

    public static bool IsCounterOnlyNode(ExecutionNode node) =>
        node is
            ExecutionLet or
            ExecutionIf or
            ExecutionAppendRow or
            ExecutionAppendExistingRow or
            ExecutionAppendRecord or
            ExecutionAssign or
            ExecutionArrayAssign or
            ExecutionContinueIf or
            ExecutionGetOrAddSingleKeyAggregateGroup or
            ExecutionGetOrAddValueTupleAggregateGroup or
            ExecutionHashAdd or
            ExecutionHashProbe or
            ExecutionKeySetAdd or
            ExecutionKeySetProbe or
            ExecutionAggregateSet or
            ExecutionAggregateCapturedValueSet ||
        node is ExecutionForEach forEach && IsProbeMatchLoop(forEach);

    public static bool IsCounterOnlyDescriptor(ExecutionPlanOperatorDescriptor descriptor) =>
        descriptor.NodeKind is
            "Let" or
            "If" or
            "AppendRow" or
            "AppendShape" or
            "AppendRowBuffer" or
            "AppendExistingRow" or
            "AppendExistingShape" or
            "AppendExistingRowBuffer" or
            "AppendRecord" or
            "Assign" or
            "ArrayAssign" or
            "ContinueIf" or
            "GetOrAddSingleKeyAggregateGroup" or
            "GetOrAddValueTupleAggregateGroup" or
            "HashAdd" or
            "HashProbe" or
            "KeySetAdd" or
            "KeySetProbe" or
            "TypedAggregateSet" or
            "AggregateCapturedValueSet" ||
        IsProbeMatchLoopDescriptor(descriptor);

    public static string CreateScopeVariableName(ExecutionPlanOperatorDescriptor descriptor)
    {
        return $"__{descriptor.Id}Scope";
    }

    public static string CreateHandleVariableName(ExecutionPlanOperatorDescriptor descriptor)
    {
        return $"__{descriptor.Id}Handle";
    }

    public static string CreateInputRowsVariableName(string operatorId) => $"__{operatorId}InputRows";

    public static string CreateOutputRowsVariableName(string operatorId) => $"__{operatorId}OutputRows";

    public static string CreateInputRowsVariableName(ExecutionPlanOperatorDescriptor descriptor) =>
        CreateInputRowsVariableName(descriptor.Id);

    public static string CreateOutputRowsVariableName(ExecutionPlanOperatorDescriptor descriptor) =>
        CreateOutputRowsVariableName(descriptor.Id);

    private static ExpressionSyntax CreateCountExpression(string variableName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.IdentifierName("Count"));
    }

    private static ExpressionSyntax? TryCreateCountExpression(ExecutionExpression expression)
    {
        return expression is ExecutionVariableRead variableRead
            ? CreateCountExpression(variableRead.Variable.Name)
            : null;
    }

    private static ExpressionSyntax CreateLiteralOne()
    {
        return SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(1));
    }

    private static bool IsProbeMatchLoop(ExecutionForEach forEach) =>
        forEach.Source is ExecutionVariableRead { Variable.Name: var name } &&
        name.EndsWith("Matches", StringComparison.Ordinal);

    private static bool IsProbeMatchLoopDescriptor(ExecutionPlanOperatorDescriptor descriptor) =>
        descriptor.NodeKind.Equals("ForEach", StringComparison.Ordinal) &&
        descriptor.DisplayName.Contains("Matches]", StringComparison.Ordinal);

    private static ArgumentListSyntax CreateArgumentList(params ExpressionSyntax[] arguments)
    {
        return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments.Select(SyntaxFactory.Argument)));
    }
}
