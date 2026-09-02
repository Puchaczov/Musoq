using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Execution.Facts;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private bool TryRenderDistinctFinalShapeRows(
        ExecutionDistinctTable distinct,
        ExpressionSyntax distinctTableExpression,
        ExecutionRenderContext context,
        out IEnumerable<StatementSyntax> statements)
    {
        if (TryGetFinalShapeSourceBuffer(distinct.Target.Name, context, out _) &&
            TryGetFinalShapeSourceBuffer(distinct.Source.Name, context, out _))
        {
            statements = RenderFinalShapeSourceBufferFromShapeRowsExpression(
                distinct.Target.Name,
                $"{distinct.Target.Name}Rows",
                CreateFinalShapeDistinctRowsExpression(distinct.Source.Name, context),
                context);
            return true;
        }

        if (!IsFinalShapeTarget(distinct.Target, context))
        {
            statements = Array.Empty<StatementSyntax>();
            return false;
        }

        if (TryGetFinalShapeSourceBuffer(distinct.Source.Name, context, out _))
        {
            statements = RenderFinalShapeRowsFromShapeRowsExpression(
                $"{distinct.Target.Name}Rows",
                CreateFinalShapeDistinctRowsExpression(distinct.Source.Name, context),
                context);
            return true;
        }

        statements = RenderFinalShapeRowsFromRowsExpression(
            $"{distinct.Target.Name}Rows",
            CreateTableRowsReadExpression(distinctTableExpression),
            context);
        return true;
    }

    private bool TryRenderTopOffsetFinalShapeRows(
        ExecutionTopOffsetTable topOffset,
        ExecutionRenderContext context,
        out List<StatementSyntax> statements)
    {
        if (!IsFinalShapeTarget(topOffset.Target, context))
        {
            statements = [];
            return false;
        }

        if (TryGetFinalShapeSourceBuffer(topOffset.Target.Name, context, out _) &&
            TryGetFinalShapeSourceBuffer(topOffset.Source.Name, context, out _) &&
            TryCreateFinalShapeTopOffsetRowsExpression(topOffset, context, out var bufferedShapeRowsExpression))
        {
            statements = RenderFinalShapeSourceBufferFromShapeRowsExpression(
                topOffset.Target.Name,
                $"{topOffset.Target.Name}Rows",
                bufferedShapeRowsExpression,
                context,
                topOffset.RenumberFieldIndexes);
            return true;
        }

        if (TryGetFinalShapeSourceBuffer(topOffset.Source.Name, context, out _) &&
            TryCreateFinalShapeTopOffsetRowsExpression(topOffset, context, out var shapeRowsExpression))
        {
            statements = RenderFinalShapeRowsFromShapeRowsExpression(
                $"{topOffset.Target.Name}Rows",
                shapeRowsExpression,
                context,
                topOffset.RenumberFieldIndexes);
            return true;
        }

        var rowsExpression = topOffset.Strategy == ExecutionTopOffsetStrategy.BoundedHeap
            ? CreateBoundedTopOffsetRowsExpression(topOffset)
            : CreateOrderedSliceRowsExpression(
                topOffset,
                TryGetGeneratedRowShape(topOffset.Source, context, out var rowShape) ? rowShape : null,
                context);
        statements = RenderFinalShapeRowsFromRowsExpression(
            $"{topOffset.Target.Name}Rows",
            rowsExpression,
            context,
            renumberFieldIndexes: topOffset.RenumberFieldIndexes);
        return true;
    }

    private bool TryRenderSortFinalShapeRows(
        ExecutionSortTable sort,
        ExecutionRenderContext context,
        out List<StatementSyntax> statements)
    {
        if (TryGetFinalShapeSourceBuffer(sort.Target.Name, context, out _) &&
            TryGetFinalShapeSourceBuffer(sort.Source.Name, context, out _) &&
            TryCreateFinalShapeOrderedRowsExpression(sort.Source.Name, sort.Keys, context, out var bufferedRowsExpression))
        {
            statements = RenderFinalShapeSourceBufferFromShapeRowsExpression(
                sort.Target.Name,
                $"{sort.Target.Name}Rows",
                bufferedRowsExpression,
                context,
                sort.RenumberFieldIndexes);
            return true;
        }

        if (!IsFinalShapeTarget(sort.Target, context))
        {
            statements = [];
            return false;
        }

        if (!TryGetFinalShapeSourceBuffer(sort.Source.Name, context, out _) ||
            !TryCreateFinalShapeOrderedRowsExpression(sort.Source.Name, sort.Keys, context, out var rowsExpression))
        {
            statements = [];
            return false;
        }

        statements = RenderFinalShapeRowsFromShapeRowsExpression(
            $"{sort.Target.Name}Rows",
            rowsExpression,
            context,
            sort.RenumberFieldIndexes);
        return true;
    }

    private bool TryRenderTopNFinalShapeRows(
        ExecutionTopNTable topN,
        ExecutionRenderContext context,
        out List<StatementSyntax> statements)
    {
        if (TryGetFinalShapeSourceBuffer(topN.Target.Name, context, out _) &&
            TryGetFinalShapeSourceBuffer(topN.Source.Name, context, out _) &&
            TryCreateFinalShapeOrderedRowsExpression(topN.Source.Name, topN.Keys, context, out var bufferedOrderedRows))
        {
            statements = RenderFinalShapeSourceBufferFromShapeRowsExpression(
                topN.Target.Name,
                $"{topN.Target.Name}Rows",
                CreateRowsMethodExpression(bufferedOrderedRows, "Take", topN.Count),
                context,
                topN.RenumberFieldIndexes);
            return true;
        }

        if (!IsFinalShapeTarget(topN.Target, context))
        {
            statements = [];
            return false;
        }

        if (!TryGetFinalShapeSourceBuffer(topN.Source.Name, context, out _) ||
            !TryCreateFinalShapeOrderedRowsExpression(topN.Source.Name, topN.Keys, context, out var orderedRows))
        {
            statements = [];
            return false;
        }

        statements = RenderFinalShapeRowsFromShapeRowsExpression(
            $"{topN.Target.Name}Rows",
            CreateRowsMethodExpression(orderedRows, "Take", topN.Count),
            context,
            topN.RenumberFieldIndexes);
        return true;
    }

    private bool TryRenderShapeSliceFinalShapeRows(
        ExecutionVariable source,
        ExecutionVariable target,
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        ExecutionRenderContext context,
        out IEnumerable<StatementSyntax> statements)
    {
        if (!IsFinalShapeTarget(target, context) || !TryGetFinalShapeSourceBuffer(source.Name, context, out _))
        {
            statements = Array.Empty<StatementSyntax>();
            return false;
        }

        statements = RenderFinalShapeRowsFromShapeRowsExpression(rowsVariableName, rowsExpression, context);
        return true;
    }

    private bool TryRenderShapeSliceFinalShapeSourceBuffer(
        ExecutionVariable source,
        ExecutionVariable target,
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        ExecutionRenderContext context,
        out IEnumerable<StatementSyntax> statements)
    {
        if (!TryGetFinalShapeSourceBuffer(target.Name, context, out _) ||
            !TryGetFinalShapeSourceBuffer(source.Name, context, out _))
        {
            statements = Array.Empty<StatementSyntax>();
            return false;
        }

        statements = RenderFinalShapeSourceBufferFromShapeRowsExpression(target.Name, rowsVariableName, rowsExpression, context);
        return true;
    }

    private bool TryRenderFinalShapeRows(
        ExecutionVariable target,
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        ExecutionRenderContext context,
        out IEnumerable<StatementSyntax> statements)
    {
        if (!IsFinalShapeTarget(target, context))
        {
            statements = Array.Empty<StatementSyntax>();
            return false;
        }

        statements = RenderFinalShapeRowsFromRowsExpression(rowsVariableName, rowsExpression, context);
        return true;
    }

    private List<StatementSyntax> RenderFinalShapeSourceBufferFromShapeRowsExpression(
        string targetName,
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        ExecutionRenderContext context,
        IReadOnlyList<int>? renumberFieldIndexes = null)
    {
        var sink = context.Session.FinalShapeYieldSink ??
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
            context,
            renumberFieldIndexes));
        return statements;
    }

    private ForEachStatementSyntax CreateFinalShapeSourceBufferRowsLoop(
        string targetName,
        string rowsVariableName,
        string rowVariableName,
        ExecutionRenderContext context,
        IReadOnlyList<int>? renumberFieldIndexes = null)
    {
        return StatementEmitter.CreateForeach(
            rowVariableName,
            SyntaxFactory.IdentifierName(rowsVariableName),
            StatementEmitter.CreateBlock(CreateRowBufferAddStatement(
                targetName,
                renumberFieldIndexes is { Count: > 0 }
                    ? CreateFinalShapeCreationFromShapeRow(rowVariableName, rowsVariableName, renumberFieldIndexes, context)
                    : SyntaxFactory.IdentifierName(rowVariableName))));
    }

    private ExpressionSyntax CreateFinalShapeDistinctRowsExpression(
        string sourceRowsName,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(sourceRowsName),
                    SyntaxFactory.IdentifierName("DistinctBy")))
            .WithArgumentList(CreateArgumentList(CreateFinalShapeDistinctKeySelector(context)));
    }

    private SimpleLambdaExpressionSyntax CreateFinalShapeDistinctKeySelector(ExecutionRenderContext context)
    {
        const string rowName = "__musoqDistinctRow";
        var sink = context.Session.FinalShapeYieldSink ??
                   throw new InvalidOperationException("Final shape sink is not active.");
        ExpressionSyntax keyExpression = sink.Fields.Count switch
        {
            0 => CreateIntLiteral(0),
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
            SyntaxFactory.IdentifierName(EscapeIdentifier(GetGeneratedFieldName(field))));
    }

    private bool TryRenderOrderedFinalShapeRows(
        ExecutionTablePostOperationMetadata operation,
        string rowsVariableName,
        ExpressionSyntax rowsExpression,
        IReadOnlyList<int> renumberFieldIndexes,
        ExecutionRenderContext context,
        out List<StatementSyntax> statements)
    {
        if (!IsFinalShapeTarget(operation.Target, context))
        {
            statements = [];
            return false;
        }

        statements = RenderFinalShapeRowsFromRowsExpression(
            rowsVariableName,
            rowsExpression,
            context,
            renumberFieldIndexes: renumberFieldIndexes);
        return true;
    }

    private bool TryCreateFinalShapeTopOffsetRowsExpression(
        ExecutionTopOffsetTable topOffset,
        ExecutionRenderContext context,
        out ExpressionSyntax rowsExpression)
    {
        if (!TryCreateFinalShapeComparerExpression(topOffset.Keys, context, out var comparer))
        {
            rowsExpression = null!;
            return false;
        }

        if (topOffset.Strategy == ExecutionTopOffsetStrategy.BoundedHeap)
        {
            rowsExpression = CreateEvaluationHelperInvocation(
                nameof(EvaluationHelper.SelectTopOffsetRecords),
                SyntaxFactory.IdentifierName(topOffset.Source.Name),
                CreateIntLiteral(topOffset.SkipCount),
                CreateIntLiteral(topOffset.TakeCount),
                comparer);
            return true;
        }

        if (!TryCreateFinalShapeOrderedRowsExpression(topOffset.Source.Name, topOffset.Keys, context, out var orderedRows))
        {
            rowsExpression = null!;
            return false;
        }

        rowsExpression = CreateRowsMethodExpression(
            CreateRowsMethodExpression(orderedRows, "Skip", topOffset.SkipCount),
            "Take",
            topOffset.TakeCount);
        return true;
    }

    private bool TryCreateFinalShapeOrderedRowsExpression(
        string sourceRowsName,
        IReadOnlyList<ExecutionOrderField> keys,
        ExecutionRenderContext context,
        out ExpressionSyntax rowsExpression)
    {
        if (!TryCreateFinalShapeComparerExpression(keys, context, out var comparer))
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
        ExecutionRenderContext context,
        out ExpressionSyntax comparer)
    {
        var sink = context.Session.FinalShapeYieldSink;
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
            AddOrderRecordComparisonStatements(body, index, key, sink.Fields[key.OutputIndex]);
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
