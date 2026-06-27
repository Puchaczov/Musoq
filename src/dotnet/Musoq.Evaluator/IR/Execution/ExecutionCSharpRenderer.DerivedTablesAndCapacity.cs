using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> CreateDerivedTableStatements(
        ExecutionVariable target,
        ExecutionVariable source,
        ExecutionCapacityHint? capacityHint,
        ExecutionColumnMetadata? columnMetadata)
    {
        yield return CreateDerivedTableDeclaration(target, source, columnMetadata);

        if (capacityHint is not null)
            yield return CreateEnsureCapacityStatement(target.Name, RenderCapacityHint(capacityHint));
    }

    private IEnumerable<StatementSyntax> CreateTablePostOperationCopyStatements(
        ExecutionTablePostOperationMetadata operation,
        string rowsVariableName)
    {
        foreach (var statement in CreateDerivedTableStatements(
                     operation.Target,
                     operation.Source,
                     operation.CapacityHint,
                     operation.ColumnMetadata))
        {
            yield return statement;
        }

        yield return CreateCopyRowsLoop(rowsVariableName, operation.Target.Name, operation.AppendMode);
    }

    private LocalDeclarationStatementSyntax CreateDerivedTableDeclaration(
        ExecutionVariable target,
        ExecutionVariable source,
        ExecutionColumnMetadata? columnMetadata)
    {
        if (TryGetTypedRowBufferShape(target.Name, out var rowShape))
        {
            return CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                target.Name,
                SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(rowShape.TypeName))
                    .WithArgumentList(SyntaxFactory.ArgumentList()));
        }

        ExpressionSyntax columns = columnMetadata is not null && TryGetStaticMetadataFieldName(columnMetadata, out var fieldName)
            ? SyntaxFactory.IdentifierName(fieldName)
            : CreateColumnArrayCopy(source);

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            target.Name,
            CreateObjectCreation(
                "Table",
                CreateStringLiteral(target.Name),
                columns));
    }

    private static InvocationExpressionSyntax CreateColumnArrayCopy(ExecutionVariable source)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(source.Name),
                    SyntaxFactory.IdentifierName("Columns")),
                SyntaxFactory.IdentifierName("ToArray")));
    }

    private ForEachStatementSyntax CreateCopyRowsLoop(
        string rowsVariableName,
        string tableName,
        ExecutionAppendMode appendMode)
    {
        const string rowVariableName = "copiedRow";

        return StatementEmitter.CreateForeach(
            rowVariableName,
            SyntaxFactory.IdentifierName(rowsVariableName),
            StatementEmitter.CreateBlock(
                TryGetTypedRowBufferShape(tableName, out _)
                    ? CreateRowBufferAddStatement(tableName, SyntaxFactory.IdentifierName(rowVariableName))
                    : CreateTableAddStatement(tableName, SyntaxFactory.IdentifierName(rowVariableName), appendMode)));
    }

    private static ExpressionStatementSyntax CreateEnsureCapacityStatement(
        string tableName,
        ExpressionSyntax capacity)
    {
        var ensureCapacityInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(tableName),
                    SyntaxFactory.IdentifierName(nameof(Table.EnsureCapacity))))
            .WithArgumentList(CreateArgumentList(capacity));

        return SyntaxFactory.ExpressionStatement(ensureCapacityInvocation);
    }

    private ExpressionSyntax RenderCapacityHint(ExecutionCapacityHint capacityHint)
    {
        return capacityHint switch
        {
            ExecutionConstantCapacityHint constant => SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(constant.Capacity)),
            ExecutionCollectionCountCapacityHint collection => CreateCollectionCountRead(collection.Collection.Name),
            ExecutionTryGetNonEnumeratedCountCapacityHint enumerable => CreateTryGetNonEnumeratedCountCapacityRead(
                enumerable.Collection.Name,
                enumerable.CountVariableName),
            ExecutionStoredTableCountCapacityHint storedTable => CreateStoredTableCountRead(storedTable.TableIndex),
            ExecutionTakeCapacityHint take => CreateMathInvocation(
                nameof(Math.Min),
                CreateCollectionCountRead(take.Collection.Name),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(take.Count))),
            ExecutionSkipCapacityHint skip => CreateMathInvocation(
                nameof(Math.Max),
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.SubtractExpression,
                    CreateCollectionCountRead(skip.Collection.Name),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(skip.Count))),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
            ExecutionSkipTakeCapacityHint skipTake => CreateMathInvocation(
                nameof(Math.Min),
                CreateMathInvocation(
                    nameof(Math.Max),
                    SyntaxFactory.BinaryExpression(
                        SyntaxKind.SubtractExpression,
                        CreateCollectionCountRead(skipTake.Collection.Name),
                        SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(skipTake.SkipCount))),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(skipTake.TakeCount))),
            _ => throw UnsupportedShape.Of($"Capacity hint {capacityHint.GetType().Name}")
        };
    }

    private static ConditionalExpressionSyntax CreateTryGetNonEnumeratedCountCapacityRead(string collectionName, string countVariableName)
    {
        return SyntaxFactory.ConditionalExpression(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(collectionName),
                        SyntaxFactory.IdentifierName(nameof(Enumerable.TryGetNonEnumeratedCount))))
                .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                            SyntaxFactory.DeclarationExpression(
                                SyntaxFactory.IdentifierName("var"),
                                SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(countVariableName))))
                        .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))))),
            SyntaxFactory.IdentifierName(countVariableName),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)));
    }

    private static MemberAccessExpressionSyntax CreateCollectionCountRead(string collectionName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(collectionName),
            SyntaxFactory.IdentifierName("Count"));
    }

    private ExpressionSyntax CreateStoredTableCountRead(int tableIndex)
    {
        if (TryGetTypedStoredTableResult(tableIndex, out _))
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                CreateCteRowResultSlotAccess(tableIndex),
                SyntaxFactory.IdentifierName("Count"));
        }

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            CreateStoredTableRead(tableIndex),
            SyntaxFactory.IdentifierName("Count"));
    }

    private static InvocationExpressionSyntax CreateMathInvocation(
        string methodName,
        params ExpressionSyntax[] arguments)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(Math)),
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(CreateArgumentList(arguments));
    }
}
