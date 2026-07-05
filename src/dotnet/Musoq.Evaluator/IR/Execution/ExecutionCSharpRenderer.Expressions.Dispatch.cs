using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax RenderExpression(ExecutionExpression expression)
    {
        return RenderExpression(expression, CreateIsolatedRenderContext());
    }

    private ExpressionSyntax RenderExpression(ExecutionExpression expression, ExecutionRenderContext context)
    {
        return new ExpressionRenderer(this, context).Render(expression);
    }

    private static ExpressionSyntax CreateWindowValueRead(ExecutionWindowValueRead windowValueRead)
    {
        var value = CreateElementAccess(
            SyntaxFactory.IdentifierName(windowValueRead.Results.Name),
            SyntaxFactory.IdentifierName(windowValueRead.Index.Name));

        if (windowValueRead.ReturnType == typeof(object))
            return value;

        return SyntaxFactory.CastExpression(CreateTypeSyntax(windowValueRead.ReturnType), value);
    }

    private static MemberAccessExpressionSyntax CreateGroupKeyRead(ExecutionGroupKeyRead groupKeyRead)
    {
        var key = groupKeyRead.Key
            ?? throw new InvalidOperationException("Typed aggregate group key read requires a key field descriptor.");

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(groupKeyRead.Group.Name),
            SyntaxFactory.IdentifierName(key.FieldName));
    }

    private static ExpressionSyntax CreateAggregateAccumulatorRead(ExecutionAggregateCall aggregateCall)
    {
        var inlineGet = TryCreateInlineAggregateGet(aggregateCall);
        if (inlineGet is not null)
            return inlineGet;

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    CreateTypeSyntax(aggregateCall.Accumulator.Kernel.KernelType),
                    SyntaxFactory.IdentifierName("Get")))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(CreateAggregateAccumulatorAccess(aggregateCall.Group, aggregateCall.Accumulator))
                    .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.InKeyword)))));
    }

    private static MemberAccessExpressionSyntax CreateAggregateAccumulatorAccess(
        ExecutionVariable group,
        AggregateAccumulatorField accumulator)
    {
        return CreateAggregateAccumulatorAccess(SyntaxFactory.IdentifierName(group.Name), accumulator);
    }

    private static MemberAccessExpressionSyntax CreateAggregateAccumulatorAccess(
        ExpressionSyntax target,
        AggregateAccumulatorField accumulator,
        bool followOwner = true)
    {
        var owner = CreateAggregateAccumulatorOwnerAccess(target, accumulator, followOwner);
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            owner,
            SyntaxFactory.IdentifierName(accumulator.FieldName));
    }

    private static ExpressionSyntax CreateAggregateAccumulatorOwnerAccess(
        ExpressionSyntax target,
        AggregateAccumulatorField accumulator,
        bool followOwner)
    {
        if (!followOwner || accumulator.OwnerFieldName is null)
            return target;

        return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                target,
                SyntaxFactory.IdentifierName(accumulator.OwnerFieldName));
    }

    private static ExpressionSyntax CreateAggregateCapturedValueRead(ExecutionAggregateCapturedValueRead capturedValueRead)
    {
        var read = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(capturedValueRead.Group.Name),
            SyntaxFactory.IdentifierName(capturedValueRead.CapturedField.FieldName));

        return CastIfNeeded(read, capturedValueRead.ReturnType);
    }

    private static ElementAccessExpressionSyntax CreateStoredTableRead(int tableIndex)
    {
        return CreateElementAccess(
            SyntaxFactory.IdentifierName("_tableResults"),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(tableIndex)));
    }

    private ExpressionSyntax RenderStoredTableRows(
        ExecutionStoredTableRows storedRows,
        ExecutionRenderContext context)
    {
        return context.Session.DeclaredStoredRowsCaches.Contains(storedRows.TableIndex) &&
               context.Session.StoredRowsCacheNames.TryGetValue(storedRows.TableIndex, out var cacheName)
            ? CreateIdentifierName(cacheName)
            : CreateStoredTableRowsRead(storedRows, context);
    }

    private static ArrayCreationExpressionSyntax CreateNullContextArray(int count)
    {
        return CreateArrayCreation(
            "object",
            Enumerable.Repeat(
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression),
                count));
    }

    private ExpressionSyntax CreateStoredTableRowsRead(int tableIndex, ExecutionRenderContext context)
    {
        if (TryGetTypedStoredTableResult(tableIndex, context, out _))
            return CreateCteRowResultSlotAccess(tableIndex);

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            CreateStoredTableRead(tableIndex),
            SyntaxFactory.IdentifierName("Rows"));
    }

    private ExpressionSyntax RenderRowPresence(
        ExecutionRowPresence rowPresence,
        ExecutionRenderContext context)
    {
        var boxedSource = SyntaxFactory.CastExpression(
            CreateTypeSyntax(typeof(object)),
            SyntaxFactory.ParenthesizedExpression(RenderExpression(rowPresence.PresenceSource, context)));
        var nullLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        var kind = rowPresence.IsPresent
            ? SyntaxKind.NotEqualsExpression
            : SyntaxKind.EqualsExpression;

        return SyntaxFactory.ParenthesizedExpression(
            SyntaxFactory.BinaryExpression(kind, boxedSource, nullLiteral));
    }

    private ExpressionSyntax CreateStoredTableRowsRead(
        ExecutionStoredTableRows storedRows,
        ExecutionRenderContext context)
    {
        if (storedRows.GeneratedRowShape != null &&
            TryGetTypedStoredTableResult(storedRows.TableIndex, storedRows.GeneratedRowShape, context, out _))
        {
            return CreateCteRowResultSlotAccess(storedRows.TableIndex);
        }

        var rows = storedRows.GeneratedRowShape == null
            ? CreateStoredTableRowsRead(storedRows.TableIndex, context)
            : SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                CreateStoredTableRead(storedRows.TableIndex),
                SyntaxFactory.IdentifierName("Rows"));

        if (storedRows.GeneratedRowShape == null)
        {
            return rows;
        }

        return SyntaxFactory.InvocationExpression(CreateGenericEvaluationHelperMemberAccess(
                nameof(EvaluationHelper.CastGeneratedRows),
                storedRows.GeneratedRowShape.TypeName))
            .WithArgumentList(CreateArgumentList(rows));
    }

    private ExpressionSyntax RenderArrayAccess(
        ExecutionArrayAccess arrayAccess,
        ExecutionRenderContext context)
    {
        var arrayExpression = RenderExpression(arrayAccess.Array, context);
        while (arrayExpression is CastExpressionSyntax castExpression)
            arrayExpression = castExpression.Expression;

        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(SafeArrayAccess)),
                    SyntaxFactory.IdentifierName(nameof(SafeArrayAccess.GetIndexedElement))))
            .WithArgumentList(CreateArgumentList(
                arrayExpression,
                RenderExpression(arrayAccess.Index, context),
                SyntaxFactory.TypeOfExpression(CreateTypeSyntax(arrayAccess.ElementType))));

        return CastIfNeeded(invocation, arrayAccess.ReturnType);
    }

    private static ObjectCreationExpressionSyntax RenderIndexedHashRowCreate(ExecutionIndexedHashRowCreate indexedRowCreate)
    {
        var typeName = string.IsNullOrWhiteSpace(indexedRowCreate.GeneratedRowTypeName)
            ? CreateTypeSyntax(indexedRowCreate.ReturnType)
            : SyntaxFactory.ParseTypeName(indexedRowCreate.GeneratedRowTypeName);

        return SyntaxFactory.ObjectCreationExpression(typeName)
            .WithArgumentList(CreateArgumentList(
                CreateIdentifierName(indexedRowCreate.Row.Name),
                CreateIdentifierName(indexedRowCreate.Index.Name)));
    }

    private static MemberAccessExpressionSyntax CreateIndexedHashRowMemberRead(
        ExecutionVariable indexedRow,
        string memberName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            CreateIdentifierName(indexedRow.Name),
            SyntaxFactory.IdentifierName(memberName));
    }
}
