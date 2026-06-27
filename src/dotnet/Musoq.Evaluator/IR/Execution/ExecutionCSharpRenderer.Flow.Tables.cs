using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderCreateTable(ExecutionCreateTable createTable)
    {
        if (TryGetFinalShapeSourceBuffer(createTable.Table.Name, out var finalShapeBuffer))
        {
            yield return CreateFinalShapeSourceBufferDeclaration(createTable, finalShapeBuffer);
            yield break;
        }

        if (TryGetTypedRowBufferShape(createTable.Table.Name, out var rowShape))
        {
            yield return CreateTypedRowBufferDeclaration(createTable, rowShape);
            yield break;
        }

        var metadata = ResolveTableColumnMetadata(createTable);
        ExpressionSyntax columns = TryGetStaticMetadataFieldName(metadata, out var fieldName)
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
            yield return CreateEnsureCapacityStatement(createTable.Table.Name, RenderCapacityHint(createTable.CapacityHint));
    }

    private ExpressionStatementSyntax RenderEnsureTableCapacity(ExecutionEnsureTableCapacity ensureCapacity)
    {
        return CreateEnsureCapacityStatement(
            ensureCapacity.Table.Name,
            RenderCapacityHint(ensureCapacity.CapacityHint));
    }

    private LocalDeclarationStatementSyntax CreateTypedRowBufferDeclaration(
        ExecutionCreateTable createTable,
        GeneratedRowShape rowShape)
    {
        var listType = CreateListTypeSyntax(rowShape.TypeName);
        var arguments = createTable.CapacityHint == null
            ? SyntaxFactory.ArgumentList()
            : CreateArgumentList(RenderCapacityHint(createTable.CapacityHint));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            createTable.Table.Name,
            SyntaxFactory.ObjectCreationExpression(listType).WithArgumentList(arguments));
    }

    private LocalDeclarationStatementSyntax RenderCreateValuesRows(ExecutionCreateValuesRows valuesRows)
    {
        var rowCreations = valuesRows.Values
            .Select(row => CreateObjectCreation(
                valuesRows.RowShape.TypeName,
                row.Select((value, index) => RenderRowConstructorValue(
                    value.Value,
                    valuesRows.RowShape.Fields[index].Type)).ToArray()))
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
