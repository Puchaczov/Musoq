using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderUnionAllFinalShapeOperation(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(
                    CreateFinalShapeCreationFromSetRow(leftRowName, setOperation.Left, context),
                    context))),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateRowsRead(setOperation.Right, context),
                StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(
                    CreateFinalShapeCreationFromSetRow(rightRowName, setOperation.Right, context),
                    context)))
        ];
    }

    private List<StatementSyntax> RenderHashSetFinalShapeOperation(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var statements = new List<StatementSyntax>();
        statements.AddRange(setOperation.Kind switch
        {
            SetOpKind.Union => RenderHashSetUnionFinalShapeRows(setOperation, context),
            SetOpKind.Except => RenderHashSetExceptFinalShapeRows(setOperation, context),
            SetOpKind.Intersect => RenderHashSetIntersectFinalShapeRows(setOperation, context),
            _ => throw UnsupportedShape.Of($"Hash set set-operation rendering for {setOperation.Kind}")
        });

        return statements;
    }

    private IEnumerable<StatementSyntax> RenderGeneratedEqualityFinalShapeOperation(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        return setOperation.Kind switch
        {
            SetOpKind.Union => RenderGeneratedEqualityUnionFinalShapeRows(setOperation, context),
            SetOpKind.Except => RenderGeneratedEqualityExceptFinalShapeRows(setOperation, context),
            SetOpKind.Intersect => RenderGeneratedEqualityIntersectFinalShapeRows(setOperation, context),
            _ => throw UnsupportedShape.Of($"Generated equality set-operation rendering for {setOperation.Kind}")
        };
    }

    private IEnumerable<StatementSyntax> RenderGeneratedEqualityUnionFinalShapeRows(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var sink = context.Session.FinalShapeYieldSink ??
                   throw new InvalidOperationException("Final shape sink is not active.");
        var seenRowsName = $"{setOperation.Target.Name}GeneratedSetRows";
        var seenRowName = $"{setOperation.Target.Name}GeneratedSetRow";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var leftShapeName = $"{setOperation.Target.Name}LeftShape";
        var rightRowName = $"{setOperation.Target.Name}RightRow";
        var rightShapeName = $"{setOperation.Target.Name}RightShape";

        return
        [
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                seenRowsName,
                SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(sink.ShapeTypeName))
                    .WithArgumentList(CreateArgumentList(CreateCombinedRowsCountRead(setOperation.Left, setOperation.Right, context)))),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(
                    CreateLocalDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        leftShapeName,
                        CreateFinalShapeCreationFromSetRow(leftRowName, setOperation.Left, context)),
                    CreateListAddStatement(seenRowsName, SyntaxFactory.IdentifierName(leftShapeName)),
                    CreateFinalShapeOutputStatement(SyntaxFactory.IdentifierName(leftShapeName), context))),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateRowsRead(setOperation.Right, context),
                StatementEmitter.CreateBlock(
                    CreateLocalDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        rightShapeName,
                        CreateFinalShapeCreationFromSetRow(rightRowName, setOperation.Right, context)),
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.PrefixUnaryExpression(
                            SyntaxKind.LogicalNotExpression,
                            CreateFinalShapeRowsAnyMatch(
                                seenRowsName,
                                seenRowName,
                                rightShapeName,
                                setOperation,
                                context)),
                        StatementEmitter.CreateBlock(
                            CreateListAddStatement(seenRowsName, SyntaxFactory.IdentifierName(rightShapeName)),
                            CreateFinalShapeOutputStatement(SyntaxFactory.IdentifierName(rightShapeName), context)))))
        ];
    }

    private IEnumerable<StatementSyntax> RenderGeneratedEqualityExceptFinalShapeRows(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(SyntaxFactory.IfStatement(
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        CreateSetRowsAnyMatch(
                            CreateRowsRead(setOperation.Right, context),
                            rightRowName,
                            leftRowName,
                            setOperation.Right,
                            setOperation.Left,
                            setOperation,
                            context)),
                    StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(
                        CreateFinalShapeCreationFromSetRow(leftRowName, setOperation.Left, context),
                        context)))))
        ];
    }

    private IEnumerable<StatementSyntax> RenderGeneratedEqualityIntersectFinalShapeRows(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(SyntaxFactory.IfStatement(
                    CreateSetRowsAnyMatch(
                        CreateRowsRead(setOperation.Right, context),
                        rightRowName,
                        leftRowName,
                        setOperation.Right,
                        setOperation.Left,
                        setOperation,
                        context),
                    StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(
                        CreateFinalShapeCreationFromSetRow(leftRowName, setOperation.Left, context),
                        context)))))
        ];
    }

    private IEnumerable<StatementSyntax> RenderHashSetUnionFinalShapeRows(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var keysName = $"{setOperation.Target.Name}Keys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            ExecutionCSharpRenderer.CreateSetKeyHashSetDeclaration(
                keysName,
                setOperation.FieldTypes,
                CreateCombinedRowsCountRead(setOperation.Left, setOperation.Right, context)),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(
                    CreateHashSetAddStatement(keysName, leftRowName, setOperation, setOperation.Left, context),
                    CreateFinalShapeOutputStatement(CreateFinalShapeCreationFromSetRow(leftRowName, setOperation.Left, context), context))),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateRowsRead(setOperation.Right, context),
                StatementEmitter.CreateBlock(CreateConditionalSetRowOutput(
                    keysName,
                    rightRowName,
                    setOperation,
                    setOperation.Right,
                    ExecutionCSharpRenderer.SetKeyCondition.Added,
                    context)))
        ];
    }

    private IEnumerable<StatementSyntax> RenderHashSetExceptFinalShapeRows(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var rightKeysName = $"{setOperation.Target.Name}RightKeys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            ExecutionCSharpRenderer.CreateSetKeyHashSetDeclaration(
                rightKeysName,
                setOperation.FieldTypes,
                CreateRowsCountRead(setOperation.Right, context)),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateRowsRead(setOperation.Right, context),
                StatementEmitter.CreateBlock(CreateHashSetAddStatement(rightKeysName, rightRowName, setOperation, setOperation.Right, context))),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(CreateConditionalSetRowOutput(
                    rightKeysName,
                    leftRowName,
                    setOperation,
                    setOperation.Left,
                    ExecutionCSharpRenderer.SetKeyCondition.NotContained,
                    context)))
        ];
    }

    private IEnumerable<StatementSyntax> RenderHashSetIntersectFinalShapeRows(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var rightKeysName = $"{setOperation.Target.Name}RightKeys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            ExecutionCSharpRenderer.CreateSetKeyHashSetDeclaration(
                rightKeysName,
                setOperation.FieldTypes,
                CreateRowsCountRead(setOperation.Right, context)),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateRowsRead(setOperation.Right, context),
                StatementEmitter.CreateBlock(CreateHashSetAddStatement(rightKeysName, rightRowName, setOperation, setOperation.Right, context))),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(CreateConditionalSetRowOutput(
                    rightKeysName,
                    leftRowName,
                    setOperation,
                    setOperation.Left,
                    ExecutionCSharpRenderer.SetKeyCondition.Contained,
                    context)))
        ];
    }

    private IfStatementSyntax CreateConditionalSetRowOutput(
        string keysName,
        string rowName,
        ExecutionSetOperation setOperation,
        ExecutionVariable source,
        ExecutionCSharpRenderer.SetKeyCondition condition,
        ExecutionRenderContext context)
    {
        ExpressionSyntax conditionExpression = condition switch
        {
            ExecutionCSharpRenderer.SetKeyCondition.Added => CreateHashSetInvocation(keysName, nameof(HashSet<>.Add), rowName, setOperation, source, context),
            ExecutionCSharpRenderer.SetKeyCondition.Contained => CreateHashSetInvocation(keysName, nameof(HashSet<>.Contains), rowName, setOperation, source, context),
            ExecutionCSharpRenderer.SetKeyCondition.NotContained => SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                CreateHashSetInvocation(keysName, nameof(HashSet<>.Contains), rowName, setOperation, source, context)),
            _ => throw UnsupportedShape.Of($"Set key condition {condition}")
        };

        return SyntaxFactory.IfStatement(
            conditionExpression,
            StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(
                CreateFinalShapeCreationFromSetRow(rowName, source, context),
                context)));
    }

    private InvocationExpressionSyntax CreateFinalShapeRowsAnyMatch(
        string rowsName,
        string existingRowName,
        string candidateRowName,
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var lambda = SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(existingRowName)),
            CreateFinalShapeKeyEqualityExpression(existingRowName, candidateRowName, setOperation, context));

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(rowsName),
                    SyntaxFactory.IdentifierName(nameof(Enumerable.Any))))
            .WithArgumentList(CreateArgumentList(lambda));
    }

    private ExpressionSyntax CreateFinalShapeKeyEqualityExpression(
        string firstRowName,
        string secondRowName,
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        if (setOperation.FieldIndexes.Count == 0)
            return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);

        ExpressionSyntax? body = null;

        for (var index = 0; index < setOperation.FieldIndexes.Count; index++)
        {
            var fieldType = index < setOperation.FieldTypes.Count
                ? setOperation.FieldTypes[index]
                : typeof(object);
            var fieldIndex = setOperation.FieldIndexes[index];
            var equality = CreateSetFieldEquality(
                CreateFinalShapeFieldRead(firstRowName, fieldIndex, context),
                CreateFinalShapeFieldRead(secondRowName, fieldIndex, context),
                fieldType);
            body = body == null
                ? equality
                : SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, body, equality);
        }

        return body!;
    }

    private static ExpressionSyntax CreateFinalShapeFieldRead(
        string rowName,
        int fieldIndex,
        ExecutionRenderContext context)
    {
        var sink = context.Session.FinalShapeYieldSink ??
                   throw new InvalidOperationException("Final shape sink is not active.");
        if (fieldIndex < 0 || fieldIndex >= sink.Fields.Count)
            throw UnsupportedShape.Of($"Final shape set-operation field index {fieldIndex}");

        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(rowName),
            SyntaxFactory.IdentifierName(EscapeIdentifier(GetGeneratedFieldName(sink.Fields[fieldIndex]))));
    }

    private static ExpressionStatementSyntax CreateListAddStatement(
        string listName,
        ExpressionSyntax value)
    {
        return SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(listName),
                    SyntaxFactory.IdentifierName(nameof(List<object>.Add))))
            .WithArgumentList(CreateArgumentList(value)));
    }

    private ObjectCreationExpressionSyntax CreateFinalShapeCreationFromSetRow(
        string rowVariableName,
        ExecutionVariable source,
        ExecutionRenderContext context)
    {
        if (!TryGetTypedRowBufferShape(source.Name, context, out var sourceShape))
            return CreateFinalShapeCreationFromRow(rowVariableName, context);

        var sink = context.Session.FinalShapeYieldSink ??
                   throw new InvalidOperationException("Final shape sink is not active.");
        var arguments = sink.Fields
            .Select((field, index) => SyntaxFactory.Argument(
                CreateTypedSetFieldRead(rowVariableName, index, field.Type, sourceShape)))
            .ToArray();

        return CreateFinalShapeCreation(sink.ShapeTypeName, arguments);
    }
}
