using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderMaterializeRecordListToFinalShapes(
        ExecutionMaterializeRecordListToTable materialize,
        ExecutionRenderContext context)
    {
        var statements = new List<StatementSyntax>();

        const string rowNumberVariableName = "rowNumber";
        var needsRowNumber = materialize.RenumberFieldIndexes.Count > 0;
        if (needsRowNumber)
        {
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                rowNumberVariableName,
                ExecutionCSharpRenderer.CreateIntLiteral(1)));
        }

        var recordVariableName = $"{materialize.Target.Name}Record";
        statements.Add(StatementEmitter.CreateForeach(
            recordVariableName,
            SyntaxFactory.IdentifierName(materialize.Source.Name),
            StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(
                CreateFinalShapeCreationFromRecord(materialize, recordVariableName, rowNumberVariableName, context),
                context))));

        return statements;
    }

    private ObjectCreationExpressionSyntax CreateFinalShapeCreationFromRecord(
        ExecutionMaterializeRecordListToTable materialize,
        string recordVariableName,
        string rowNumberVariableName,
        ExecutionRenderContext context)
    {
        var renumberIndexes = materialize.RenumberFieldIndexes.ToHashSet();
        var arguments = materialize.FieldIndexes.Select((sourceIndex, fieldIndex) =>
        {
            var value = renumberIndexes.Contains(sourceIndex)
                ? SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.PostIncrementExpression,
                    SyntaxFactory.IdentifierName(rowNumberVariableName))
                : ExecutionCSharpRenderer.CreateRecordPropertyRead(
                    recordVariableName,
                    materialize.RecordShape.Fields[sourceIndex],
                    materialize.RowShape.Fields[fieldIndex].Type.RequireClrType());

            return SyntaxFactory.Argument(value);
        });

        var sink = context.Session.FinalShapeYieldSink ??
                   throw new InvalidOperationException("Final shape sink is not active.");
        return CreateFinalShapeCreation(sink.ShapeTypeName, arguments);
    }
}
