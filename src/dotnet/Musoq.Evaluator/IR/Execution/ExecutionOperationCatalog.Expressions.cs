using System.Collections.Generic;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionOperationCatalog
{
    private static IEnumerable<KeyValuePair<Type, ExecutionOperationId>> CreateExpressionOperations()
    {
        yield return Operation<ExecutionFieldRead>("expr.field-read");
        yield return Operation<ExecutionMemberRead>("expr.member-read");
        yield return Operation<ExecutionScriptParameterRead>("expr.script-parameter");
        yield return Operation<ExecutionScriptVariableRead>("expr.script-variable");
        yield return Operation<ExecutionLiteral>("expr.literal");
        yield return Operation<ExecutionBinary>("expr.binary");
        yield return Operation<ExecutionUnary>("expr.unary");
        yield return Operation<ExecutionMethodCall>("expr.call");
        yield return Operation<ExecutionStrictCast>("expr.strict-cast");
        yield return Operation<ExecutionMethodTargetReuseCandidate>("optimizer.method-target-reuse");
        yield return Operation<ExecutionArrayAccess>("expr.array-access");
        yield return Operation<ExecutionIndexedHashRowCreate>("expr.indexed-hash-row.create");
        yield return Operation<ExecutionIndexedHashRowRowRead>("expr.indexed-hash-row.row-read");
        yield return Operation<ExecutionIndexedHashRowIndexRead>("expr.indexed-hash-row.index-read");
        yield return Operation<ExecutionIsNullCheck>("expr.null-check");
        yield return Operation<ExecutionRowPresence>("expr.row-presence");
        yield return Operation<ExecutionInCheck>("expr.in");
        yield return Operation<ExecutionCollectionInCheck>("expr.collection-in");
        yield return Operation<ExecutionPatternMatch>("expr.pattern");
        yield return Operation<ExecutionBetween>("expr.between");
        yield return Operation<ExecutionCaseWhen>("expr.case");
        yield return Operation<ExecutionCoalesce>("expr.coalesce");
        yield return Operation<ExecutionRowStream>("stream.rows");
        yield return Operation<ExecutionScalarRowStream>("stream.scalar-row");
        yield return Operation<ExecutionStoredTable>("cte.table.read");
        yield return Operation<ExecutionStoredTableRows>("cte.table.rows");
        yield return Operation<ExecutionVariableRead>("expr.variable-read");
        yield return Operation<ExecutionRowContextsRead>("expr.row-contexts");
        yield return Operation<ExecutionNullContextArray>("expr.null-context-array");
        yield return Operation<ExecutionContextArray>("expr.context-array");
        yield return Operation<ExecutionCompositeKey>("expr.composite-key");
        yield return Operation<ExecutionValueTupleKey>("expr.tuple-key");
        yield return Operation<ExecutionWindowValueRead>("window.value-read");
        yield return Operation<ExecutionAggregateCall>("aggregate.call");
        yield return Operation<ExecutionGroupKeyRead>("aggregate.group-key-read");
        yield return Operation<ExecutionAggregateCapturedValueRead>("aggregate.capture-read");
        yield return Operation<ExecutionAggregateResultRef>("aggregate.result-ref");
        yield return Operation<ExecutionWindowResultRef>("window.result-ref");
    }
}
