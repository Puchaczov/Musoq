using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderGeneratedEqualitySetOperation(ExecutionSetOperation setOperation)
    {
        if (IsFinalShapeTarget(setOperation.Target))
            return RenderGeneratedEqualityFinalShapeOperation(setOperation);

        var statements = new List<StatementSyntax>
        {
            CreateSetOperationResultBufferDeclaration(setOperation)
        };

        statements.AddRange(setOperation.Kind switch
        {
            SetOpKind.Union => RenderGeneratedEqualityUnion(setOperation),
            SetOpKind.Except => RenderGeneratedEqualityExcept(setOperation),
            SetOpKind.Intersect => RenderGeneratedEqualityIntersect(setOperation),
            _ => throw UnsupportedShape.Of($"Generated equality set-operation rendering for {setOperation.Kind}")
        });

        return statements;
    }

    private IEnumerable<StatementSyntax> RenderGeneratedEqualityUnion(ExecutionSetOperation setOperation)
    {
        var resultRowName = $"{setOperation.Target.Name}ResultRow";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left),
                StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(setOperation, leftRowName, setOperation.Left))),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateSetRowsRead(setOperation.Right),
                StatementEmitter.CreateBlock(SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        CreateSetRowsAnyMatch(
                            CreateSetRowsRead(setOperation.Target),
                            resultRowName,
                            rightRowName,
                            setOperation.Target,
                            setOperation.Right,
                            setOperation)),
                    StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(
                        setOperation,
                        rightRowName,
                        setOperation.Right)))))
        ];
    }

    private IEnumerable<StatementSyntax> RenderGeneratedEqualityExcept(ExecutionSetOperation setOperation)
    {
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left),
                StatementEmitter.CreateBlock(SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        CreateSetRowsAnyMatch(
                            CreateSetRowsRead(setOperation.Right),
                            rightRowName,
                            leftRowName,
                            setOperation.Right,
                            setOperation.Left,
                            setOperation)),
                    StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(
                        setOperation,
                        leftRowName,
                        setOperation.Left)))))
        ];
    }

    private IEnumerable<StatementSyntax> RenderGeneratedEqualityIntersect(ExecutionSetOperation setOperation)
    {
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left),
                StatementEmitter.CreateBlock(SyntaxFactory.IfStatement(
                    CreateSetRowsAnyMatch(
                        CreateSetRowsRead(setOperation.Right),
                        rightRowName,
                        leftRowName,
                        setOperation.Right,
                        setOperation.Left,
                        setOperation),
                    StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(
                        setOperation,
                        leftRowName,
                        setOperation.Left)))))
        ];
    }

    private InvocationExpressionSyntax CreateSetRowsAnyMatch(
        ExpressionSyntax rowsExpression,
        string existingRowName,
        string candidateRowName,
        ExecutionVariable existingSource,
        ExecutionVariable candidateSource,
        ExecutionSetOperation setOperation)
    {
        var lambda = SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(existingRowName)),
            CreateSetRowsEqualExpression(
                existingRowName,
                existingSource,
                candidateRowName,
                candidateSource,
                setOperation.FieldIndexes,
                setOperation.FieldTypes));

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    rowsExpression,
                    SyntaxFactory.IdentifierName(nameof(Enumerable.Any))))
            .WithArgumentList(CreateArgumentList(lambda));
    }

    private ExpressionSyntax CreateSetRowsEqualExpression(
        string firstRowName,
        ExecutionVariable firstSource,
        string secondRowName,
        ExecutionVariable secondSource,
        IReadOnlyList<int> fieldIndexes,
        IReadOnlyList<Type> fieldTypes)
    {
        if (fieldIndexes.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

        ExpressionSyntax? body = null;

        for (var index = 0; index < fieldIndexes.Count; index++)
        {
            var fieldType = index < fieldTypes.Count ? fieldTypes[index] : typeof(object);
            var equality = CreateSetFieldEquality(
                firstRowName,
                firstSource,
                secondRowName,
                secondSource,
                fieldIndexes[index],
                fieldType);
            body = body == null
                ? equality
                : SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, body, equality);
        }

        return body!;
    }

    private ExpressionSyntax CreateSetFieldEquality(
        string firstRowName,
        ExecutionVariable firstSource,
        string secondRowName,
        ExecutionVariable secondSource,
        int fieldIndex,
        Type fieldType)
    {
        var firstShape = TryGetTypedRowBufferShape(firstSource.Name, out var typedFirstShape)
            ? typedFirstShape
            : null;
        var secondShape = TryGetTypedRowBufferShape(secondSource.Name, out var typedSecondShape)
            ? typedSecondShape
            : null;

        return CreateSetFieldEquality(
            CreateTypedSetFieldRead(firstRowName, fieldIndex, fieldType, firstShape),
            CreateTypedSetFieldRead(secondRowName, fieldIndex, fieldType, secondShape),
            fieldType);
    }

    private static ExpressionSyntax CreateSetFieldEquality(
        ExpressionSyntax firstFieldAccess,
        ExpressionSyntax secondFieldAccess,
        Type fieldType)
    {
        if (fieldType != typeof(object))
        {
            return SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                firstFieldAccess,
                secondFieldAccess);
        }

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                    SyntaxFactory.IdentifierName(nameof(object.Equals))))
            .WithArgumentList(CreateArgumentList(firstFieldAccess, secondFieldAccess));
    }
}
