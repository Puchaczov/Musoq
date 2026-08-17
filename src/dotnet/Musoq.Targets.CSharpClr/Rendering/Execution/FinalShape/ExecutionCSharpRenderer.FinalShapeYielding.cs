using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool IsFinalShapeTarget(ExecutionVariable target, ExecutionRenderContext context)
    {
        return context.Session.FinalShapeYieldSink is { } sink &&
               string.Equals(target.Name, sink.TableName, StringComparison.Ordinal);
    }

    private static bool TryGetFinalShapeSourceBuffer(
        string tableName,
        ExecutionRenderContext context,
        out FinalShapeSourceBuffer buffer)
    {
        if (context.Session.FinalShapeYieldSink?.SourceBuffers != null &&
            context.Session.FinalShapeYieldSink.SourceBuffers.TryGetValue(tableName, out buffer!))
        {
            return true;
        }

        buffer = null!;
        return false;
    }

    private List<StatementSyntax> RenderFinalShapeRowsFromRowsExpression(
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        ExecutionRenderContext context,
        IReadOnlyList<int>? fieldIndexes = null,
        IReadOnlyList<int>? renumberFieldIndexes = null)
    {
        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), rowsVariableName, rowsExpression)
        };
        statements.AddRange(CreateFinalShapeRenumberCounterDeclarations(rowsVariableName, renumberFieldIndexes));
        statements.Add(CreateFinalShapeRowsLoop(
            rowsVariableName,
            $"{rowsVariableName}Row",
            context,
            fieldIndexes,
            renumberFieldIndexes));
        return statements;
    }

    private List<StatementSyntax> RenderFinalShapeRowsFromShapeRowsExpression(
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        ExecutionRenderContext context,
        IReadOnlyList<int>? renumberFieldIndexes = null)
    {
        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), rowsVariableName, rowsExpression)
        };
        statements.AddRange(CreateFinalShapeRenumberCounterDeclarations(rowsVariableName, renumberFieldIndexes));
        statements.Add(CreateFinalShapeShapeRowsLoop(
            rowsVariableName,
            $"{rowsVariableName}Row",
            context,
            renumberFieldIndexes));
        return statements;
    }

    private ForEachStatementSyntax CreateFinalShapeRowsLoop(
        string rowsVariableName,
        string rowVariableName,
        ExecutionRenderContext context,
        IReadOnlyList<int>? fieldIndexes = null,
        IReadOnlyList<int>? renumberFieldIndexes = null)
    {
        return StatementEmitter.CreateForeach(
            rowVariableName,
            SyntaxFactory.IdentifierName(rowsVariableName),
            StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(
                CreateFinalShapeCreationFromRow(
                    rowVariableName,
                    context,
                    fieldIndexes,
                    rowsVariableName,
                    renumberFieldIndexes),
                context)));
    }

    private ForEachStatementSyntax CreateFinalShapeShapeRowsLoop(
        string rowsVariableName,
        string rowVariableName,
        ExecutionRenderContext context,
        IReadOnlyList<int>? renumberFieldIndexes = null)
    {
        return StatementEmitter.CreateForeach(
            rowVariableName,
            SyntaxFactory.IdentifierName(rowsVariableName),
            StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(
                renumberFieldIndexes is { Count: > 0 }
                    ? CreateFinalShapeCreationFromShapeRow(rowVariableName, rowsVariableName, renumberFieldIndexes, context)
                    : SyntaxFactory.IdentifierName(rowVariableName),
                context)));
    }

    private ObjectCreationExpressionSyntax CreateFinalShapeCreationFromRow(
        string rowVariableName,
        ExecutionRenderContext context,
        IReadOnlyList<int>? fieldIndexes = null,
        string? rowsVariableName = null,
        IReadOnlyList<int>? renumberFieldIndexes = null)
    {
        var sink = context.Session.FinalShapeYieldSink ??
                   throw new InvalidOperationException("Final shape sink is not active.");
        var renumberFieldIndexSet = CreateRenumberFieldIndexSet(renumberFieldIndexes);
        var arguments = Enumerable.Select<FieldBinding, ArgumentSyntax>(sink.Fields, (field, index) =>
        {
            if (renumberFieldIndexSet?.Contains(index) == true)
            {
                if (rowsVariableName == null)
                    throw new InvalidOperationException("Renumbered final shape rows require a row source variable name.");

                return SyntaxFactory.Argument(CreateFinalShapeRenumberRead(rowsVariableName, index));
            }

            var sourceIndex = fieldIndexes != null && index < fieldIndexes.Count
                ? fieldIndexes[index]
                : index;
            return SyntaxFactory.Argument(CreateFinalShapeRowValueRead(rowVariableName, sourceIndex, field.Type.RequireClrType()));
        });

        return CreateFinalShapeCreation(sink.ShapeTypeName, arguments);
    }

    private ObjectCreationExpressionSyntax CreateFinalShapeCreationFromShapeRow(
        string rowVariableName,
        string rowsVariableName,
        IReadOnlyList<int> renumberFieldIndexes,
        ExecutionRenderContext context)
    {
        var sink = context.Session.FinalShapeYieldSink ??
                   throw new InvalidOperationException("Final shape sink is not active.");
        var renumberFieldIndexSet = CreateRenumberFieldIndexSet(renumberFieldIndexes);
        var arguments = Enumerable.Select<FieldBinding, ArgumentSyntax>(sink.Fields, (field, index) => SyntaxFactory.Argument(
            renumberFieldIndexSet?.Contains(index) == true
                ? CreateFinalShapeRenumberRead(rowsVariableName, index)
                : SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(rowVariableName),
                    SyntaxFactory.IdentifierName(EscapeIdentifier(ExecutionCSharpRenderer.GetGeneratedFieldName(field))))));

        return CreateFinalShapeCreation(sink.ShapeTypeName, arguments);
    }

    private static IReadOnlyList<LocalDeclarationStatementSyntax> CreateFinalShapeRenumberCounterDeclarations(
        string rowsVariableName,
        IReadOnlyList<int>? renumberFieldIndexes)
    {
        if (renumberFieldIndexes is not { Count: > 0 })
            return [];

        return renumberFieldIndexes
            .Distinct()
            .Order()
            .Select(index => CreateLocalDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                CreateFinalShapeRenumberCounterName(rowsVariableName, index),
                ExecutionCSharpRenderer.CreateIntLiteral(1)))
            .ToArray();
    }

    private static HashSet<int>? CreateRenumberFieldIndexSet(IReadOnlyList<int>? renumberFieldIndexes)
    {
        return renumberFieldIndexes is { Count: > 0 }
            ? new HashSet<int>(renumberFieldIndexes)
            : null;
    }

    private static PostfixUnaryExpressionSyntax CreateFinalShapeRenumberRead(
        string rowsVariableName,
        int fieldIndex)
    {
        return SyntaxFactory.PostfixUnaryExpression(
            SyntaxKind.PostIncrementExpression,
            SyntaxFactory.IdentifierName(CreateFinalShapeRenumberCounterName(rowsVariableName, fieldIndex)));
    }

    private static string CreateFinalShapeRenumberCounterName(string rowsVariableName, int fieldIndex)
    {
        return $"{rowsVariableName}RowNumber{fieldIndex}";
    }

    private static ExpressionSyntax CreateFinalShapeRowValueRead(
        string rowVariableName,
        int sourceIndex,
        Type targetType)
    {
        var value = CreateElementAccess(
            SyntaxFactory.IdentifierName(rowVariableName),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(sourceIndex)));

        return targetType == typeof(object)
            ? value
            : SyntaxFactory.CastExpression(CreateTypeSyntax(targetType), value);
    }

    private static ObjectCreationExpressionSyntax CreateFinalShapeCreation(
        string shapeTypeName,
        IEnumerable<ArgumentSyntax> arguments)
    {
        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(shapeTypeName))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
    }

    private StatementSyntax CreateFinalShapeOutputStatement(
        ExpressionSyntax shapeCreation,
        ExecutionRenderContext context)
    {
        var sink = context.Session.FinalShapeYieldSink ??
                   throw new InvalidOperationException("Final shape sink is not active.");

        if (sink.BufferName == null)
        {
            return SyntaxFactory.YieldStatement(
                SyntaxKind.YieldReturnStatement,
                shapeCreation);
        }

        return SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName((string)sink.BufferName),
                    SyntaxFactory.IdentifierName(nameof(List<object>.Add))))
            .WithArgumentList(CreateArgumentList(shapeCreation)));
    }
}
