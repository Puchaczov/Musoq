using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed class ExpressionRenderer(ExecutionCSharpRenderer renderer)
    {
        public ExpressionSyntax Render(ExecutionExpression expression) =>
            expression switch
            {
                ExecutionFieldRead fieldRead => renderer.RenderFieldRead(fieldRead),
                ExecutionScriptParameterRead parameterRead => CreateIdentifierName(renderer.GetScriptParameterLocalName(parameterRead.Name)),
                ExecutionScriptVariableRead variableRead => CreateIdentifierName(renderer.GetScriptVariableLocalName(variableRead.Name)),
                ExecutionLiteral literal => ExecutionCSharpRenderer.RenderLiteral(literal.Value),
                ExecutionBinary binary => renderer.RenderBinary(binary),
                ExecutionUnary unary => renderer.RenderUnary(unary),
                ExecutionMethodCall methodCall => renderer.RenderMethodCall(methodCall),
                ExecutionStrictCast strictCast => renderer.RenderStrictCast(strictCast),
                ExecutionMethodTargetReuseCandidate candidate => renderer.RenderMethodCall(candidate.MethodCall),
                ExecutionArrayAccess arrayAccess => renderer.RenderArrayAccess(arrayAccess),
                ExecutionIndexedHashRowCreate indexedRowCreate => ExecutionCSharpRenderer.RenderIndexedHashRowCreate(indexedRowCreate),
                ExecutionIndexedHashRowRowRead indexedRowRead => ExecutionCSharpRenderer.CreateIndexedHashRowMemberRead(indexedRowRead.IndexedRow, "Row"),
                ExecutionIndexedHashRowIndexRead indexedIndexRead => ExecutionCSharpRenderer.CreateIndexedHashRowMemberRead(indexedIndexRead.IndexedRow, "Index"),
                ExecutionIsNullCheck isNull => renderer.RenderIsNullCheck(isNull),
                ExecutionRowPresence rowPresence => renderer.RenderRowPresence(rowPresence),
                ExecutionInCheck inCheck => renderer.RenderInCheck(inCheck),
                ExecutionCollectionInCheck collectionInCheck => renderer.RenderCollectionInCheck(collectionInCheck),
                ExecutionPatternMatch patternMatch => renderer.RenderPatternMatch(patternMatch),
                ExecutionBetween between => renderer.RenderBetween(between),
                ExecutionCaseWhen caseWhen => renderer.RenderCaseWhen(caseWhen),
                ExecutionCoalesce coalesce => renderer.RenderCoalesce(coalesce),
                ExecutionRowStream { Kind: ExecutionRowStreamKind.Rows, RowsAccess: ExecutionRowStreamRowsAccess.TableRows } rows =>
                    renderer.TryGetTypedRowBufferShape(rows.Variable.Name, out _)
                        ? CreateIdentifierName(rows.Variable.Name)
                        : ExecutionCSharpRenderer.CreateTableRowsRead(rows.Variable.Name),
                ExecutionRowStream rows => SyntaxFactory.IdentifierName(rows.Variable.Name),
                ExecutionScalarRowStream rows => SyntaxFactory.IdentifierName(rows.Variable.Name),
                ExecutionStoredTable storedTable => ExecutionCSharpRenderer.CreateStoredTableRead(storedTable.TableIndex),
                ExecutionStoredTableRows storedRows => renderer.RenderStoredTableRows(storedRows),
                ExecutionVariableRead variableRead => CreateIdentifierName(variableRead.Variable.Name),
                ExecutionRowContextsRead contextsRead => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    CreateIdentifierName(contextsRead.Row.Name),
                    SyntaxFactory.IdentifierName(nameof(Row.Contexts))),
                ExecutionNullContextArray nullContextArray => ExecutionCSharpRenderer.CreateNullContextArray(nullContextArray.Count),
                ExecutionContextArray contextArray => renderer.RenderContextArray(contextArray),
                ExecutionCompositeKey compositeKey => renderer.CreateCompositeKeyInvocation(compositeKey),
                ExecutionValueTupleKey valueTupleKey => renderer.CreateValueTupleKeyExpression(valueTupleKey),
                ExecutionAggregateCall aggregateCall => ExecutionCSharpRenderer.CreateAggregateAccumulatorRead(aggregateCall),
                ExecutionWindowValueRead windowValueRead => ExecutionCSharpRenderer.CreateWindowValueRead(windowValueRead),
                ExecutionGroupKeyRead groupKeyRead => ExecutionCSharpRenderer.CreateGroupKeyRead(groupKeyRead),
                ExecutionAggregateCapturedValueRead capturedValueRead => ExecutionCSharpRenderer.CreateAggregateCapturedValueRead(capturedValueRead),
                _ => throw UnsupportedShape.Of($"Execution expression '{expression.GetType().Name}'", "the C# backend")
            };
    }

}
