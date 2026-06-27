using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private StatementSyntax RenderAppendRow(ExecutionAppendRow appendRow)
    {
        appendRow = NormalizeLazyContextSegments(appendRow);

        if (_finalShapeYieldSink is { } finalShapeYieldSink &&
            string.Equals(appendRow.Table.Name, finalShapeYieldSink.TableName, StringComparison.Ordinal))
        {
            return CreateFinalShapeOutputStatement(CreateFinalShapeCreation(finalShapeYieldSink.ShapeTypeName, appendRow));
        }

        if (TryGetFinalShapeSourceBuffer(appendRow.Table.Name, out var finalShapeSourceBuffer))
            return CreateRowBufferAddStatement(
                appendRow.Table.Name,
                CreateFinalShapeCreation(finalShapeSourceBuffer.ShapeTypeName, appendRow));

        if (TryGetTypedRowBufferShape(appendRow.Table.Name, out _))
            return CreateRowBufferAddStatement(appendRow.Table.Name, CreateGeneratedRowCreation(appendRow));

        return CreateTableAddStatement(
            appendRow.Table.Name,
            CreateGeneratedRowCreation(appendRow),
            appendRow.AppendMode);
    }

    private StatementSyntax RenderAppendExistingRow(ExecutionAppendExistingRow appendRow)
    {
        if (_finalShapeYieldSink is { } finalShapeYieldSink &&
            string.Equals(appendRow.Table.Name, finalShapeYieldSink.TableName, StringComparison.Ordinal))
        {
            return CreateFinalShapeOutputStatement(CreateFinalShapeCreationFromRow(appendRow.Row.Name));
        }

        if (TryGetFinalShapeSourceBuffer(appendRow.Table.Name, out _))
            return CreateRowBufferAddStatement(
                appendRow.Table.Name,
                CreateFinalShapeCreationFromRow(appendRow.Row.Name));

        if (TryGetTypedRowBufferShape(appendRow.Table.Name, out _))
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
        return CreateGeneratedRowCreation(
            appendRow.RowShape,
            appendRow.Values,
            appendRow.Contexts,
            appendRow.ContextLayout);
    }

    private ExpressionSyntax RenderRowConstructorValue(
        ExecutionExpression expression,
        Type targetType)
    {
        return expression is ExecutionBinary binary &&
               RequiresNullableTemporalSubtraction(binary) &&
               CanBeNull(targetType)
            ? RenderNullableTemporalSubtractionValue(binary)
            : expression is ExecutionBinary nullableBinary &&
              CanRenderBinaryAsNullableTarget(nullableBinary, targetType)
                ? RenderExpression(nullableBinary with { ReturnType = targetType })
            : RenderExpression(expression);
    }

    private static bool CanRenderBinaryAsNullableTarget(ExecutionBinary binary, Type targetType)
    {
        return Nullable.GetUnderlyingType(targetType) == binary.ReturnType &&
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
            .Select(RenderContextSegmentArgument)
            .ToArray();
        return true;
    }

    private ExpressionSyntax RenderContextSegmentArgument(ExecutionContextSegment segment)
    {
        return segment.Kind switch
        {
            ExecutionContextSegmentKind.Single => SyntaxFactory.CastExpression(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                RenderContextSegmentValue(segment.Value)),
            ExecutionContextSegmentKind.Array => RenderExpression(segment.Value),
            ExecutionContextSegmentKind.Row => RenderExpression(segment.Value),
            _ => throw UnsupportedShape.Of($"Execution context segment kind {segment.Kind}")
        };
    }

    private ExpressionSyntax RenderContextSegmentValue(ExecutionExpression value)
    {
        return value is ExecutionFieldRead { AccessStrategy: ContextAccess or GeneratedRowContextAccess } fieldRead
            ? RenderExpression(fieldRead with { ReturnType = typeof(object) })
            : RenderExpression(value);
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
