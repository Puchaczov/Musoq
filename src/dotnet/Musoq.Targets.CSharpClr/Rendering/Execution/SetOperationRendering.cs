using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.Tables;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderSetOperation(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        if (setOperation.Kind == SetOpKind.UnionAll)
            return RenderUnionAllAppendOperation(setOperation, context);

        if (setOperation.Strategy == ExecutionSetOperationStrategy.HashSet)
            return RenderHashSetSetOperation(setOperation, context);

        return RenderGeneratedEqualitySetOperation(setOperation, context);
    }

    private IEnumerable<StatementSyntax> RenderUnionAllAppendOperation(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        if (IsFinalShapeTarget(setOperation.Target, context))
            return RenderUnionAllFinalShapeOperation(setOperation, context);

        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            CreateSetOperationResultBufferDeclaration(
                setOperation,
                CreateCombinedRowsCountRead(setOperation.Left, setOperation.Right, context),
                context),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(
                    setOperation,
                    leftRowName,
                    setOperation.Left,
                    context))),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateSetRowsRead(setOperation.Right, context),
                StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(
                    setOperation,
                    rightRowName,
                    setOperation.Right,
                    context)))
        ];
    }

    private List<StatementSyntax> RenderHashSetSetOperation(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        if (IsFinalShapeTarget(setOperation.Target, context))
            return RenderHashSetFinalShapeOperation(setOperation, context);

        var statements = new List<StatementSyntax>
        {
            CreateSetOperationResultBufferDeclaration(setOperation, context: context)
        };

        statements.AddRange(setOperation.Kind switch
        {
            SetOpKind.Union => RenderHashSetUnion(setOperation, context),
            SetOpKind.Except => RenderHashSetExcept(setOperation, context),
            SetOpKind.Intersect => RenderHashSetIntersect(setOperation, context),
            _ => throw new NotSupportedException(
                $"Hash set set-operation rendering does not support {setOperation.Kind}.")
        });

        return statements;
    }

    private IEnumerable<StatementSyntax> RenderHashSetUnion(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var keysName = $"{setOperation.Target.Name}Keys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            CreateSetKeyHashSetDeclaration(
                keysName,
                setOperation.FieldTypes.RequireClrTypes(),
                CreateCombinedRowsCountRead(setOperation.Left, setOperation.Right, context)),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(
                    CreateHashSetAddStatement(keysName, leftRowName, setOperation, setOperation.Left, context),
                    CreateSetOperationTargetAddStatement(setOperation, leftRowName, setOperation.Left, context))),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateSetRowsRead(setOperation.Right, context),
                StatementEmitter.CreateBlock(CreateConditionalSetRowAppend(
                    keysName,
                    rightRowName,
                    setOperation,
                    setOperation.Right,
                    SetKeyCondition.Added,
                    context)))
        ];
    }

    private IEnumerable<StatementSyntax> RenderHashSetExcept(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var rightKeysName = $"{setOperation.Target.Name}RightKeys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            CreateSetKeyHashSetDeclaration(
                rightKeysName,
                setOperation.FieldTypes.RequireClrTypes(),
                CreateRowsCountRead(setOperation.Right, context)),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateSetRowsRead(setOperation.Right, context),
                StatementEmitter.CreateBlock(CreateHashSetAddStatement(rightKeysName, rightRowName, setOperation, setOperation.Right, context))),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(CreateConditionalSetRowAppend(
                    rightKeysName,
                    leftRowName,
                    setOperation,
                    setOperation.Left,
                    SetKeyCondition.NotContained,
                    context)))
        ];
    }

    private IEnumerable<StatementSyntax> RenderHashSetIntersect(
        ExecutionSetOperation setOperation,
        ExecutionRenderContext context)
    {
        var rightKeysName = $"{setOperation.Target.Name}RightKeys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            CreateSetKeyHashSetDeclaration(
                rightKeysName,
                setOperation.FieldTypes.RequireClrTypes(),
                CreateRowsCountRead(setOperation.Right, context)),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateSetRowsRead(setOperation.Right, context),
                StatementEmitter.CreateBlock(CreateHashSetAddStatement(rightKeysName, rightRowName, setOperation, setOperation.Right, context))),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left, context),
                StatementEmitter.CreateBlock(CreateConditionalSetRowAppend(
                    rightKeysName,
                    leftRowName,
                    setOperation,
                    setOperation.Left,
                    SetKeyCondition.Contained,
                    context)))
        ];
    }

    private LocalDeclarationStatementSyntax CreateSetOperationResultBufferDeclaration(
        ExecutionSetOperation setOperation,
        ExpressionSyntax? capacity = null,
        ExecutionRenderContext? context = null)
    {
        var renderContext = context ?? CreateIsolatedRenderContext();
        if (TryGetTypedRowBufferShape(setOperation.Target.Name, renderContext, out var rowShape))
        {
            var argumentList = capacity == null
                ? SyntaxFactory.ArgumentList()
                : CreateArgumentList(capacity);

            return CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                setOperation.Target.Name,
                SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(rowShape.TypeName))
                    .WithArgumentList(argumentList));
        }

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            setOperation.Target.Name,
            CreateObjectCreation(
                nameof(Table),
                CreateStringLiteral(setOperation.Target.Name),
                CreateColumnArrayForSource(setOperation.Left, renderContext)));
    }

    private static LocalDeclarationStatementSyntax CreateSetKeyHashSetDeclaration(
        string variableName,
        IReadOnlyList<Type> fieldTypes,
        ExpressionSyntax? capacity)
    {
        return GeneratedIndexSyntaxFactory.CreateIndexDeclaration(
            variableName,
            SetOperationKeySyntaxFactory.CreateHashSetTypeSyntax(fieldTypes),
            capacity);
    }

    private IfStatementSyntax CreateConditionalSetRowAppend(
        string keysName,
        string rowName,
        ExecutionSetOperation setOperation,
        ExecutionVariable source,
        SetKeyCondition condition,
        ExecutionRenderContext context)
    {
        ExpressionSyntax conditionExpression = condition switch
        {
            SetKeyCondition.Added => CreateHashSetInvocation(keysName, nameof(HashSet<>.Add), rowName, setOperation, source, context),
            SetKeyCondition.Contained => CreateHashSetInvocation(keysName, nameof(HashSet<>.Contains), rowName, setOperation, source, context),
            SetKeyCondition.NotContained => SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                CreateHashSetInvocation(keysName, nameof(HashSet<>.Contains), rowName, setOperation, source, context)),
            _ => throw UnsupportedShape.Of($"Set key condition {condition}")
        };

        return SyntaxFactory.IfStatement(
            conditionExpression,
            StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(setOperation, rowName, source, context)));
    }

    private ExpressionStatementSyntax CreateHashSetAddStatement(
        string keysName,
        string rowName,
        ExecutionSetOperation setOperation,
        ExecutionVariable source,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.ExpressionStatement(CreateHashSetInvocation(
            keysName,
            nameof(HashSet<>.Add),
            rowName,
            setOperation,
            source,
            context));
    }

    private InvocationExpressionSyntax CreateHashSetInvocation(
        string keysName,
        string methodName,
        string rowName,
        ExecutionSetOperation setOperation,
        ExecutionVariable source,
        ExecutionRenderContext context)
    {
        return CreateInvocationExpression(
            keysName,
            methodName,
            CreateSetKeyExpression(rowName, source, setOperation.FieldIndexes, setOperation.FieldTypes.RequireClrTypes(), context));
    }

    private ExpressionSyntax CreateSetKeyExpression(
        string rowName,
        ExecutionVariable source,
        IReadOnlyList<int> fieldIndexes,
        IReadOnlyList<Type> fieldTypes,
        ExecutionRenderContext context)
    {
        var sourceShape = TryGetTypedRowBufferShape(source.Name, context, out var typedSourceShape)
            ? typedSourceShape
            : null;

        if (fieldIndexes.Count == 1)
            return CreateTypedSetFieldRead(rowName, fieldIndexes[0], fieldTypes[0], sourceShape);

        return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(
            fieldIndexes.Select((fieldIndex, index) => SyntaxFactory.Argument(
                CreateTypedSetFieldRead(rowName, fieldIndex, fieldTypes[index], sourceShape)))));
    }

    private static ExpressionSyntax CreateTypedSetFieldRead(
        string rowName,
        int fieldIndex,
        Type fieldType,
        GeneratedRowShape? sourceShape)
    {
        ExpressionSyntax read;
        if (sourceShape != null && fieldIndex >= 0 && fieldIndex < sourceShape.Fields.Count)
        {
            var sourceField = sourceShape.Fields[fieldIndex];
            read = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(rowName),
                CreateIdentifierName(GetGeneratedFieldName(sourceField)));
        }
        else
        {
            read = CreateElementAccess(
                SyntaxFactory.IdentifierName(rowName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(fieldIndex)));
        }

        return fieldType == typeof(object)
            ? read
            : SyntaxFactory.CastExpression(CreateTypeSyntax(fieldType), read);
    }

    private ExpressionSyntax CreateSetRowsRead(ExecutionVariable source, ExecutionRenderContext context)
    {
        return TryGetTypedRowBufferShape(source.Name, context, out _)
            ? SyntaxFactory.IdentifierName(source.Name)
            : CreateTableRowsRead(source.Name);
    }

    private ExpressionSyntax CreateCombinedRowsCountRead(
        ExecutionVariable left,
        ExecutionVariable right,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.BinaryExpression(
            SyntaxKind.AddExpression,
            CreateRowsCountRead(left, context),
            CreateRowsCountRead(right, context));
    }

    private ExpressionStatementSyntax CreateSetOperationTargetAddStatement(
        ExecutionSetOperation setOperation,
        string rowName,
        ExecutionVariable source,
        ExecutionRenderContext context)
    {
        var rowExpression = CreateSetOperationTargetRowExpression(setOperation, rowName, source, context);
        return TryGetTypedRowBufferShape(setOperation.Target.Name, context, out _)
            ? CreateRowBufferAddStatement(setOperation.Target.Name, rowExpression)
            : CreateTableAddStatement(setOperation.Target.Name, rowExpression, ExecutionAppendMode.Direct);
    }

    private ExpressionSyntax CreateSetOperationTargetRowExpression(
        ExecutionSetOperation setOperation,
        string rowName,
        ExecutionVariable source,
        ExecutionRenderContext context)
    {
        if (!TryGetTypedRowBufferShape(setOperation.Target.Name, context, out var targetShape))
            return SyntaxFactory.IdentifierName(rowName);

        var sourceShape = TryGetTypedRowBufferShape(source.Name, context, out var typedSourceShape)
            ? typedSourceShape
            : null;

        if (sourceShape != null &&
            string.Equals(sourceShape.TypeName, targetShape.TypeName, StringComparison.Ordinal))
        {
            return SyntaxFactory.IdentifierName(rowName);
        }

        var arguments = targetShape.Fields
            .Select((field, index) => CreateTypedSetFieldRead(rowName, index, field.Type.RequireClrType(), sourceShape))
            .Cast<ExpressionSyntax>()
            .ToArray();

        return CreateObjectCreation(targetShape.TypeName, arguments);
    }

    private enum SetKeyCondition
    {
        Added,
        Contained,
        NotContained
    }
}
