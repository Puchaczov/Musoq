using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderCreateTable(
        ExecutionCreateTable createTable,
        ExecutionRenderContext context)
    {
        if (TryGetFinalShapeSourceBuffer(createTable.Table.Name, context, out var finalShapeBuffer))
        {
            yield return CreateFinalShapeSourceBufferDeclaration(createTable, finalShapeBuffer, context);
            yield break;
        }

        if (TryGetTypedRowBufferShape(createTable.Table.Name, context, out var rowShape))
        {
            yield return CreateTypedRowBufferDeclaration(createTable, rowShape, context);
            yield break;
        }

        var metadata = ResolveTableColumnMetadata(createTable);
        ExpressionSyntax columns = TryGetStaticMetadataFieldName(metadata, context, out var fieldName)
            ? SyntaxFactory.IdentifierName(fieldName)
            : CreateColumnArrayCreation(metadata.Fields);
        var tableCreation = CreateObjectCreation(
            "Table",
            CreateStringLiteral(createTable.Table.Name),
            columns);

        yield return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            createTable.Table.Name,
            tableCreation);

        if (createTable.CapacityHint is not null)
            yield return CreateEnsureCapacityStatement(createTable.Table.Name, RenderCapacityHint(createTable.CapacityHint, context));
    }

    private ExpressionStatementSyntax RenderEnsureTableCapacity(
        ExecutionEnsureTableCapacity ensureCapacity,
        ExecutionRenderContext context)
    {
        return CreateEnsureCapacityStatement(
            ensureCapacity.Table.Name,
            RenderCapacityHint(ensureCapacity.CapacityHint, context));
    }

    private LocalDeclarationStatementSyntax CreateTypedRowBufferDeclaration(
        ExecutionCreateTable createTable,
        GeneratedRowShape rowShape,
        ExecutionRenderContext context)
    {
        var listType = CreateListTypeSyntax(rowShape.TypeName);
        var arguments = createTable.CapacityHint == null
            ? SyntaxFactory.ArgumentList()
            : CreateArgumentList(RenderCapacityHint(createTable.CapacityHint, context));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            createTable.Table.Name,
            SyntaxFactory.ObjectCreationExpression(listType).WithArgumentList(arguments));
    }

    private LocalDeclarationStatementSyntax RenderCreateValuesRows(
        ExecutionCreateValuesRows valuesRows,
        ExecutionRenderContext context)
    {
        var rowCreations = valuesRows.Values
            .Select(row => CreateObjectCreation(
                valuesRows.RowShape.TypeName,
                row.Select((value, index) => RenderRowConstructorValue(
                    value.Value,
                    valuesRows.RowShape.Fields[index].Type,
                    context)).ToArray()))
            .Cast<ExpressionSyntax>()
            .ToArray();

        var arrayType = SyntaxFactory.ArrayType(SyntaxFactory.ParseTypeName(valuesRows.RowShape.TypeName))
            .WithRankSpecifiers(SyntaxFactory.SingletonList(
                SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.OmittedArraySizeExpression()))));
        var arrayCreation = SyntaxFactory.ArrayCreationExpression(arrayType)
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList(rowCreations)));

        return CreateLocalDeclaration(
            CreateVariableTypeSyntax(valuesRows.Rows),
            valuesRows.Rows.Name,
            arrayCreation);
    }
}
