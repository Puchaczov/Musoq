using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private StatementSyntax RenderAppendRow(ExecutionAppendRow appendRow, ExecutionRenderContext context)
    {
        appendRow = NormalizeLazyContextSegments(appendRow);

        if (context.Session.FinalShapeYieldSink is { } finalShapeYieldSink &&
            string.Equals(appendRow.Table.Name, finalShapeYieldSink.TableName, StringComparison.Ordinal))
        {
            return CreateFinalShapeOutputStatement(CreateFinalShapeCreation(finalShapeYieldSink.ShapeTypeName, appendRow), context);
        }

        if (TryGetFinalShapeSourceBuffer(appendRow.Table.Name, context, out var finalShapeSourceBuffer))
            return CreateRowBufferAddStatement(
                appendRow.Table.Name,
                CreateFinalShapeCreation(finalShapeSourceBuffer.ShapeTypeName, appendRow));

        if (TryGetTypedRowBufferShape(appendRow.Table.Name, context, out _))
            return CreateRowBufferAddStatement(appendRow.Table.Name, CreateGeneratedRowCreation(appendRow, context));

        return CreateTableAddStatement(
            appendRow.Table.Name,
            CreateGeneratedRowCreation(appendRow, context),
            appendRow.AppendMode);
    }

    private StatementSyntax RenderAppendExistingRow(ExecutionAppendExistingRow appendRow, ExecutionRenderContext context)
    {
        if (context.Session.FinalShapeYieldSink is { } finalShapeYieldSink &&
            string.Equals(appendRow.Table.Name, finalShapeYieldSink.TableName, StringComparison.Ordinal))
        {
            return CreateFinalShapeOutputStatement(CreateFinalShapeCreationFromRow(appendRow.Row.Name, context), context);
        }

        if (TryGetFinalShapeSourceBuffer(appendRow.Table.Name, context, out _))
            return CreateRowBufferAddStatement(
                appendRow.Table.Name,
                CreateFinalShapeCreationFromRow(appendRow.Row.Name, context));

        if (TryGetTypedRowBufferShape(appendRow.Table.Name, context, out _))
            return CreateRowBufferAddStatement(
                appendRow.Table.Name,
                SyntaxFactory.IdentifierName(appendRow.Row.Name));

        return CreateTableAddStatement(
            appendRow.Table.Name,
            SyntaxFactory.IdentifierName(appendRow.Row.Name),
            appendRow.AppendMode);
    }

    private ObjectCreationExpressionSyntax CreateGeneratedRowCreation(ExecutionAppendRow appendRow)
    {
        return CreateGeneratedRowCreation(appendRow, CreateIsolatedRenderContext());
    }

    private ObjectCreationExpressionSyntax CreateGeneratedRowCreation(ExecutionAppendRow appendRow, ExecutionRenderContext context)
    {
        return CreateGeneratedRowCreation(
            appendRow.RowShape,
            appendRow.Values,
            appendRow.Contexts,
            appendRow.ContextLayout,
            context);
    }

    private ExpressionSyntax RenderRowConstructorValue(
        ExecutionExpression expression,
        Type targetType)
    {
        return RenderRowConstructorValue(expression, targetType, CreateIsolatedRenderContext());
    }

    private ExpressionSyntax RenderRowConstructorValue(
        ExecutionExpression expression,
        Type targetType,
        ExecutionRenderContext context)
    {
        return expression is ExecutionBinary binary &&
               RequiresNullableTemporalSubtraction(binary) &&
               CanBeNull(targetType)
            ? RenderNullableTemporalSubtractionValue(binary, context)
            : expression is ExecutionBinary nullableBinary &&
              CanRenderBinaryAsNullableTarget(nullableBinary, targetType)
                ? RenderExpression(nullableBinary with { ReturnType = ExecutionTypeRef.FromClr(targetType) }, context)
            : RenderExpression(expression, context);
    }

    private static bool CanRenderBinaryAsNullableTarget(ExecutionBinary binary, Type targetType)
    {
        return Nullable.GetUnderlyingType(targetType) == binary.ReturnType.RequireClrType() &&
               IsNullableLiftableBinaryKind(binary.Kind);
    }

    private static bool IsNullableLiftableBinaryKind(BinaryOpKind kind)
    {
        return kind is BinaryOpKind.Add
            or BinaryOpKind.Subtract
            or BinaryOpKind.Multiply
            or BinaryOpKind.Divide
            or BinaryOpKind.Modulo
            or BinaryOpKind.BitwiseAnd
            or BinaryOpKind.BitwiseOr
            or BinaryOpKind.BitwiseXor
            or BinaryOpKind.LeftShift
            or BinaryOpKind.RightShift;
    }

    private bool TryCreateContextLayoutArguments(
        ExecutionContextLayout? contextLayout,
        int contextCount,
        ExecutionRenderContext context,
        out ExpressionSyntax[] arguments)
    {
        if (contextLayout == null ||
            contextLayout.Segments.Count == 0 ||
            contextLayout.Segments.Sum(static segment => segment.Count) != contextCount)
        {
            arguments = [];
            return false;
        }

        if (contextLayout.Segments.Count > 2 &&
            contextLayout.Segments.Any(static segment => segment.Kind != ExecutionContextSegmentKind.Single))
        {
            arguments = [];
            return false;
        }

        arguments = contextLayout.Segments
            .Select(segment => RenderContextSegmentArgument(segment, context))
            .ToArray();
        return true;
    }

    private ExpressionSyntax RenderContextSegmentArgument(
        ExecutionContextSegment segment,
        ExecutionRenderContext context)
    {
        return segment.Kind switch
        {
            ExecutionContextSegmentKind.Single => SyntaxFactory.CastExpression(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                RenderContextSegmentValue(segment.Value, context)),
            ExecutionContextSegmentKind.Array => RenderExpression(segment.Value, context),
            ExecutionContextSegmentKind.Row => RenderExpression(segment.Value, context),
            _ => throw UnsupportedShape.Of($"Execution context segment kind {segment.Kind}")
        };
    }

    private ExpressionSyntax RenderContextSegmentValue(
        ExecutionExpression value,
        ExecutionRenderContext context)
    {
        return value is ExecutionFieldRead { AccessStrategy: ContextAccess or GeneratedRowContextAccess } fieldRead
            ? RenderExpression(fieldRead with { ReturnType = ExecutionTypeRef.FromClr(typeof(object)) }, context)
            : RenderExpression(value, context);
    }

    private static ExpressionStatementSyntax CreateTableAddStatement(
        string tableName,
        ExpressionSyntax rowCreation,
        ExecutionAppendMode appendMode)
    {
        var addInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(tableName),
                    SyntaxFactory.IdentifierName(GetAppendMethodName(appendMode))))
            .WithArgumentList(CreateArgumentList(rowCreation));

        return SyntaxFactory.ExpressionStatement(addInvocation);
    }

    private static ExpressionStatementSyntax CreateRowBufferAddStatement(
        string bufferName,
        ExpressionSyntax rowCreation)
    {
        var addInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(bufferName),
                    SyntaxFactory.IdentifierName(nameof(List<Row>.Add))))
            .WithArgumentList(CreateArgumentList(rowCreation));

        return SyntaxFactory.ExpressionStatement(addInvocation);
    }

    private static string GetAppendMethodName(ExecutionAppendMode appendMode)
    {
        return appendMode switch
        {
            ExecutionAppendMode.Checked => nameof(Table.Add),
            ExecutionAppendMode.Unchecked => nameof(Table.AddUnchecked),
            ExecutionAppendMode.Direct => nameof(Table.AddDirect),
            _ => throw UnsupportedShape.Of($"Append mode {appendMode}")
        };
    }
}
