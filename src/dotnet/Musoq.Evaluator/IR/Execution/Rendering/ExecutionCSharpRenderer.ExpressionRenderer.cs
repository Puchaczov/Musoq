using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed class ExpressionRenderer(ExecutionCSharpRenderer renderer, ExecutionRenderContext context)
    {
        public ExpressionSyntax Render(ExecutionExpression expression) =>
            expression switch
            {
                ExecutionFieldRead fieldRead => renderer.RenderFieldRead(fieldRead, context),
                ExecutionScriptParameterRead parameterRead => CreateIdentifierName(renderer.GetScriptParameterLocalName(parameterRead.Name)),
                ExecutionScriptVariableRead variableRead => CreateIdentifierName(renderer.GetScriptVariableLocalName(variableRead.Name)),
                ExecutionLiteral literal => ExecutionCSharpRenderer.RenderLiteral(literal.Value),
                ExecutionBinary binary => renderer.RenderBinary(binary, context),
                ExecutionUnary unary => renderer.RenderUnary(unary, context),
                ExecutionMethodCall methodCall => renderer.RenderMethodCall(methodCall, context),
                ExecutionStrictCast strictCast => renderer.RenderStrictCast(strictCast, context),
                ExecutionMethodTargetReuseCandidate candidate => renderer.RenderMethodCall(candidate.MethodCall, context),
                ExecutionArrayAccess arrayAccess => renderer.RenderArrayAccess(arrayAccess, context),
                ExecutionIndexedHashRowCreate indexedRowCreate => ExecutionCSharpRenderer.RenderIndexedHashRowCreate(indexedRowCreate),
                ExecutionIndexedHashRowRowRead indexedRowRead => ExecutionCSharpRenderer.CreateIndexedHashRowMemberRead(indexedRowRead.IndexedRow, "Row"),
                ExecutionIndexedHashRowIndexRead indexedIndexRead => ExecutionCSharpRenderer.CreateIndexedHashRowMemberRead(indexedIndexRead.IndexedRow, "Index"),
                ExecutionIsNullCheck isNull => renderer.RenderIsNullCheck(isNull, context),
                ExecutionRowPresence rowPresence => renderer.RenderRowPresence(rowPresence, context),
                ExecutionInCheck inCheck => renderer.RenderInCheck(inCheck, context),
                ExecutionCollectionInCheck collectionInCheck => renderer.RenderCollectionInCheck(collectionInCheck, context),
                ExecutionPatternMatch patternMatch => renderer.RenderPatternMatch(patternMatch, context),
                ExecutionBetween between => renderer.RenderBetween(between, context),
                ExecutionCaseWhen caseWhen => renderer.RenderCaseWhen(caseWhen, context),
                ExecutionCoalesce coalesce => renderer.RenderCoalesce(coalesce, context),
                ExecutionRowStream { Kind: ExecutionRowStreamKind.Rows, RowsAccess: ExecutionRowStreamRowsAccess.TableRows } rows =>
                    context.Session.TypedRowBufferVariables.ContainsKey(rows.Variable.Name)
                        ? CreateIdentifierName(rows.Variable.Name)
                        : ExecutionCSharpRenderer.CreateTableRowsRead(rows.Variable.Name),
                ExecutionRowStream rows => SyntaxFactory.IdentifierName(rows.Variable.Name),
                ExecutionScalarRowStream rows => SyntaxFactory.IdentifierName(rows.Variable.Name),
                ExecutionStoredTable storedTable => ExecutionCSharpRenderer.CreateStoredTableRead(storedTable.TableIndex),
                ExecutionStoredTableRows storedRows => renderer.RenderStoredTableRows(storedRows, context),
                ExecutionVariableRead variableRead => CreateIdentifierName(variableRead.Variable.Name),
                ExecutionRowContextsRead contextsRead => SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    CreateIdentifierName(contextsRead.Row.Name),
                    SyntaxFactory.IdentifierName(nameof(Row.Contexts))),
                ExecutionNullContextArray nullContextArray => ExecutionCSharpRenderer.CreateNullContextArray(nullContextArray.Count),
                ExecutionContextArray contextArray => renderer.RenderContextArray(contextArray, context),
                ExecutionCompositeKey compositeKey => renderer.CreateCompositeKeyInvocation(compositeKey, context),
                ExecutionValueTupleKey valueTupleKey => renderer.CreateValueTupleKeyExpression(valueTupleKey, context),
                ExecutionAggregateCall aggregateCall => ExecutionCSharpRenderer.CreateAggregateAccumulatorRead(aggregateCall),
                ExecutionWindowValueRead windowValueRead => ExecutionCSharpRenderer.CreateWindowValueRead(windowValueRead),
                ExecutionGroupKeyRead groupKeyRead => ExecutionCSharpRenderer.CreateGroupKeyRead(groupKeyRead),
                ExecutionAggregateCapturedValueRead capturedValueRead => ExecutionCSharpRenderer.CreateAggregateCapturedValueRead(capturedValueRead),
                _ => throw UnsupportedShape.Of($"Execution expression '{expression.GetType().Name}'", "the C# backend")
            };
    }

}
