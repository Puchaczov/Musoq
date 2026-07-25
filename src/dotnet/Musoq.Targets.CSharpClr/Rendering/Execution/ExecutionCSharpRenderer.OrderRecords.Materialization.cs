using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderMaterializeRecordListToTable(
        ExecutionMaterializeRecordListToTable materialize,
        ExecutionRenderContext context)
    {
        if (IsFinalShapeTarget(materialize.Target, context))
            return RenderMaterializeRecordListToFinalShapes(materialize, context);

        var statements = new List<StatementSyntax>();
        statements.AddRange(RenderCreateTable(new ExecutionCreateTable(
            materialize.Target,
            materialize.RowShape,
            materialize.CapacityHint), context));

        const string rowNumberVariableName = "rowNumber";
        var needsRowNumber = materialize.RenumberFieldIndexes.Count > 0;
        if (needsRowNumber)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                rowNumberVariableName,
                CreateIntLiteral(1)));
        }

        var recordVariableName = $"{materialize.Target.Name}Record";
        var renumberIndexes = materialize.RenumberFieldIndexes.ToHashSet();
        var rowValues = materialize.FieldIndexes
            .Select((sourceIndex, fieldIndex) => renumberIndexes.Contains(sourceIndex)
                ? SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.PostIncrementExpression,
                    SyntaxFactory.IdentifierName(rowNumberVariableName))
                : CreateRecordPropertyRead(
                    recordVariableName,
                    materialize.RecordShape.Fields[sourceIndex],
                    materialize.RowShape.Fields[fieldIndex].Type.RequireClrType()))
            .ToArray();
        var rowCreation = CreateObjectCreation(materialize.RowShape.TypeName, rowValues);
        var addRow = CreateTableAddStatement(materialize.Target.Name, rowCreation, materialize.AppendMode);

        statements.Add(StatementEmitter.CreateForeach(
            recordVariableName,
            SyntaxFactory.IdentifierName(materialize.Source.Name),
            StatementEmitter.CreateBlock(addRow)));

        return statements;
    }

    private static ExpressionSyntax CreateRecordPropertyRead(
        string recordVariableName,
        FieldBinding field,
        Type targetType)
    {
        var value = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(recordVariableName),
            CreateIdentifierName(GetGeneratedFieldName(field)));

        if (field.Type.RequireClrType() == targetType || targetType == typeof(object))
            return value;

        return SyntaxFactory.CastExpression(CreateTypeSyntax(targetType), value);
    }
}
