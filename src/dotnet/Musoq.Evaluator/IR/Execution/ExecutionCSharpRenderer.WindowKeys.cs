using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static ExecutionWindowKeyArray? ResolveWindowKeyArray(
        ExecutionExpression? expression,
        ExecutionWindowKeyArray? keyArray,
        string defaultVariableName)
    {
        if (expression == null)
            return null;

        return keyArray ?? new ExecutionWindowKeyArray(
            new ExecutionVariable(defaultVariableName, CreateWindowKeyArrayType(expression.ReturnType)),
            true,
            CreateWindowKeyShape(expression.ReturnType));
    }

    private static ExecutionWindowKeyArray ResolveWindowKeyArray(
        ExecutionWindowKeyArray? keyArray,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeys,
        string defaultVariableName)
    {
        var elementType = ResolveWindowOrderKeyElementType(orderKeys);
        return keyArray ?? new ExecutionWindowKeyArray(
            new ExecutionVariable(defaultVariableName, elementType.MakeArrayType()),
            true,
            CreateWindowKeyShape(elementType));
    }

    private static ExecutionWindowKeyShape CreateWindowKeyShape(Type elementType)
    {
        return new ExecutionWindowKeyShape(elementType, elementType != typeof(object));
    }

    private static Type CreateWindowKeyArrayType(Type elementType)
    {
        return ResolveWindowKeyElementType(elementType).MakeArrayType();
    }

    private static Type ResolveWindowKeyElementType(Type elementType)
    {
        return CanUseTypedWindowKeyElement(elementType) ? elementType : typeof(object);
    }

    private static Type ResolveWindowOrderKeyElementType(IReadOnlyList<ExecutionWindowOrderKey> orderKeys)
    {
        if (orderKeys.Count == 1)
            return ResolveWindowKeyElementType(orderKeys[0].Expression.ReturnType);

        if (orderKeys.Count is < 2 or > 7)
            return typeof(object);

        var firstDirection = orderKeys[0].Descending;
        if (orderKeys.Any(key => key.Descending != firstDirection || !CanUseTypedWindowOrderKeyElement(key.Expression.ReturnType)))
            return typeof(object);

        return CreateValueTupleType(orderKeys.Select(key => key.Expression.ReturnType).ToArray());
    }

    private static bool CanUseTypedWindowKeyElement(Type type)
    {
        if (Nullable.GetUnderlyingType(type) != null)
            return false;

        return type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(bool) ||
               type == typeof(char) ||
               type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double);
    }

    private static bool CanUseTypedWindowOrderKeyElement(Type type)
    {
        return CanUseTypedWindowKeyElement(type) &&
               typeof(IComparable<>).MakeGenericType(type).IsAssignableFrom(type);
    }

    private static void AddWindowPartitionDeclarations(
        List<StatementSyntax> statements,
        ExecutionWindowPartitionSet? partitions,
        ExecutionVariable? partitionKeys,
        ExecutionVariable buffer,
        ExecutionVariable? partitionBuilder = null)
    {
        if (partitions is not { ShouldCreate: true })
            return;

        if (partitionBuilder != null)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                partitions.Variable.Name,
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(partitionBuilder.Name),
                        SyntaxFactory.IdentifierName(nameof(WindowPartitionBuilder<>.ToPartitionSet))))));
            return;
        }

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            partitions.Variable.Name,
            CreateWindowHelperInvocation(
                nameof(WindowFunctionHelpers.ResolvePartitionSet),
                CreateBufferCountExpression(buffer),
                CreatePartitionKeysArgument(partitionKeys))));
    }

    private static void AddWindowMethodTargetDeclarations(
        List<StatementSyntax> statements,
        IReadOnlyList<ExecutionVariable>? methodTargets)
    {
        if (methodTargets == null)
            return;

        foreach (var target in methodTargets)
        {
            statements.Add(RenderCreateAggregateLibrary(
                new ExecutionCreateAggregateLibrary(target, target.Type)));
        }
    }

    private static void AddWindowSortedPartitionDeclarations(
        List<StatementSyntax> statements,
        ExecutionWindowPartitionSet? sortedPartitions,
        ExecutionWindowPartitionSet? partitions,
        ExecutionWindowKeyArray orderKeys,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeyExpressions)
    {
        if (HasGeneratedWindowKeyType(orderKeys))
        {
            AddGeneratedWindowSortedPartitionDeclarations(
                statements,
                sortedPartitions,
                partitions,
                orderKeys.Variable);
            return;
        }

        AddWindowSortedPartitionDeclarations(
            statements,
            sortedPartitions,
            partitions,
            orderKeys.Variable,
            orderKeyExpressions);
    }

    private static void AddGeneratedWindowSortedPartitionDeclarations(
        List<StatementSyntax> statements,
        ExecutionWindowPartitionSet? sortedPartitions,
        ExecutionWindowPartitionSet? partitions,
        ExecutionVariable orderKeys)
    {
        if (sortedPartitions is not { ShouldCreate: true })
            return;

        if (partitions == null)
            throw new InvalidOperationException("Sorted window partitions require a partition set.");

        var helperName = sortedPartitions.SortInPlace
            ? nameof(WindowFunctionHelpers.SortStructPartitionSetInPlace)
            : nameof(WindowFunctionHelpers.SortStructPartitionSet);
        var invocation = CreateWindowHelperInvocation(
            helperName,
            SyntaxFactory.IdentifierName(partitions.Variable.Name),
            SyntaxFactory.IdentifierName(orderKeys.Name),
            CreateBooleanLiteral(false));

        if (sortedPartitions.SortInPlace)
        {
            statements.Add(SyntaxFactory.ExpressionStatement(invocation));
            return;
        }

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            sortedPartitions.Variable.Name,
            invocation));
    }

    private static void AddWindowSortedPartitionDeclarations(
        List<StatementSyntax> statements,
        ExecutionWindowPartitionSet? sortedPartitions,
        ExecutionWindowPartitionSet? partitions,
        ExecutionVariable orderKeys,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeyExpressions)
    {
        if (sortedPartitions is not { ShouldCreate: true })
            return;

        if (partitions == null)
            throw new InvalidOperationException("Sorted window partitions require a partition set.");

        var useScalarDirection = CanUseScalarOrderDirection(orderKeys, orderKeyExpressions);
        ExpressionSyntax directionArgument = useScalarDirection
            ? CreateBooleanLiteral(orderKeyExpressions[0].Descending)
            : CreateWindowOrderDescendingArray(orderKeyExpressions);

        if (sortedPartitions.SortInPlace)
        {
            statements.Add(SyntaxFactory.ExpressionStatement(
                CreateWindowHelperInvocation(
                    nameof(WindowFunctionHelpers.SortPartitionSetInPlace),
                    SyntaxFactory.IdentifierName(partitions.Variable.Name),
                    SyntaxFactory.IdentifierName(orderKeys.Name),
                    directionArgument)));
            return;
        }

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            sortedPartitions.Variable.Name,
            CreateWindowHelperInvocation(
                useScalarDirection
                    ? nameof(WindowFunctionHelpers.SortPartitionSetInPlace)
                    : nameof(WindowFunctionHelpers.SortPartitionSet),
                useScalarDirection
                    ? SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(partitions.Variable.Name),
                            SyntaxFactory.IdentifierName(nameof(WindowPartitionSet.Copy))))
                    : SyntaxFactory.IdentifierName(partitions.Variable.Name),
                SyntaxFactory.IdentifierName(orderKeys.Name),
                directionArgument)));
    }

    private static bool CanUseScalarOrderDirection(
        ExecutionVariable orderKeys,
        IReadOnlyList<ExecutionWindowOrderKey> orderKeyExpressions)
    {
        return orderKeyExpressions.Count == 1 &&
               CanUseTypedWindowOrderKeyElement(GetArrayElementType(orderKeys));
    }

    private ExpressionSyntax RenderWindowOrderKey(
        IReadOnlyList<ExecutionWindowOrderKey> orderKeys,
        ExecutionVariable orderKeyArray)
    {
        var expression = CreateWindowOrderKeyExpression(orderKeys, GetArrayElementType(orderKeyArray));

        return RenderExpression(expression);
    }

    private static ExecutionExpression CreateWindowOrderKeyExpression(
        IReadOnlyList<ExecutionWindowOrderKey> orderKeys,
        Type elementType)
    {
        if (orderKeys.Count == 1)
            return orderKeys[0].Expression;

        var parts = orderKeys.Select(key => key.Expression).ToArray();
        return IsValueTupleType(elementType, parts.Length)
            ? new ExecutionValueTupleKey(parts, elementType)
            : new ExecutionCompositeKey(parts);
    }
}
