using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private bool TryRenderDistinctFinalShapeRows(
        ExecutionDistinctTable distinct,
        ExpressionSyntax distinctTableExpression,
        out IEnumerable<StatementSyntax> statements)
    {
        if (TryGetFinalShapeSourceBuffer(distinct.Target.Name, out _) &&
            TryGetFinalShapeSourceBuffer(distinct.Source.Name, out _))
        {
            statements = RenderFinalShapeSourceBufferFromShapeRowsExpression(
                distinct.Target.Name,
                $"{distinct.Target.Name}Rows",
                CreateFinalShapeDistinctRowsExpression(distinct.Source.Name));
            return true;
        }

        if (!IsFinalShapeTarget(distinct.Target))
        {
            statements = Array.Empty<StatementSyntax>();
            return false;
        }

        if (TryGetFinalShapeSourceBuffer(distinct.Source.Name, out _))
        {
            statements = RenderFinalShapeRowsFromShapeRowsExpression(
                $"{distinct.Target.Name}Rows",
                CreateFinalShapeDistinctRowsExpression(distinct.Source.Name));
            return true;
        }

        statements = RenderFinalShapeRowsFromRowsExpression(
            $"{distinct.Target.Name}Rows",
            CreateTableRowsReadExpression(distinctTableExpression));
        return true;
    }

    private bool TryRenderTopOffsetFinalShapeRows(
        ExecutionTopOffsetTable topOffset,
        out List<StatementSyntax> statements)
    {
        if (!IsFinalShapeTarget(topOffset.Target))
        {
            statements = [];
            return false;
        }

        if (TryGetFinalShapeSourceBuffer(topOffset.Target.Name, out _) &&
            TryGetFinalShapeSourceBuffer(topOffset.Source.Name, out _) &&
            TryCreateFinalShapeTopOffsetRowsExpression(topOffset, out var bufferedShapeRowsExpression))
        {
            statements = RenderFinalShapeSourceBufferFromShapeRowsExpression(
                topOffset.Target.Name,
                $"{topOffset.Target.Name}Rows",
                bufferedShapeRowsExpression,
                topOffset.RenumberFieldIndexes);
            return true;
        }

        if (TryGetFinalShapeSourceBuffer(topOffset.Source.Name, out _) &&
            TryCreateFinalShapeTopOffsetRowsExpression(topOffset, out var shapeRowsExpression))
        {
            statements = RenderFinalShapeRowsFromShapeRowsExpression(
                $"{topOffset.Target.Name}Rows",
                shapeRowsExpression,
                topOffset.RenumberFieldIndexes);
            return true;
        }

        var rowsExpression = topOffset.Strategy == ExecutionTopOffsetStrategy.BoundedHeap
            ? ExecutionCSharpRenderer.CreateBoundedTopOffsetRowsExpression(topOffset)
            : CreateOrderedSliceRowsExpression(
                topOffset,
                TryGetGeneratedRowShape(topOffset.Source, out var rowShape) ? rowShape : null);
        statements = RenderFinalShapeRowsFromRowsExpression(
            $"{topOffset.Target.Name}Rows",
            rowsExpression,
            renumberFieldIndexes: topOffset.RenumberFieldIndexes);
        return true;
    }

    private bool TryRenderSortFinalShapeRows(
        ExecutionSortTable sort,
        out List<StatementSyntax> statements)
    {
        if (TryGetFinalShapeSourceBuffer(sort.Target.Name, out _) &&
            TryGetFinalShapeSourceBuffer(sort.Source.Name, out _) &&
            TryCreateFinalShapeOrderedRowsExpression(sort.Source.Name, sort.Keys, out var bufferedRowsExpression))
        {
            statements = RenderFinalShapeSourceBufferFromShapeRowsExpression(
                sort.Target.Name,
                $"{sort.Target.Name}Rows",
                bufferedRowsExpression,
                sort.RenumberFieldIndexes);
            return true;
        }

        if (!IsFinalShapeTarget(sort.Target))
        {
            statements = [];
            return false;
        }

        if (!TryGetFinalShapeSourceBuffer(sort.Source.Name, out _) ||
            !TryCreateFinalShapeOrderedRowsExpression(sort.Source.Name, sort.Keys, out var rowsExpression))
        {
            statements = [];
            return false;
        }

        statements = RenderFinalShapeRowsFromShapeRowsExpression(
            $"{sort.Target.Name}Rows",
            rowsExpression,
            sort.RenumberFieldIndexes);
        return true;
    }

    private bool TryRenderTopNFinalShapeRows(
        ExecutionTopNTable topN,
        out List<StatementSyntax> statements)
    {
        if (TryGetFinalShapeSourceBuffer(topN.Target.Name, out _) &&
            TryGetFinalShapeSourceBuffer(topN.Source.Name, out _) &&
            TryCreateFinalShapeOrderedRowsExpression(topN.Source.Name, topN.Keys, out var bufferedOrderedRows))
        {
            statements = RenderFinalShapeSourceBufferFromShapeRowsExpression(
                topN.Target.Name,
                $"{topN.Target.Name}Rows",
                ExecutionCSharpRenderer.CreateRowsMethodExpression(bufferedOrderedRows, "Take", topN.Count),
                topN.RenumberFieldIndexes);
            return true;
        }

        if (!IsFinalShapeTarget(topN.Target))
        {
            statements = [];
            return false;
        }

        if (!TryGetFinalShapeSourceBuffer(topN.Source.Name, out _) ||
            !TryCreateFinalShapeOrderedRowsExpression(topN.Source.Name, topN.Keys, out var orderedRows))
        {
            statements = [];
            return false;
        }

        statements = RenderFinalShapeRowsFromShapeRowsExpression(
            $"{topN.Target.Name}Rows",
            ExecutionCSharpRenderer.CreateRowsMethodExpression(orderedRows, "Take", topN.Count),
            topN.RenumberFieldIndexes);
        return true;
    }

    private bool TryRenderShapeSliceFinalShapeRows(
        ExecutionVariable source,
        ExecutionVariable target,
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        out IEnumerable<StatementSyntax> statements)
    {
        if (!IsFinalShapeTarget(target) || !TryGetFinalShapeSourceBuffer(source.Name, out _))
        {
            statements = Array.Empty<StatementSyntax>();
            return false;
        }

        statements = RenderFinalShapeRowsFromShapeRowsExpression(rowsVariableName, rowsExpression);
        return true;
    }

    private bool TryRenderShapeSliceFinalShapeSourceBuffer(
        ExecutionVariable source,
        ExecutionVariable target,
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        out IEnumerable<StatementSyntax> statements)
    {
        if (!TryGetFinalShapeSourceBuffer(target.Name, out _) ||
            !TryGetFinalShapeSourceBuffer(source.Name, out _))
        {
            statements = Array.Empty<StatementSyntax>();
            return false;
        }

        statements = RenderFinalShapeSourceBufferFromShapeRowsExpression(target.Name, rowsVariableName, rowsExpression);
        return true;
    }

    private bool TryRenderFinalShapeRows(
        ExecutionVariable target,
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        out IEnumerable<StatementSyntax> statements)
    {
        if (!IsFinalShapeTarget(target))
        {
            statements = Array.Empty<StatementSyntax>();
            return false;
        }

        statements = RenderFinalShapeRowsFromRowsExpression(rowsVariableName, rowsExpression);
        return true;
    }

    private List<StatementSyntax> RenderFinalShapeSourceBufferFromShapeRowsExpression(
        string targetName,
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        IReadOnlyList<int>? renumberFieldIndexes = null)
    {
        var sink = RenderSession.FinalShapeYieldSink ??
                   throw new InvalidOperationException("Final shape sink is not active.");
        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), rowsVariableName, rowsExpression),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                targetName,
                SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(sink.ShapeTypeName))
                    .WithArgumentList(SyntaxFactory.ArgumentList()))
        };
        statements.AddRange(CreateFinalShapeRenumberCounterDeclarations(rowsVariableName, renumberFieldIndexes));
        statements.Add(CreateFinalShapeSourceBufferRowsLoop(
            targetName,
            rowsVariableName,
            $"{rowsVariableName}Row",
            renumberFieldIndexes));
        return statements;
    }

    private ForEachStatementSyntax CreateFinalShapeSourceBufferRowsLoop(
        string targetName,
        string rowsVariableName,
        string rowVariableName,
        IReadOnlyList<int>? renumberFieldIndexes = null)
    {
        return StatementEmitter.CreateForeach(
            rowVariableName,
            SyntaxFactory.IdentifierName(rowsVariableName),
            StatementEmitter.CreateBlock(CreateRowBufferAddStatement(
                targetName,
                renumberFieldIndexes is { Count: > 0 }
                    ? CreateFinalShapeCreationFromShapeRow(rowVariableName, rowsVariableName, renumberFieldIndexes)
                    : SyntaxFactory.IdentifierName(rowVariableName))));
    }

    private ExpressionSyntax CreateFinalShapeDistinctRowsExpression(string sourceRowsName)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(sourceRowsName),
                    SyntaxFactory.IdentifierName("DistinctBy")))
            .WithArgumentList(CreateArgumentList(CreateFinalShapeDistinctKeySelector()));
    }

    private SimpleLambdaExpressionSyntax CreateFinalShapeDistinctKeySelector()
    {
        const string rowName = "__musoqDistinctRow";
        var sink = RenderSession.FinalShapeYieldSink ??
                   throw new InvalidOperationException("Final shape sink is not active.");
        ExpressionSyntax keyExpression = sink.Fields.Count switch
        {
            0 => ExecutionCSharpRenderer.CreateIntLiteral(0),
            1 => CreateFinalShapeFieldRead(rowName, sink.Fields[0]),
            _ => SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(
                sink.Fields.Select(field => SyntaxFactory.Argument(CreateFinalShapeFieldRead(rowName, field)))))
        };

        return SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier(rowName)))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithExpressionBody(keyExpression);
    }

    private static MemberAccessExpressionSyntax CreateFinalShapeFieldRead(string rowName, FieldBinding field)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(rowName),
            SyntaxFactory.IdentifierName(EscapeIdentifier(ExecutionCSharpRenderer.GetGeneratedFieldName(field))));
    }

    private bool TryRenderOrderedFinalShapeRows(
        ExecutionTablePostOperationMetadata operation,
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        IReadOnlyList<int> renumberFieldIndexes,
        out List<StatementSyntax> statements)
    {
        if (!IsFinalShapeTarget(operation.Target))
        {
            statements = [];
            return false;
        }

        statements = RenderFinalShapeRowsFromRowsExpression(
            rowsVariableName,
            rowsExpression,
            renumberFieldIndexes: renumberFieldIndexes);
        return true;
    }

    private bool TryCreateFinalShapeTopOffsetRowsExpression(
        ExecutionTopOffsetTable topOffset,
        out ExpressionSyntax rowsExpression)
    {
        if (!TryCreateFinalShapeComparerExpression(topOffset.Keys, out var comparer))
        {
            rowsExpression = null!;
            return false;
        }

        if (topOffset.Strategy == ExecutionTopOffsetStrategy.BoundedHeap)
        {
            rowsExpression = ExecutionCSharpRenderer.CreateEvaluationHelperInvocation(
                nameof(EvaluationHelper.SelectTopOffsetRecords),
                SyntaxFactory.IdentifierName(topOffset.Source.Name),
                ExecutionCSharpRenderer.CreateIntLiteral(topOffset.SkipCount),
                ExecutionCSharpRenderer.CreateIntLiteral(topOffset.TakeCount),
                comparer);
            return true;
        }

        if (!TryCreateFinalShapeOrderedRowsExpression(topOffset.Source.Name, topOffset.Keys, out var orderedRows))
        {
            rowsExpression = null!;
            return false;
        }

        rowsExpression = ExecutionCSharpRenderer.CreateRowsMethodExpression(
            ExecutionCSharpRenderer.CreateRowsMethodExpression(orderedRows, "Skip", topOffset.SkipCount),
            "Take",
            topOffset.TakeCount);
        return true;
    }

    private bool TryCreateFinalShapeOrderedRowsExpression(
        string sourceRowsName,
        IReadOnlyList<ExecutionOrderField> keys,
        out ExpressionSyntax rowsExpression)
    {
        if (!TryCreateFinalShapeComparerExpression(keys, out var comparer))
        {
            rowsExpression = null!;
            return false;
        }

        rowsExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(sourceRowsName),
                    SyntaxFactory.IdentifierName("OrderBy")))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.SimpleLambdaExpression(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("__musoqOrderRow")))
                    .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                    .WithExpressionBody(SyntaxFactory.IdentifierName("__musoqOrderRow")),
                comparer));
        return true;
    }

    private bool TryCreateFinalShapeComparerExpression(
        IReadOnlyList<ExecutionOrderField> keys,
        out ExpressionSyntax comparer)
    {
        var sink = RenderSession.FinalShapeYieldSink;
        if (sink == null ||
            keys.Count == 0 ||
            keys.Any(key => key.OutputIndex < 0 || key.OutputIndex >= sink.Fields.Count))
        {
            comparer = null!;
            return false;
        }

        var body = new List<string>
        {
            $"Comparer<{sink.ShapeTypeName}>.Create((left, right) =>",
            "{"
        };

        for (var index = 0; index < keys.Count; index++)
        {
            var key = keys[index];
            ExecutionCSharpRenderer.AddOrderRecordComparisonStatements(body, index, key, sink.Fields[key.OutputIndex]);
        }

        body.Add("        return 0;");
        body.Add("})");
        comparer = SyntaxFactory.ParseExpression(string.Join(Environment.NewLine, body));
        return true;
    }

    private static MemberAccessExpressionSyntax CreateTableRowsReadExpression(ExpressionSyntax tableExpression)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            tableExpression,
            SyntaxFactory.IdentifierName("Rows"));
    }
}
