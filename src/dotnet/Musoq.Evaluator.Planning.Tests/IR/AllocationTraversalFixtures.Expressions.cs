using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

internal sealed partial class AllocationTraversalFixtures
{
    private static void RegisterExpressions(Dictionary<Type, Func<AllocationTraversalFixtures, object>> factories)
    {
        factories.Add(
            typeof(ExecutionMethodTargetReuseCandidate),
            static fixture => new ExecutionMethodTargetReuseCandidate(
                (ExecutionMethodCall)fixture.Create(typeof(ExecutionMethodCall)),
                fixture._includeOptional
                    ? (ExecutionMethodTargetScope)fixture.Create(typeof(ExecutionMethodTargetScope))
                    : null
            )
        );
        factories.Add(
            typeof(ExecutionMethodCall),
            static fixture => new ExecutionMethodCall(
                (ExecutionCallableRef)fixture.Create(typeof(ExecutionCallableRef)),
                (IReadOnlyList<ExecutionExpression>)fixture.Create(typeof(IReadOnlyList<ExecutionExpression>)),
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null,
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                fixture._includeOptional ? (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)) : null,
                fixture._includeOptional ? (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)) : null,
                fixture._includeOptional ? (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)) : null
            )
        );
        factories.Add(
            typeof(ExecutionAggregateCall),
            static fixture => new ExecutionAggregateCall(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (ExecutionCallableRef)fixture.Create(typeof(ExecutionCallableRef)),
                (IReadOnlyList<ExecutionExpression>)fixture.Create(typeof(IReadOnlyList<ExecutionExpression>)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (AggregateAccumulatorField)fixture.Create(typeof(AggregateAccumulatorField)),
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null
            )
        );
        factories.Add(
            typeof(ExecutionAggregateCapturedValueRead),
            static fixture => new ExecutionAggregateCapturedValueRead(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (AggregateCapturedField)fixture.Create(typeof(AggregateCapturedField))
            )
        );
        factories.Add(
            typeof(ExecutionAggregateResultRef),
            static fixture => new ExecutionAggregateResultRef(
                (string)fixture.Create(typeof(string)),
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null,
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionArrayAccess),
            static fixture => new ExecutionArrayAccess(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionBetween),
            static fixture => new ExecutionBetween(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionBinary),
            static fixture => new ExecutionBinary(
                (global::Musoq.Evaluator.IR.Expressions.BinaryOpKind)
                    fixture.Create(typeof(global::Musoq.Evaluator.IR.Expressions.BinaryOpKind)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionCaseWhen),
            static fixture => new ExecutionCaseWhen(
                (IReadOnlyList<ExecutionCaseWhenBranch>)fixture.Create(typeof(IReadOnlyList<ExecutionCaseWhenBranch>)),
                fixture._includeOptional ? (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)) : null,
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionCoalesce),
            static fixture => new ExecutionCoalesce(
                (IReadOnlyList<ExecutionExpression>)fixture.Create(typeof(IReadOnlyList<ExecutionExpression>)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionCollectionInCheck),
            static fixture => new ExecutionCollectionInCheck(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionScriptParameterRead)fixture.Create(typeof(ExecutionScriptParameterRead)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionScriptParameterRead),
            static fixture => new ExecutionScriptParameterRead(
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionContextArray),
            static fixture => new ExecutionContextArray(
                (IReadOnlyList<ExecutionContextSegment>)fixture.Create(typeof(IReadOnlyList<ExecutionContextSegment>))
            )
        );
        factories.Add(
            typeof(ExecutionCteCollectionInput),
            static fixture => new ExecutionCteCollectionInput(
                (string)fixture.Create(typeof(string)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (IEnumerable<ExecutionCteCollectionField>)
                    fixture.Create(typeof(IEnumerable<ExecutionCteCollectionField>)),
                fixture._includeOptional
                    ? (ExecutionStructuralConstructionPlan)fixture.Create(typeof(ExecutionStructuralConstructionPlan))
                    : null
            )
        );
        factories.Add(
            typeof(ExecutionDynamicLikeMatch),
            static fixture => new ExecutionDynamicLikeMatch(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionStringMatchComparison)fixture.Create(typeof(ExecutionStringMatchComparison)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionDynamicRLikeMatch),
            static fixture => new ExecutionDynamicRLikeMatch(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionFieldRead),
            static fixture => new ExecutionFieldRead(
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null,
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                fixture._includeOptional ? (FieldAccessStrategy)fixture.Create(typeof(FieldAccessStrategy)) : null,
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null
            )
        );
        factories.Add(
            typeof(ExecutionGroupKeyRead),
            static fixture => new ExecutionGroupKeyRead(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                fixture._includeOptional ? (AggregateGroupKeyField)fixture.Create(typeof(AggregateGroupKeyField)) : null
            )
        );
        factories.Add(
            typeof(ExecutionInCheck),
            static fixture => new ExecutionInCheck(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (IReadOnlyList<ExecutionExpression>)fixture.Create(typeof(IReadOnlyList<ExecutionExpression>)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                fixture._includeOptional
                    ? (ExecutionConstantInSet)fixture.Create(typeof(ExecutionConstantInSet))
                    : null,
                (bool)fixture.Create(typeof(bool))
            )
        );
        factories.Add(
            typeof(ExecutionIndexedHashRowCreate),
            static fixture => new ExecutionIndexedHashRowCreate(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null
            )
        );
        factories.Add(
            typeof(ExecutionIndexedHashRowIndexRead),
            static fixture => new ExecutionIndexedHashRowIndexRead(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable))
            )
        );
        factories.Add(
            typeof(ExecutionIndexedHashRowRowRead),
            static fixture => new ExecutionIndexedHashRowRowRead(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionIsNullCheck),
            static fixture => new ExecutionIsNullCheck(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (bool)fixture.Create(typeof(bool)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionLikeMatcherCacheSlot),
            static fixture => new ExecutionLikeMatcherCacheSlot(
                (bool)fixture.Create(typeof(bool)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionLiteral),
            static fixture => new ExecutionLiteral(
                (ExecutionConstantValue)fixture.Create(typeof(ExecutionConstantValue)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionMemberRead),
            static fixture => new ExecutionMemberRead(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (bool)fixture.Create(typeof(bool))
            )
        );
        factories.Add(
            typeof(ExecutionNullContextArray),
            static fixture => new ExecutionNullContextArray((int)fixture.Create(typeof(int)))
        );
        factories.Add(
            typeof(ExecutionPatternMatch),
            static fixture => new ExecutionPatternMatch(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (global::Musoq.Evaluator.IR.Expressions.PatternKind)
                    fixture.Create(typeof(global::Musoq.Evaluator.IR.Expressions.PatternKind)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionPreparedLikeMatch),
            static fixture => new ExecutionPreparedLikeMatch(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionPreparedRLikeMatch),
            static fixture => new ExecutionPreparedRLikeMatch(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionPrepareLikeMatcher),
            static fixture => new ExecutionPrepareLikeMatcher(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionStringMatchComparison)fixture.Create(typeof(ExecutionStringMatchComparison)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionPrepareRLikeMatcher),
            static fixture => new ExecutionPrepareRLikeMatcher(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionRLikeMatcherCacheSlot),
            static fixture => new ExecutionRLikeMatcherCacheSlot(
                (bool)fixture.Create(typeof(bool)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionRowContextsRead),
            static fixture => new ExecutionRowContextsRead((ExecutionVariable)fixture.Create(typeof(ExecutionVariable)))
        );
        factories.Add(
            typeof(ExecutionRowPresence),
            static fixture => new ExecutionRowPresence(
                (string)fixture.Create(typeof(string)),
                (bool)fixture.Create(typeof(bool)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression))
            )
        );
        factories.Add(
            typeof(ExecutionRowStream),
            static fixture => new ExecutionRowStream(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (ExecutionRowStreamKind)fixture.Create(typeof(ExecutionRowStreamKind)),
                (ExecutionRowStreamRowsAccess)fixture.Create(typeof(ExecutionRowStreamRowsAccess))
            )
        );
        factories.Add(
            typeof(ExecutionScalarRowStream),
            static fixture => new ExecutionScalarRowStream((ExecutionVariable)fixture.Create(typeof(ExecutionVariable)))
        );
        factories.Add(
            typeof(ExecutionScriptVariableRead),
            static fixture => new ExecutionScriptVariableRead(
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionStoredTable),
            static fixture => new ExecutionStoredTable((int)fixture.Create(typeof(int)))
        );
        factories.Add(
            typeof(ExecutionStoredTableRows),
            static fixture => new ExecutionStoredTableRows(
                (int)fixture.Create(typeof(int)),
                fixture._includeOptional ? (GeneratedRowShape)fixture.Create(typeof(GeneratedRowShape)) : null
            )
        );
        factories.Add(
            typeof(ExecutionStrictCast),
            static fixture => new ExecutionStrictCast(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                fixture._includeOptional ? (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)) : null
            )
        );
        factories.Add(
            typeof(ExecutionStringConcatenation),
            static fixture => new ExecutionStringConcatenation(
                (IReadOnlyList<ExecutionExpression>)fixture.Create(typeof(IReadOnlyList<ExecutionExpression>))
            )
        );
        factories.Add(
            typeof(ExecutionStringMatch),
            static fixture => new ExecutionStringMatch(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (string)fixture.Create(typeof(string)),
                (string)fixture.Create(typeof(string)),
                (ExecutionStringMatchKind)fixture.Create(typeof(ExecutionStringMatchKind)),
                (ExecutionStringMatchComparison)fixture.Create(typeof(ExecutionStringMatchComparison)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionStructuralRecord),
            static fixture => new ExecutionStructuralRecord(
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (IEnumerable<ExecutionStructuralField>)fixture.Create(typeof(IEnumerable<ExecutionStructuralField>)),
                fixture._includeOptional
                    ? (ExecutionStructuralConstructionPlan)fixture.Create(typeof(ExecutionStructuralConstructionPlan))
                    : null
            )
        );
        factories.Add(
            typeof(ExecutionStructuralArray),
            static fixture => new ExecutionStructuralArray(
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (IEnumerable<ExecutionExpression>)fixture.Create(typeof(IEnumerable<ExecutionExpression>))
            )
        );
        factories.Add(
            typeof(ExecutionStructuralConversion),
            static fixture => new ExecutionStructuralConversion(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionSubquery),
            static fixture => new ExecutionSubquery(
                (string)fixture.Create(typeof(string)),
                (global::Musoq.Evaluator.IR.Expressions.SubqueryResultKind)
                    fixture.Create(typeof(global::Musoq.Evaluator.IR.Expressions.SubqueryResultKind)),
                (ExecutionPlan)fixture.Create(typeof(ExecutionPlan)),
                (IReadOnlyList<ExecutionExpression>)fixture.Create(typeof(IReadOnlyList<ExecutionExpression>)),
                fixture._includeOptional ? (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)) : null,
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionUnary),
            static fixture => new ExecutionUnary(
                (global::Musoq.Evaluator.IR.Expressions.UnaryOpKind)
                    fixture.Create(typeof(global::Musoq.Evaluator.IR.Expressions.UnaryOpKind)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionUnionMemberRead),
            static fixture => new ExecutionUnionMemberRead(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (IReadOnlyList<ExecutionUnionMemberCase>)fixture.Create(typeof(IReadOnlyList<ExecutionUnionMemberCase>))
            )
        );
        factories.Add(
            typeof(ExecutionValueTupleKey),
            static fixture => new ExecutionValueTupleKey(
                (IReadOnlyList<ExecutionExpression>)fixture.Create(typeof(IReadOnlyList<ExecutionExpression>)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionVariableRead),
            static fixture => new ExecutionVariableRead((ExecutionVariable)fixture.Create(typeof(ExecutionVariable)))
        );
        factories.Add(
            typeof(ExecutionWindowResultRef),
            static fixture => new ExecutionWindowResultRef(
                (int)fixture.Create(typeof(int)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionWindowValueRead),
            static fixture => new ExecutionWindowValueRead(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
    }
}
