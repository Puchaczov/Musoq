using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderProjectTable(ExecutionProjectTable project)
    {
        var rowVariableName = $"{project.Target.Name}SourceRow";
        var rowValues = project.FieldIndexes
            .Select((index, fieldIndex) => CreateProjectedTableRowValue(
                rowVariableName,
                index,
                project.RowShape.Fields[fieldIndex].Type))
            .ToArray();
        var rowCreation = project.RowShape.Contexts.Count == 0 ||
                          !GeneratedRowTypeUsesContextConstructor(project.RowShape.TypeName)
            ? CreateObjectCreation(project.RowShape.TypeName, rowValues)
            : CreateObjectCreation(
                project.RowShape.TypeName,
                [
                    ..rowValues,
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(rowVariableName),
                        SyntaxFactory.IdentifierName("Contexts"))
                ]);

        return
        [
            ..RenderCreateTable(new ExecutionCreateTable(project.Target, project.RowShape, project.CapacityHint)),
            StatementEmitter.CreateForeach(
                rowVariableName,
                CreateTableRowsRead(project.Source.Name),
                StatementEmitter.CreateBlock(CreateTableAddStatement(project.Target.Name, rowCreation, project.AppendMode)))
        ];
    }

    private static ExpressionSyntax CreateProjectedTableRowValue(
        string rowVariableName,
        int sourceIndex,
        Type targetType)
    {
        var value = CreateElementAccess(
            SyntaxFactory.IdentifierName(rowVariableName),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(sourceIndex)));

        if (targetType == typeof(object))
            return value;

        return SyntaxFactory.CastExpression(CreateTypeSyntax(targetType), value);
    }

    private ExpressionStatementSyntax RenderStoreTable(ExecutionStoreTable store)
    {
        if (TryGetTypedStoredTableResult(store.TableIndex, out var typedResult) &&
            CanStoreTypedCteRows(store.Table, typedResult))
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    CreateCteRowResultSlotAccess(store.TableIndex),
                    SyntaxFactory.IdentifierName(store.Table.Name)));
        }

        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateElementAccess(
                    SyntaxFactory.IdentifierName("_tableResults"),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(store.TableIndex))),
                SyntaxFactory.IdentifierName(store.Table.Name)));
    }

    private bool CanStoreTypedCteRows(
        ExecutionVariable table,
        TypedStoredTableResult typedResult)
    {
        return TryGetTypedRowBufferShape(table.Name, out _) ||
               string.Equals(
                   table.GeneratedRowTypeName,
                   $"List<{typedResult.RowShape.TypeName}>",
                   StringComparison.Ordinal);
    }
}
