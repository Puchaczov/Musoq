using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderDistinctTable(ExecutionDistinctTable distinct)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper.ToDistinctTable))))
            .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(distinct.Source.Name)));

        if (TryRenderDistinctFinalShapeRows(distinct, invocation, out var finalShapeRows))
            return finalShapeRows;

        return
        [
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                distinct.Target.Name,
                invocation)
        ];
    }

    private List<StatementSyntax> RenderSortTable(ExecutionSortTable sort)
    {
        if (TryRenderSortFinalShapeRows(sort, out var finalShapeRows))
            return finalShapeRows;

        var rowsVariableName = $"{sort.Target.Name}Rows";
        var rowsExpression = TryGetGeneratedRowShape(sort.Source, out var rowShape)
            ? CreateOrderedRowsExpression(sort.Source, sort.Keys, rowShape)
            : CreateOrderedRowsExpression(sort.Source, sort.Keys);

        return RenderOrderedPostOperationRows(sort, rowsVariableName, rowsExpression, sort.RenumberFieldIndexes);
    }

    private List<StatementSyntax> RenderTopNTable(ExecutionTopNTable topN)
    {
        if (TryRenderTopNFinalShapeRows(topN, out var finalShapeRows))
            return finalShapeRows;

        var rowsVariableName = $"{topN.Target.Name}Rows";
        var orderedRowsExpression = TryGetGeneratedRowShape(topN.Source, out var rowShape)
            ? CreateOrderedRowsExpression(topN.Source, topN.Keys, rowShape)
            : CreateOrderedRowsExpression(topN.Source, topN.Keys);
        return RenderOrderedPostOperationRows(
            topN,
            rowsVariableName,
            CreateRowsMethodExpression(orderedRowsExpression, "Take", topN.Count),
            topN.RenumberFieldIndexes);
    }

    private List<StatementSyntax> RenderTopOffsetTable(ExecutionTopOffsetTable topOffset)
    {
        if (TryRenderTopOffsetFinalShapeRows(topOffset, out var finalShapeRows))
            return finalShapeRows;

        if (topOffset is { Strategy: ExecutionTopOffsetStrategy.BoundedHeap, AppendMode: ExecutionAppendMode.Direct } &&
            TryGetGeneratedRowShape(topOffset.Source, out var generatedRowShape) &&
            CanUseGeneratedRowTopOffset(topOffset, generatedRowShape))
        {
            return RenderGeneratedRowBoundedTopOffsetTable(topOffset, generatedRowShape);
        }

        if (topOffset is { Strategy: ExecutionTopOffsetStrategy.BoundedHeap, AppendMode: ExecutionAppendMode.Direct })
            return RenderBoundedTopOffsetTable(topOffset);

        return RenderTopOffsetTableWithRowsVariable(topOffset);
    }

    private List<StatementSyntax> RenderGeneratedRowBoundedTopOffsetTable(
        ExecutionTopOffsetTable topOffset,
        GeneratedRowShape rowShape)
    {
        var sourceRowsVariableName = $"{topOffset.Target.Name}SourceRows";
        var selectedRowsVariableName = $"{topOffset.Target.Name}Rows";
        var sourceRowVariableName = $"{topOffset.Target.Name}SourceRow";
        var comparerName = CreateGeneratedRowOrderComparerTypeName(rowShape, topOffset.Keys);
        var statements = new List<StatementSyntax>
        {
            SyntaxFactory.ParseStatement(
                $"var {sourceRowsVariableName} = new List<{rowShape.TypeName}>({topOffset.Source.Name}.Rows.Count);"),
            SyntaxFactory.ForEachStatement(
                SyntaxFactory.IdentifierName(rowShape.TypeName),
                sourceRowVariableName,
                CreateTableRowsRead(topOffset.Source.Name),
                StatementEmitter.CreateBlock(CreateInvocationStatement(
                        sourceRowsVariableName,
                        nameof(List<>.Add),
                        SyntaxFactory.IdentifierName(sourceRowVariableName)))),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                selectedRowsVariableName,
                CreateEvaluationHelperInvocation(
                    nameof(EvaluationHelper.SelectTopOffsetRecords),
                    SyntaxFactory.IdentifierName(sourceRowsVariableName),
                    CreateIntLiteral(topOffset.SkipCount),
                    CreateIntLiteral(topOffset.TakeCount),
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(comparerName),
                        SyntaxFactory.IdentifierName("Instance"))))
        };

        statements.AddRange(CreateTablePostOperationCopyStatements(
            GetTablePostOperation(topOffset),
            selectedRowsVariableName));
        statements.AddRange(topOffset.RenumberFieldIndexes.Select(index => CreateRenumberRowsStatement(topOffset.Target.Name, index)));
        return statements;
    }

    private List<StatementSyntax> RenderBoundedTopOffsetTable(ExecutionTopOffsetTable topOffset)
    {
        var statements = new List<StatementSyntax>();

        statements.AddRange(CreateDerivedTableStatements(
            topOffset.Target,
            topOffset.Source,
            topOffset.CapacityHint,
            topOffset.ColumnMetadata));
        statements.Add(CreateAppendBoundedTopOffsetRowsStatement(topOffset));
        statements.AddRange(topOffset.RenumberFieldIndexes.Select(index => CreateRenumberRowsStatement(topOffset.Target.Name, index)));
        return statements;
    }

    private List<StatementSyntax> RenderTopOffsetTableWithRowsVariable(ExecutionTopOffsetTable topOffset)
    {
        var rowsVariableName = $"{topOffset.Target.Name}Rows";
        var generatedRowShape = TryGetGeneratedRowShape(topOffset.Source, out var rowShape) ? rowShape : null;
        var rowsExpression = topOffset.Strategy == ExecutionTopOffsetStrategy.BoundedHeap
            ? CreateBoundedTopOffsetRowsExpression(topOffset)
            : CreateOrderedSliceRowsExpression(topOffset, generatedRowShape);
        return RenderOrderedPostOperationRows(topOffset, rowsVariableName, rowsExpression, topOffset.RenumberFieldIndexes);
    }

    private static ExpressionStatementSyntax CreateAppendBoundedTopOffsetRowsStatement(ExecutionTopOffsetTable topOffset)
    {
        return CreateInvocationStatement(
            nameof(EvaluationHelper),
            nameof(EvaluationHelper.AppendTopOffsetRowsDirect),
            CreateTableRowsRead(topOffset.Source.Name),
            SyntaxFactory.IdentifierName(topOffset.Target.Name),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(topOffset.SkipCount)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(topOffset.TakeCount)),
            CreateArrayCreation(nameof(RowOrderKey), topOffset.Keys.Select(CreateRowOrderKeyCreation)));
    }

    private InvocationExpressionSyntax CreateOrderedSliceRowsExpression(
        ExecutionTopOffsetTable topOffset,
        GeneratedRowShape? generatedRowShape)
    {
        return CreateRowsMethodExpression(
            CreateRowsMethodExpression(
                CreateOrderedRowsExpression(topOffset.Source, topOffset.Keys, generatedRowShape),
                "Skip",
                topOffset.SkipCount),
            "Take",
            topOffset.TakeCount);
    }

    private static InvocationExpressionSyntax CreateBoundedTopOffsetRowsExpression(ExecutionTopOffsetTable topOffset)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper.SelectTopOffsetRows))))
            .WithArgumentList(CreateArgumentList(
                CreateTableRowsRead(topOffset.Source.Name),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(topOffset.SkipCount)),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(topOffset.TakeCount)),
                CreateArrayCreation(nameof(RowOrderKey), topOffset.Keys.Select(CreateRowOrderKeyCreation))));
    }

    private static ObjectCreationExpressionSyntax CreateRowOrderKeyCreation(ExecutionOrderField key)
    {
        return CreateObjectCreation(
            nameof(RowOrderKey),
            CreateOrderKeySelector(key),
            CreateBooleanLiteral(key.Descending),
            CreateIntLiteral((int)key.NullOrdering));
    }

    private static BlockSyntax CreateRenumberRowsStatement(string tableName, int fieldIndex)
    {
        const string rowNumberVariableName = "rowNumber";
        const string rowVariableName = "rowToRenumber";
        var rowNumberDeclaration = CreateLocalDeclaration(
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
            rowNumberVariableName,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));
        var assignRowNumber = SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(rowVariableName),
                    SyntaxFactory.IdentifierName(nameof(Row.AssignValue))))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(fieldIndex)),
                SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.PostIncrementExpression,
                    SyntaxFactory.IdentifierName(rowNumberVariableName)))));

        return StatementEmitter.CreateBlock(
            rowNumberDeclaration,
            StatementEmitter.CreateForeach(
                rowVariableName,
                CreateTableRowsRead(tableName),
                StatementEmitter.CreateBlock(assignRowNumber)));
    }

    private IEnumerable<StatementSyntax> RenderSkipTable(ExecutionSkipTable skip)
    {
        return RenderTableSlice(skip, "Skip", skip.Count);
    }

    private IEnumerable<StatementSyntax> RenderTakeTable(ExecutionTakeTable take)
    {
        return RenderTableSlice(take, "Take", take.Count);
    }

    private IEnumerable<StatementSyntax> RenderSliceTable(ExecutionSliceTable slice)
    {
        var rowsVariableName = $"{slice.Target.Name}Rows";
        var rows = CreateRowsMethodExpression(
            CreateRowsMethodExpression(CreateTableRowsRead(slice.Source.Name), "Skip", slice.SkipCount),
            "Take",
            slice.TakeCount);

        var shapeRowsExpression = CreateRowsMethodExpression(
            CreateRowsMethodExpression(SyntaxFactory.IdentifierName(slice.Source.Name), "Skip", slice.SkipCount),
            "Take",
            slice.TakeCount);
        if (TryRenderShapeSliceFinalShapeSourceBuffer(slice.Source, slice.Target, rowsVariableName, shapeRowsExpression, out var shapeBufferRows))
            return shapeBufferRows;

        if (TryRenderShapeSliceFinalShapeRows(slice.Source, slice.Target, rowsVariableName, shapeRowsExpression, out var shapeRows))
            return shapeRows;

        if (TryRenderFinalShapeRows(slice.Target, rowsVariableName, rows, out var finalShapeRows))
            return finalShapeRows;

        return
        [
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), rowsVariableName, rows),
            ..CreateTablePostOperationCopyStatements(GetTablePostOperation(slice), rowsVariableName)
        ];
    }

    private IEnumerable<StatementSyntax> RenderTableSlice(
        ExecutionNode node,
        string methodName,
        int count)
    {
        var operation = GetTablePostOperation(node);
        var rowsVariableName = $"{operation.Target.Name}Rows";
        var rows = CreateRowsMethodExpression(CreateTableRowsRead(operation.Source.Name), methodName, count);

        var shapeRowsExpression = CreateRowsMethodExpression(SyntaxFactory.IdentifierName(operation.Source.Name), methodName, count);
        if (TryRenderShapeSliceFinalShapeSourceBuffer(operation.Source, operation.Target, rowsVariableName, shapeRowsExpression, out var shapeBufferRows))
            return shapeBufferRows;

        if (TryRenderShapeSliceFinalShapeRows(operation.Source, operation.Target, rowsVariableName, shapeRowsExpression, out var shapeRows))
            return shapeRows;

        if (TryRenderFinalShapeRows(operation.Target, rowsVariableName, rows, out var finalShapeRows))
            return finalShapeRows;

        return
        [
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), rowsVariableName, rows),
            ..CreateTablePostOperationCopyStatements(operation, rowsVariableName)
        ];
    }

    private List<StatementSyntax> RenderOrderedPostOperationRows(
        ExecutionNode node,
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        IReadOnlyList<int> renumberFieldIndexes)
    {
        var operation = GetTablePostOperation(node);
        if (TryRenderOrderedFinalShapeRows(
                operation,
                rowsVariableName,
                rowsExpression,
                renumberFieldIndexes,
                out var finalShapeRows))
            return finalShapeRows;

        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), rowsVariableName, rowsExpression)
        };

        statements.AddRange(CreateTablePostOperationCopyStatements(operation, rowsVariableName));
        statements.AddRange(renumberFieldIndexes.Select(index => CreateRenumberRowsStatement(operation.Target.Name, index)));
        return statements;
    }

    private static ExecutionTablePostOperationMetadata GetTablePostOperation(ExecutionNode node)
    {
        return ExecutionNodeFacts.TryGetTablePostOperation(node, out var operation)
            ? operation
            : throw new NotSupportedException($"Execution node {node.GetType().Name} is not a table post-operation.");
    }

    private static InvocationExpressionSyntax CreateRowsMethodExpression(
        ExpressionSyntax sourceRows,
        string methodName,
        int count)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    sourceRows,
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(CreateArgumentList(SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(count))));
    }
}
