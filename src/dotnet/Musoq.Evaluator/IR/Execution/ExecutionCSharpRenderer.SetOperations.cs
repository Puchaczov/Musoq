using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderSetOperation(ExecutionSetOperation setOperation)
    {
        if (setOperation.Kind == SetOpKind.UnionAll)
            return RenderUnionAllAppendOperation(setOperation);

        if (setOperation.Strategy == ExecutionSetOperationStrategy.HashSet)
            return RenderHashSetSetOperation(setOperation);

        return RenderRowComparerSetOperation(setOperation);
    }

    private IEnumerable<StatementSyntax> RenderUnionAllAppendOperation(ExecutionSetOperation setOperation)
    {
        if (IsFinalShapeTarget(setOperation.Target))
            return RenderUnionAllFinalShapeOperation(setOperation);

        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            CreateSetOperationResultBufferDeclaration(
                setOperation,
                CreateCombinedRowsCountRead(setOperation.Left, setOperation.Right)),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left),
                StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(
                    setOperation,
                    leftRowName,
                    setOperation.Left))),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateSetRowsRead(setOperation.Right),
                StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(
                    setOperation,
                    rightRowName,
                    setOperation.Right)))
        ];
    }

    private IEnumerable<StatementSyntax> RenderRowComparerSetOperation(ExecutionSetOperation setOperation)
    {
        var invocation = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(ResolveSetOperationMethodName(setOperation.Kind)))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.IdentifierName(setOperation.Left.Name),
                SyntaxFactory.IdentifierName(setOperation.Right.Name),
                CreateSetComparer(setOperation.FieldIndexes, setOperation.FieldTypes)));

        if (IsFinalShapeTarget(setOperation.Target))
            return RenderFinalShapeRowsFromRowsExpression(
                $"{setOperation.Target.Name}Rows",
                CreateTableRowsReadExpression(invocation));

        return
        [
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                setOperation.Target.Name,
                invocation)
        ];
    }

    private List<StatementSyntax> RenderHashSetSetOperation(ExecutionSetOperation setOperation)
    {
        if (IsFinalShapeTarget(setOperation.Target))
            return RenderHashSetFinalShapeOperation(setOperation);

        var statements = new List<StatementSyntax>
        {
            CreateSetOperationResultBufferDeclaration(setOperation)
        };

        statements.AddRange(setOperation.Kind switch
        {
            SetOpKind.Union => RenderHashSetUnion(setOperation),
            SetOpKind.Except => RenderHashSetExcept(setOperation),
            SetOpKind.Intersect => RenderHashSetIntersect(setOperation),
            _ => throw new NotSupportedException(
                $"Hash set set-operation rendering does not support {setOperation.Kind}.")
        });

        return statements;
    }

    private IEnumerable<StatementSyntax> RenderHashSetUnion(ExecutionSetOperation setOperation)
    {
        var keysName = $"{setOperation.Target.Name}Keys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            CreateSetKeyHashSetDeclaration(
                keysName,
                setOperation.FieldTypes,
                CreateCombinedRowsCountRead(setOperation.Left, setOperation.Right)),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left),
                StatementEmitter.CreateBlock(
                    CreateHashSetAddStatement(keysName, leftRowName, setOperation, setOperation.Left),
                    CreateSetOperationTargetAddStatement(setOperation, leftRowName, setOperation.Left))),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateSetRowsRead(setOperation.Right),
                StatementEmitter.CreateBlock(CreateConditionalSetRowAppend(
                    keysName,
                    rightRowName,
                    setOperation,
                    setOperation.Right,
                    SetKeyCondition.Added)))
        ];
    }

    private IEnumerable<StatementSyntax> RenderHashSetExcept(ExecutionSetOperation setOperation)
    {
        var rightKeysName = $"{setOperation.Target.Name}RightKeys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            CreateSetKeyHashSetDeclaration(
                rightKeysName,
                setOperation.FieldTypes,
                CreateRowsCountRead(setOperation.Right)),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateSetRowsRead(setOperation.Right),
                StatementEmitter.CreateBlock(CreateHashSetAddStatement(rightKeysName, rightRowName, setOperation, setOperation.Right))),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left),
                StatementEmitter.CreateBlock(CreateConditionalSetRowAppend(
                    rightKeysName,
                    leftRowName,
                    setOperation,
                    setOperation.Left,
                    SetKeyCondition.NotContained)))
        ];
    }

    private IEnumerable<StatementSyntax> RenderHashSetIntersect(ExecutionSetOperation setOperation)
    {
        var rightKeysName = $"{setOperation.Target.Name}RightKeys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            CreateSetKeyHashSetDeclaration(
                rightKeysName,
                setOperation.FieldTypes,
                CreateRowsCountRead(setOperation.Right)),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateSetRowsRead(setOperation.Right),
                StatementEmitter.CreateBlock(CreateHashSetAddStatement(rightKeysName, rightRowName, setOperation, setOperation.Right))),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateSetRowsRead(setOperation.Left),
                StatementEmitter.CreateBlock(CreateConditionalSetRowAppend(
                    rightKeysName,
                    leftRowName,
                    setOperation,
                    setOperation.Left,
                    SetKeyCondition.Contained)))
        ];
    }

    private LocalDeclarationStatementSyntax CreateSetOperationResultBufferDeclaration(
        ExecutionSetOperation setOperation,
        ExpressionSyntax? capacity = null)
    {
        if (TryGetTypedRowBufferShape(setOperation.Target.Name, out var rowShape))
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
                CreateColumnArrayCopy(setOperation.Left)));
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
        SetKeyCondition condition)
    {
        ExpressionSyntax conditionExpression = condition switch
        {
            SetKeyCondition.Added => CreateHashSetInvocation(keysName, nameof(HashSet<>.Add), rowName, setOperation, source),
            SetKeyCondition.Contained => CreateHashSetInvocation(keysName, nameof(HashSet<>.Contains), rowName, setOperation, source),
            SetKeyCondition.NotContained => SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                CreateHashSetInvocation(keysName, nameof(HashSet<>.Contains), rowName, setOperation, source)),
            _ => throw UnsupportedShape.Of($"Set key condition {condition}")
        };

        return SyntaxFactory.IfStatement(
            conditionExpression,
            StatementEmitter.CreateBlock(CreateSetOperationTargetAddStatement(setOperation, rowName, source)));
    }

    private ExpressionStatementSyntax CreateHashSetAddStatement(
        string keysName,
        string rowName,
        ExecutionSetOperation setOperation,
        ExecutionVariable source)
    {
        return SyntaxFactory.ExpressionStatement(CreateHashSetInvocation(
            keysName,
            nameof(HashSet<>.Add),
            rowName,
            setOperation,
            source));
    }

    private InvocationExpressionSyntax CreateHashSetInvocation(
        string keysName,
        string methodName,
        string rowName,
        ExecutionSetOperation setOperation,
        ExecutionVariable source)
    {
        return CreateInvocationExpression(
            keysName,
            methodName,
            CreateSetKeyExpression(rowName, source, setOperation.FieldIndexes, setOperation.FieldTypes));
    }

    private ExpressionSyntax CreateSetKeyExpression(
        string rowName,
        ExecutionVariable source,
        IReadOnlyList<int> fieldIndexes,
        IReadOnlyList<Type> fieldTypes)
    {
        var sourceShape = TryGetTypedRowBufferShape(source.Name, out var typedSourceShape)
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

    private ExpressionSyntax CreateSetRowsRead(ExecutionVariable source)
    {
        return TryGetTypedRowBufferShape(source.Name, out _)
            ? SyntaxFactory.IdentifierName(source.Name)
            : CreateTableRowsRead(source.Name);
    }

    private ExpressionSyntax CreateCombinedRowsCountRead(ExecutionVariable left, ExecutionVariable right)
    {
        return SyntaxFactory.BinaryExpression(
            SyntaxKind.AddExpression,
            CreateRowsCountRead(left),
            CreateRowsCountRead(right));
    }

    private ExpressionStatementSyntax CreateSetOperationTargetAddStatement(
        ExecutionSetOperation setOperation,
        string rowName,
        ExecutionVariable source)
    {
        var rowExpression = CreateSetOperationTargetRowExpression(setOperation, rowName, source);
        return TryGetTypedRowBufferShape(setOperation.Target.Name, out _)
            ? CreateRowBufferAddStatement(setOperation.Target.Name, rowExpression)
            : CreateTableAddStatement(setOperation.Target.Name, rowExpression, ExecutionAppendMode.Direct);
    }

    private ExpressionSyntax CreateSetOperationTargetRowExpression(
        ExecutionSetOperation setOperation,
        string rowName,
        ExecutionVariable source)
    {
        if (!TryGetTypedRowBufferShape(setOperation.Target.Name, out var targetShape))
            return SyntaxFactory.IdentifierName(rowName);

        var sourceShape = TryGetTypedRowBufferShape(source.Name, out var typedSourceShape)
            ? typedSourceShape
            : null;

        if (sourceShape != null &&
            string.Equals(sourceShape.TypeName, targetShape.TypeName, StringComparison.Ordinal))
        {
            return SyntaxFactory.IdentifierName(rowName);
        }

        var arguments = targetShape.Fields
            .Select((field, index) => CreateTypedSetFieldRead(rowName, index, field.Type, sourceShape))
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
