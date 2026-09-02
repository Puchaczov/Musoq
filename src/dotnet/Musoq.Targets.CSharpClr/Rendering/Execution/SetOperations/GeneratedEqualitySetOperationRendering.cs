using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderGeneratedEqualitySetOperation(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        if (IsFinalShapeTarget(setOperation.Target, context))
            return RenderGeneratedEqualityFinalShapeOperation(setOperation, context);

        var statements = new List<StatementSyntax>
        {
            CreateSetOperationResultBufferDeclaration(setOperation, context: context)
        };

        statements.AddRange(setOperation.Kind switch
        {
            SetOpKind.Union => RenderGeneratedEqualityUnion(setOperation, context),
            SetOpKind.Except => RenderGeneratedEqualityExcept(setOperation, context),
            SetOpKind.Intersect => RenderGeneratedEqualityIntersect(setOperation, context),
            _ => throw UnsupportedShape.Of($"Generated equality set-operation rendering for {setOperation.Kind}")
        });

        return statements;
    }

    private IEnumerable<StatementSyntax> RenderGeneratedEqualityUnion(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var resultRowName = $"{setOperation.Target.Name}ResultRow";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        CreateSetRowsAnyMatch(
                            CreateSetRowsRead(setOperation.Target, context),
                            resultRowName,
                            leftRowName,
                            setOperation.Target,
                            setOperation.Left,
                            setOperation,
                            context)),
                    StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(
                        setOperation,
                        leftRowName,
                        setOperation.Left,
                        context))))),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateSetRowsRead(setOperation.Right, context),
                StatementEmitter.CreateBlock(SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        CreateSetRowsAnyMatch(
                            CreateSetRowsRead(setOperation.Target, context),
                            resultRowName,
                            rightRowName,
                            setOperation.Target,
                            setOperation.Right,
                            setOperation,
                            context)),
                    StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(
                        setOperation,
                        rightRowName,
                        setOperation.Right,
                        context)))))
        ];
    }

    private IEnumerable<StatementSyntax> RenderGeneratedEqualityExcept(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        CreateSetRowsAnyMatch(
                            CreateSetRowsRead(setOperation.Right, context),
                            rightRowName,
                            leftRowName,
                            setOperation.Right,
                            setOperation.Left,
                            setOperation,
                            context)),
                    StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(
                        setOperation,
                        leftRowName,
                        setOperation.Left,
                        context)))))
        ];
    }

    private IEnumerable<StatementSyntax> RenderGeneratedEqualityIntersect(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(SyntaxFactory.IfStatement(
                    CreateSetRowsAnyMatch(
                        CreateSetRowsRead(setOperation.Right, context),
                        rightRowName,
                        leftRowName,
                        setOperation.Right,
                        setOperation.Left,
                        setOperation,
                        context),
                    StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(
                        setOperation,
                        leftRowName,
                        setOperation.Left,
                        context)))))
        ];
    }

    private InvocationExpressionSyntax CreateSetRowsAnyMatch(
        ExpressionSyntax rowsExpression,
        string existingRowName,
        string candidateRowName,
        ExecutionVariable existingSource,
        ExecutionVariable candidateSource,
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var lambda = SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(existingRowName)),
            CreateSetRowsEqualExpression(
                existingRowName,
                existingSource,
                candidateRowName,
                candidateSource,
                setOperation.FieldIndexes,
                setOperation.FieldTypes.RequireClrTypes(),
                context));

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
        IReadOnlyList<Type> fieldTypes,
        ExecutionRenderContext context)
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
                fieldType,
                context);
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
        Type fieldType,
        ExecutionRenderContext context)
    {
        var firstShape = TryGetTypedRowBufferShape(firstSource.Name, context, out var typedFirstShape)
            ? typedFirstShape
            : null;
        var secondShape = TryGetTypedRowBufferShape(secondSource.Name, context, out var typedSecondShape)
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
