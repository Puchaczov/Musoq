using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderUnionAllFinalShapeOperation(ExecutionSetOperation setOperation)
    {
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateRowsRead(setOperation.Left),
                StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(
                    CreateFinalShapeCreationFromSetRow(leftRowName, setOperation.Left)))),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateRowsRead(setOperation.Right),
                StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(
                    CreateFinalShapeCreationFromSetRow(rightRowName, setOperation.Right))))
        ];
    }

    private List<StatementSyntax> RenderHashSetFinalShapeOperation(ExecutionSetOperation setOperation)
    {
        var statements = new List<StatementSyntax>();
        statements.AddRange(setOperation.Kind switch
        {
            SetOpKind.Union => RenderHashSetUnionFinalShapeRows(setOperation),
            SetOpKind.Except => RenderHashSetExceptFinalShapeRows(setOperation),
            SetOpKind.Intersect => RenderHashSetIntersectFinalShapeRows(setOperation),
            _ => throw UnsupportedShape.Of($"Hash set set-operation rendering for {setOperation.Kind}")
        });

        return statements;
    }

    private IEnumerable<StatementSyntax> RenderHashSetUnionFinalShapeRows(ExecutionSetOperation setOperation)
    {
        var keysName = $"{setOperation.Target.Name}Keys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            ExecutionCSharpRenderer.CreateSetKeyHashSetDeclaration(
                keysName,
                setOperation.FieldTypes,
                CreateCombinedRowsCountRead(setOperation.Left, setOperation.Right)),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateRowsRead(setOperation.Left),
                StatementEmitter.CreateBlock(
                    CreateHashSetAddStatement(keysName, leftRowName, setOperation, setOperation.Left),
                    CreateFinalShapeOutputStatement(CreateFinalShapeCreationFromSetRow(leftRowName, setOperation.Left)))),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateRowsRead(setOperation.Right),
                StatementEmitter.CreateBlock(CreateConditionalSetRowOutput(
                    keysName,
                    rightRowName,
                    setOperation,
                    setOperation.Right,
                    ExecutionCSharpRenderer.SetKeyCondition.Added)))
        ];
    }

    private IEnumerable<StatementSyntax> RenderHashSetExceptFinalShapeRows(ExecutionSetOperation setOperation)
    {
        var rightKeysName = $"{setOperation.Target.Name}RightKeys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            ExecutionCSharpRenderer.CreateSetKeyHashSetDeclaration(
                rightKeysName,
                setOperation.FieldTypes,
                CreateRowsCountRead(setOperation.Right)),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateRowsRead(setOperation.Right),
                StatementEmitter.CreateBlock(CreateHashSetAddStatement(rightKeysName, rightRowName, setOperation, setOperation.Right))),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateRowsRead(setOperation.Left),
                StatementEmitter.CreateBlock(CreateConditionalSetRowOutput(
                    rightKeysName,
                    leftRowName,
                    setOperation,
                    setOperation.Left,
                    ExecutionCSharpRenderer.SetKeyCondition.NotContained)))
        ];
    }

    private IEnumerable<StatementSyntax> RenderHashSetIntersectFinalShapeRows(ExecutionSetOperation setOperation)
    {
        var rightKeysName = $"{setOperation.Target.Name}RightKeys";
        var leftRowName = $"{setOperation.Target.Name}LeftRow";
        var rightRowName = $"{setOperation.Target.Name}RightRow";

        return
        [
            ExecutionCSharpRenderer.CreateSetKeyHashSetDeclaration(
                rightKeysName,
                setOperation.FieldTypes,
                CreateRowsCountRead(setOperation.Right)),
            StatementEmitter.CreateForeach(
                rightRowName,
                CreateRowsRead(setOperation.Right),
                StatementEmitter.CreateBlock(CreateHashSetAddStatement(rightKeysName, rightRowName, setOperation, setOperation.Right))),
            StatementEmitter.CreateForeach(
                leftRowName,
                CreateRowsRead(setOperation.Left),
                StatementEmitter.CreateBlock(CreateConditionalSetRowOutput(
                    rightKeysName,
                    leftRowName,
                    setOperation,
                    setOperation.Left,
                    ExecutionCSharpRenderer.SetKeyCondition.Contained)))
        ];
    }

    private IfStatementSyntax CreateConditionalSetRowOutput(
        string keysName,
        string rowName,
        ExecutionSetOperation setOperation,
        ExecutionVariable source,
        ExecutionCSharpRenderer.SetKeyCondition condition)
    {
        ExpressionSyntax conditionExpression = condition switch
        {
            ExecutionCSharpRenderer.SetKeyCondition.Added => CreateHashSetInvocation(keysName, nameof(HashSet<>.Add), rowName, setOperation, source),
            ExecutionCSharpRenderer.SetKeyCondition.Contained => CreateHashSetInvocation(keysName, nameof(HashSet<>.Contains), rowName, setOperation, source),
            ExecutionCSharpRenderer.SetKeyCondition.NotContained => SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                CreateHashSetInvocation(keysName, nameof(HashSet<>.Contains), rowName, setOperation, source)),
            _ => throw UnsupportedShape.Of($"Set key condition {condition}")
        };

        return SyntaxFactory.IfStatement(
            conditionExpression,
            StatementEmitter.CreateBlock(CreateFinalShapeOutputStatement(
                CreateFinalShapeCreationFromSetRow(rowName, source))));
    }

    private ObjectCreationExpressionSyntax CreateFinalShapeCreationFromSetRow(
        string rowVariableName,
        ExecutionVariable source)
    {
        if (!TryGetTypedRowBufferShape(source.Name, out var sourceShape))
            return CreateFinalShapeCreationFromRow(rowVariableName);

        var sink = _finalShapeYieldSink ??
                   throw new InvalidOperationException("Final shape sink is not active.");
        var arguments = sink.Fields
            .Select((field, index) => SyntaxFactory.Argument(
                CreateTypedSetFieldRead(rowVariableName, index, field.Type, sourceShape)))
            .ToArray();

        return CreateFinalShapeCreation(sink.ShapeTypeName, arguments);
    }
}
