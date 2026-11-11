using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

internal sealed partial class AllocationTraversalFixtures
{
    private static void RegisterDependencies(Dictionary<Type, Func<AllocationTraversalFixtures, object>> factories)
    {
        factories.Add(
            typeof(IReadOnlyList<ExecutionFusedCteOutput>),
            static fixture =>
                new ExecutionFusedCteOutput[]
                {
                    (ExecutionFusedCteOutput)fixture.Create(typeof(ExecutionFusedCteOutput)),
                    (ExecutionFusedCteOutput)fixture.Create(typeof(ExecutionFusedCteOutput)),
                }
        );
        factories.Add(
            typeof(ExecutionFusedCteOutput),
            static fixture => new ExecutionFusedCteOutput(
                (int)fixture.Create(typeof(int)),
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (GeneratedRowShape)fixture.Create(typeof(GeneratedRowShape)),
                (bool)fixture.Create(typeof(bool))
            )
        );
        factories.Add(typeof(int), static fixture => default(int));
        factories.Add(
            typeof(ExecutionVariable),
            static fixture => new ExecutionVariable($"reference{++fixture._nextName}", typeof(int))
        );
        factories.Add(
            typeof(GeneratedRowShape),
            static fixture => new GeneratedRowShape(
                (string)fixture.Create(typeof(string)),
                (IReadOnlyList<FieldBinding>)fixture.Create(typeof(IReadOnlyList<FieldBinding>)),
                (IReadOnlyList<FieldBinding>)fixture.Create(typeof(IReadOnlyList<FieldBinding>)),
                (bool)fixture.Create(typeof(bool)),
                (bool)fixture.Create(typeof(bool))
            )
        );
        factories.Add(typeof(string), static fixture => $"name{++fixture._nextName}");
        factories.Add(
            typeof(IReadOnlyList<FieldBinding>),
            static fixture =>
                new FieldBinding[]
                {
                    (FieldBinding)fixture.Create(typeof(FieldBinding)),
                    (FieldBinding)fixture.Create(typeof(FieldBinding)),
                }
        );
        factories.Add(
            typeof(FieldBinding),
            static fixture => new FieldBinding(
                (string)fixture.Create(typeof(string)),
                (string)fixture.Create(typeof(string)),
                (int)fixture.Create(typeof(int)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (FieldNullability)fixture.Create(typeof(FieldNullability)),
                (FieldAccessStrategy)fixture.Create(typeof(FieldAccessStrategy)),
                fixture._includeOptional ? (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)) : null,
                fixture._includeOptional
                    ? (IReadOnlyDictionary<string, string>)fixture.Create(typeof(IReadOnlyDictionary<string, string>))
                    : null,
                fixture._includeOptional ? (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)) : null,
                fixture._includeOptional
                    ? (global::Musoq.Schema.EnumTypeDescriptor)
                        fixture.Create(typeof(global::Musoq.Schema.EnumTypeDescriptor))
                    : null
            )
        );
        factories.Add(typeof(ExecutionTypeRef), static fixture => ExecutionClrBindingFactory.FromClr(typeof(int)));
        factories.Add(typeof(FieldNullability), static fixture => default(FieldNullability));
        factories.Add(typeof(FieldAccessStrategy), static fixture => default(FieldAccessStrategy)!);
        factories.Add(typeof(IReadOnlyDictionary<string, string>), static fixture => new Dictionary<string, string>());
        factories.Add(
            typeof(global::Musoq.Schema.EnumTypeDescriptor),
            static fixture => new global::Musoq.Schema.EnumTypeDescriptor(
                (string)fixture.Create(typeof(string)),
                (global::Musoq.Schema.EnumTypeOrigin)fixture.Create(typeof(global::Musoq.Schema.EnumTypeOrigin)),
                (global::Musoq.Schema.EnumUnderlyingKind)
                    fixture.Create(typeof(global::Musoq.Schema.EnumUnderlyingKind)),
                (bool)fixture.Create(typeof(bool)),
                (IReadOnlyList<global::Musoq.Schema.EnumMemberDescriptor>)
                    fixture.Create(typeof(IReadOnlyList<global::Musoq.Schema.EnumMemberDescriptor>))
            )
        );
        factories.Add(
            typeof(global::Musoq.Schema.EnumTypeOrigin),
            static fixture => default(global::Musoq.Schema.EnumTypeOrigin)
        );
        factories.Add(
            typeof(global::Musoq.Schema.EnumUnderlyingKind),
            static fixture => default(global::Musoq.Schema.EnumUnderlyingKind)
        );
        factories.Add(typeof(bool), static fixture => default(bool));
        factories.Add(
            typeof(IReadOnlyList<global::Musoq.Schema.EnumMemberDescriptor>),
            static fixture =>
                new global::Musoq.Schema.EnumMemberDescriptor[]
                {
                    (global::Musoq.Schema.EnumMemberDescriptor)
                        fixture.Create(typeof(global::Musoq.Schema.EnumMemberDescriptor)),
                    (global::Musoq.Schema.EnumMemberDescriptor)
                        fixture.Create(typeof(global::Musoq.Schema.EnumMemberDescriptor)),
                }
        );
        factories.Add(
            typeof(global::Musoq.Schema.EnumMemberDescriptor),
            static fixture => new global::Musoq.Schema.EnumMemberDescriptor(
                (string)fixture.Create(typeof(string)),
                (global::Musoq.Schema.EnumScalarValue)fixture.Create(typeof(global::Musoq.Schema.EnumScalarValue))
            )
        );
        factories.Add(
            typeof(global::Musoq.Schema.EnumScalarValue),
            static fixture => default(global::Musoq.Schema.EnumScalarValue)
        );
        factories.Add(
            typeof(ExecutionBlock),
            static fixture => new ExecutionBlock([new ExecutionBreak(), new ExecutionContinue()])
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionRowValue>),
            static fixture =>
                new ExecutionRowValue[]
                {
                    (ExecutionRowValue)fixture.Create(typeof(ExecutionRowValue)),
                    (ExecutionRowValue)fixture.Create(typeof(ExecutionRowValue)),
                }
        );
        factories.Add(
            typeof(ExecutionRowValue),
            static fixture => new ExecutionRowValue(
                (string)fixture.Create(typeof(string)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression))
            )
        );
        factories.Add(
            typeof(ExecutionExpression),
            static fixture => new ExecutionLiteral(++fixture._nextName, typeof(int))
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionExpression>),
            static fixture =>
                new ExecutionExpression[]
                {
                    (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                    (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                }
        );
        factories.Add(typeof(ExecutionAppendMode), static fixture => default(ExecutionAppendMode));
        factories.Add(
            typeof(ExecutionContextLayout),
            static fixture => new ExecutionContextLayout(
                (IReadOnlyList<ExecutionContextSegment>)fixture.Create(typeof(IReadOnlyList<ExecutionContextSegment>))
            )
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionContextSegment>),
            static fixture =>
                new ExecutionContextSegment[]
                {
                    (ExecutionContextSegment)fixture.Create(typeof(ExecutionContextSegment)),
                    (ExecutionContextSegment)fixture.Create(typeof(ExecutionContextSegment)),
                }
        );
        factories.Add(
            typeof(ExecutionContextSegment),
            static fixture => new ExecutionContextSegment(
                (ExecutionContextSegmentKind)fixture.Create(typeof(ExecutionContextSegmentKind)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (int)fixture.Create(typeof(int))
            )
        );
        factories.Add(typeof(ExecutionContextSegmentKind), static fixture => default(ExecutionContextSegmentKind));
        factories.Add(
            typeof(IReadOnlyList<ExecutionCteSidecarAppendIndexSpec>),
            static fixture =>
                new ExecutionCteSidecarAppendIndexSpec[]
                {
                    (ExecutionCteSidecarAppendIndexSpec)fixture.Create(typeof(ExecutionCteSidecarAppendIndexSpec)),
                    (ExecutionCteSidecarAppendIndexSpec)fixture.Create(typeof(ExecutionCteSidecarAppendIndexSpec)),
                }
        );
        factories.Add(
            typeof(ExecutionCteSidecarAppendIndexSpec),
            static fixture => new ExecutionCteSidecarAppendIndexSpec(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionCteSidecarIndexKind)fixture.Create(typeof(ExecutionCteSidecarIndexKind)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                fixture._includeOptional ? (HashPayloadShape)fixture.Create(typeof(HashPayloadShape)) : null,
                (IReadOnlyList<ExecutionRowValue>)fixture.Create(typeof(IReadOnlyList<ExecutionRowValue>))
            )
        );
        factories.Add(typeof(ExecutionCteSidecarIndexKind), static fixture => default(ExecutionCteSidecarIndexKind));
        factories.Add(
            typeof(HashPayloadShape),
            static fixture => new HashPayloadShape(
                (string)fixture.Create(typeof(string)),
                (IReadOnlyList<FieldBinding>)fixture.Create(typeof(IReadOnlyList<FieldBinding>)),
                (IReadOnlyList<FieldBinding>)fixture.Create(typeof(IReadOnlyList<FieldBinding>))
            )
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionCteSidecarIndexCreateSpec>),
            static fixture =>
                new ExecutionCteSidecarIndexCreateSpec[]
                {
                    (ExecutionCteSidecarIndexCreateSpec)fixture.Create(typeof(ExecutionCteSidecarIndexCreateSpec)),
                    (ExecutionCteSidecarIndexCreateSpec)fixture.Create(typeof(ExecutionCteSidecarIndexCreateSpec)),
                }
        );
        factories.Add(
            typeof(ExecutionCteSidecarIndexCreateSpec),
            static fixture => new ExecutionCteSidecarIndexCreateSpec(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (ExecutionCteSidecarIndexKind)fixture.Create(typeof(ExecutionCteSidecarIndexKind)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                fixture._includeOptional ? (ExecutionCapacityHint)fixture.Create(typeof(ExecutionCapacityHint)) : null,
                fixture._includeOptional ? (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)) : null,
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null
            )
        );
        factories.Add(typeof(ExecutionCapacityHint), static fixture => default(ExecutionCapacityHint)!);
        factories.Add(typeof(ExecutionHoistKind), static fixture => default(ExecutionHoistKind));
        factories.Add(typeof(ExecutionHoistScope), static fixture => default(ExecutionHoistScope));
        factories.Add(
            typeof(ExecutionMethodTargetScope),
            static fixture => new ExecutionMethodTargetScope(
                (ExecutionMethodTargetScopeKind)fixture.Create(typeof(ExecutionMethodTargetScopeKind)),
                (string)fixture.Create(typeof(string))
            )
        );
        factories.Add(
            typeof(ExecutionMethodTargetScopeKind),
            static fixture => default(ExecutionMethodTargetScopeKind)
        );
        factories.Add(
            typeof(AggregateCapturedField),
            static fixture => new AggregateCapturedField(
                (string)fixture.Create(typeof(string)),
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionCallableRef),
            static fixture => new ExecutionCallableRef(
                (global::Musoq.Targets.Abstractions.ExecutionPortableCallableDescriptor)
                    fixture.Create(typeof(global::Musoq.Targets.Abstractions.ExecutionPortableCallableDescriptor))
            )
        );
        factories.Add(
            typeof(global::Musoq.Targets.Abstractions.ExecutionPortableCallableDescriptor),
            static fixture => new global::Musoq.Targets.Abstractions.ExecutionPortableCallableDescriptor(
                (global::Musoq.Targets.Abstractions.ExecutionPortableCallableKind)
                    fixture.Create(typeof(global::Musoq.Targets.Abstractions.ExecutionPortableCallableKind)),
                (string)fixture.Create(typeof(string)),
                (string)fixture.Create(typeof(string))
            )
        );
        factories.Add(
            typeof(global::Musoq.Targets.Abstractions.ExecutionPortableCallableKind),
            static fixture => default(global::Musoq.Targets.Abstractions.ExecutionPortableCallableKind)
        );
        factories.Add(
            typeof(AggregateAccumulatorField),
            static fixture => new AggregateAccumulatorField(
                (string)fixture.Create(typeof(string)),
                (string)fixture.Create(typeof(string)),
                (global::Musoq.Plugins.AggregateKernelDescriptor)
                    fixture.Create(typeof(global::Musoq.Plugins.AggregateKernelDescriptor)),
                (int)fixture.Create(typeof(int)),
                (int)fixture.Create(typeof(int)),
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null
            )
        );
        factories.Add(
            typeof(global::Musoq.Plugins.AggregateKernelDescriptor),
            static fixture => new global::Musoq.Plugins.AggregateKernelDescriptor(
                (string)fixture.Create(typeof(string)),
                (global::System.Reflection.MethodInfo)fixture.Create(typeof(global::System.Reflection.MethodInfo)),
                (Type)fixture.Create(typeof(Type)),
                (Type)fixture.Create(typeof(Type)),
                (global::System.Reflection.MethodInfo)fixture.Create(typeof(global::System.Reflection.MethodInfo)),
                (global::System.Reflection.MethodInfo)fixture.Create(typeof(global::System.Reflection.MethodInfo)),
                fixture._includeOptional
                    ? (global::System.Reflection.MethodInfo)fixture.Create(typeof(global::System.Reflection.MethodInfo))
                    : null,
                (global::Musoq.Plugins.AggregateInputShape)
                    fixture.Create(typeof(global::Musoq.Plugins.AggregateInputShape)),
                (Type)fixture.Create(typeof(Type)),
                (Type)fixture.Create(typeof(Type)),
                (global::Musoq.Plugins.AggregateResultDescriptor)
                    fixture.Create(typeof(global::Musoq.Plugins.AggregateResultDescriptor)),
                fixture._includeOptional ? (int)fixture.Create(typeof(int)) : null,
                (bool)fixture.Create(typeof(bool))
            )
            {
                ResetMethod = ((Action)(() => { })).Method,
            }
        );
        factories.Add(typeof(global::System.Reflection.MethodInfo), static fixture => ((Action)(() => { })).Method);
        factories.Add(typeof(Type), static fixture => typeof(int));
        factories.Add(
            typeof(global::Musoq.Plugins.AggregateInputShape),
            static fixture => new global::Musoq.Plugins.AggregateInputShape(
                (Type)fixture.Create(typeof(Type)),
                (IReadOnlyList<Type>)fixture.Create(typeof(IReadOnlyList<Type>))
            )
        );
        factories.Add(
            typeof(IReadOnlyList<Type>),
            static fixture => new Type[] { (Type)fixture.Create(typeof(Type)), (Type)fixture.Create(typeof(Type)) }
        );
        factories.Add(
            typeof(global::Musoq.Plugins.AggregateResultDescriptor),
            static fixture => new global::Musoq.Plugins.AggregateResultDescriptor(
                (Type)fixture.Create(typeof(Type)),
                (Type)fixture.Create(typeof(Type)),
                (global::Musoq.Plugins.AggregateEmptyResultBehavior)
                    fixture.Create(typeof(global::Musoq.Plugins.AggregateEmptyResultBehavior))
            )
        );
        factories.Add(
            typeof(global::Musoq.Plugins.AggregateEmptyResultBehavior),
            static fixture => default(global::Musoq.Plugins.AggregateEmptyResultBehavior)
        );
        factories.Add(
            typeof(AggregateGroupPlan),
            static fixture => new AggregateGroupPlan(
                (AggregateGroupShape)fixture.Create(typeof(AggregateGroupShape)),
                (IReadOnlyList<AggregateGroupLevelPlan>)fixture.Create(typeof(IReadOnlyList<AggregateGroupLevelPlan>))
            )
        );
        factories.Add(
            typeof(AggregateGroupShape),
            static fixture => new AggregateGroupShape(
                (string)fixture.Create(typeof(string)),
                (IReadOnlyList<AggregateGroupKeyField>)fixture.Create(typeof(IReadOnlyList<AggregateGroupKeyField>)),
                (IReadOnlyList<AggregateCapturedField>)fixture.Create(typeof(IReadOnlyList<AggregateCapturedField>)),
                (IReadOnlyList<AggregateAccumulatorField>)
                    fixture.Create(typeof(IReadOnlyList<AggregateAccumulatorField>)),
                (IReadOnlyList<AggregateGroupOwnerField>)fixture.Create(typeof(IReadOnlyList<AggregateGroupOwnerField>))
            )
        );
        factories.Add(
            typeof(IReadOnlyList<AggregateGroupKeyField>),
            static fixture =>
                new AggregateGroupKeyField[]
                {
                    (AggregateGroupKeyField)fixture.Create(typeof(AggregateGroupKeyField)),
                    (AggregateGroupKeyField)fixture.Create(typeof(AggregateGroupKeyField)),
                }
        );
        factories.Add(
            typeof(AggregateGroupKeyField),
            static fixture => new AggregateGroupKeyField(
                (string)fixture.Create(typeof(string)),
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(IReadOnlyList<AggregateCapturedField>),
            static fixture =>
                new AggregateCapturedField[]
                {
                    (AggregateCapturedField)fixture.Create(typeof(AggregateCapturedField)),
                    (AggregateCapturedField)fixture.Create(typeof(AggregateCapturedField)),
                }
        );
        factories.Add(
            typeof(IReadOnlyList<AggregateAccumulatorField>),
            static fixture =>
                new AggregateAccumulatorField[]
                {
                    (AggregateAccumulatorField)fixture.Create(typeof(AggregateAccumulatorField)),
                    (AggregateAccumulatorField)fixture.Create(typeof(AggregateAccumulatorField)),
                }
        );
        factories.Add(
            typeof(IReadOnlyList<AggregateGroupOwnerField>),
            static fixture =>
                new AggregateGroupOwnerField[]
                {
                    (AggregateGroupOwnerField)fixture.Create(typeof(AggregateGroupOwnerField)),
                    (AggregateGroupOwnerField)fixture.Create(typeof(AggregateGroupOwnerField)),
                }
        );
        factories.Add(
            typeof(AggregateGroupOwnerField),
            static fixture => new AggregateGroupOwnerField(
                (int)fixture.Create(typeof(int)),
                (string)fixture.Create(typeof(string)),
                (AggregateGroupShape)fixture.Create(typeof(AggregateGroupShape))
            )
        );
        factories.Add(
            typeof(IReadOnlyList<AggregateGroupLevelPlan>),
            static fixture =>
                new AggregateGroupLevelPlan[]
                {
                    (AggregateGroupLevelPlan)fixture.Create(typeof(AggregateGroupLevelPlan)),
                    (AggregateGroupLevelPlan)fixture.Create(typeof(AggregateGroupLevelPlan)),
                }
        );
        factories.Add(
            typeof(AggregateGroupLevelPlan),
            static fixture => new AggregateGroupLevelPlan(
                (int)fixture.Create(typeof(int)),
                (AggregateGroupShape)fixture.Create(typeof(AggregateGroupShape))
            )
        );
        factories.Add(
            typeof(IReadOnlyList<AggregateGroupLookup>),
            static fixture =>
                new AggregateGroupLookup[]
                {
                    (AggregateGroupLookup)fixture.Create(typeof(AggregateGroupLookup)),
                    (AggregateGroupLookup)fixture.Create(typeof(AggregateGroupLookup)),
                }
        );
        factories.Add(
            typeof(AggregateGroupLookup),
            static fixture => new AggregateGroupLookup(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (int)fixture.Create(typeof(int))
            )
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionTypeRef>),
            static fixture =>
                new ExecutionTypeRef[]
                {
                    (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                    (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                }
        );
        factories.Add(
            typeof(IReadOnlyList<string>),
            static fixture =>
                new string[] { (string)fixture.Create(typeof(string)), (string)fixture.Create(typeof(string)) }
        );
        factories.Add(
            typeof(GeneratedRecordShape),
            static fixture => new GeneratedRecordShape(
                (string)fixture.Create(typeof(string)),
                (IReadOnlyList<FieldBinding>)fixture.Create(typeof(IReadOnlyList<FieldBinding>)),
                (bool)fixture.Create(typeof(bool))
            )
        );
        factories.Add(typeof(ExecutionRowAccessMode), static fixture => default(ExecutionRowAccessMode));
        factories.Add(typeof(ExecutionLetCacheMode), static fixture => default(ExecutionLetCacheMode));
        factories.Add(
            typeof(IReadOnlyList<ExecutionParallelTask>),
            static fixture =>
                new ExecutionParallelTask[]
                {
                    (ExecutionParallelTask)fixture.Create(typeof(ExecutionParallelTask)),
                    (ExecutionParallelTask)fixture.Create(typeof(ExecutionParallelTask)),
                }
        );
        factories.Add(
            typeof(ExecutionParallelTask),
            static fixture => new ExecutionParallelTask(
                (string)fixture.Create(typeof(string)),
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (ExecutionBlock)fixture.Create(typeof(ExecutionBlock)),
                fixture._includeOptional ? (int)fixture.Create(typeof(int)) : null,
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null
            )
        );
        factories.Add(
            typeof(ExecutionParallelMerge),
            static fixture => new ExecutionParallelMerge((ExecutionBlock)fixture.Create(typeof(ExecutionBlock)))
        );
        factories.Add(typeof(ExecutionStructuralConstructionPlan), static fixture => fixture.CreateConstructionPlan());
        factories.Add(
            typeof(ExecutionStructuralShape),
            static fixture => new ExecutionStructuralShape(
                global::Musoq.Schema.StructuralInputs.StructuralTypeKind.Scalar,
                ExecutionClrBindingFactory.FromClr(typeof(int)),
                null,
                null,
                false
            )
        );
        factories.Add(
            typeof(global::Musoq.Schema.StructuralInputs.StructuralTypeKind),
            static fixture => default(global::Musoq.Schema.StructuralInputs.StructuralTypeKind)
        );
        factories.Add(
            typeof(IEnumerable<ExecutionStructuralFieldPlan>),
            static fixture =>
                new ExecutionStructuralFieldPlan[]
                {
                    (ExecutionStructuralFieldPlan)fixture.Create(typeof(ExecutionStructuralFieldPlan)),
                    (ExecutionStructuralFieldPlan)fixture.Create(typeof(ExecutionStructuralFieldPlan)),
                }
        );
        factories.Add(
            typeof(ExecutionStructuralFieldPlan),
            static fixture => new ExecutionStructuralFieldPlan(
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (bool)fixture.Create(typeof(bool)),
                (bool)fixture.Create(typeof(bool)),
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null,
                fixture._includeOptional
                    ? (ExecutionStructuralShape)fixture.Create(typeof(ExecutionStructuralShape))
                    : null
            )
        );
        factories.Add(
            typeof(IEnumerable<int>),
            static fixture => new int[] { (int)fixture.Create(typeof(int)), (int)fixture.Create(typeof(int)) }
        );
        factories.Add(
            typeof(IEnumerable<ExecutionExpression>),
            static fixture =>
                new ExecutionExpression[]
                {
                    (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                    (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                }
        );
        factories.Add(
            typeof(ExecutionStructuralPreparationLifetime),
            static fixture => default(ExecutionStructuralPreparationLifetime)
        );
        factories.Add(typeof(ExecutionStructuralLimitPlan), static fixture => default(ExecutionStructuralLimitPlan));
        factories.Add(
            typeof(ExecutionStructuralInputOrigin),
            static fixture => default(ExecutionStructuralInputOrigin)
        );
        factories.Add(
            typeof(ExecutionStructuralMetricsStrategy),
            static fixture => default(ExecutionStructuralMetricsStrategy)
        );
        factories.Add(
            typeof(ExecutionStructuralOwnershipMode),
            static fixture => default(ExecutionStructuralOwnershipMode)
        );
        factories.Add(
            typeof(ExecutionRecursiveCteIdentityMode),
            static fixture => default(ExecutionRecursiveCteIdentityMode)
        );
        factories.Add(
            typeof(int[]),
            static fixture => new int[] { (int)fixture.Create(typeof(int)), (int)fixture.Create(typeof(int)) }
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionAsOfEqualityKey>),
            static fixture =>
                new ExecutionAsOfEqualityKey[]
                {
                    (ExecutionAsOfEqualityKey)fixture.Create(typeof(ExecutionAsOfEqualityKey)),
                    (ExecutionAsOfEqualityKey)fixture.Create(typeof(ExecutionAsOfEqualityKey)),
                }
        );
        factories.Add(
            typeof(ExecutionAsOfEqualityKey),
            static fixture => new ExecutionAsOfEqualityKey(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression))
            )
        );
        factories.Add(
            typeof(global::Musoq.Evaluator.IR.Expressions.BinaryOpKind),
            static fixture => default(global::Musoq.Evaluator.IR.Expressions.BinaryOpKind)
        );
        factories.Add(
            typeof(ExecutionAsOfTieBreak),
            static fixture => new ExecutionAsOfTieBreak(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (bool)fixture.Create(typeof(bool)),
                (global::Musoq.Evaluator.IR.Bindings.NullOrdering)
                    fixture.Create(typeof(global::Musoq.Evaluator.IR.Bindings.NullOrdering))
            )
        );
        factories.Add(
            typeof(global::Musoq.Evaluator.IR.Bindings.NullOrdering),
            static fixture => default(global::Musoq.Evaluator.IR.Bindings.NullOrdering)
        );
        factories.Add(typeof(ExecutionKeyBuildNullHandling), static fixture => default(ExecutionKeyBuildNullHandling));
        factories.Add(
            typeof(IReadOnlyList<int>),
            static fixture => new int[] { (int)fixture.Create(typeof(int)), (int)fixture.Create(typeof(int)) }
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionOrderField>),
            static fixture =>
                new ExecutionOrderField[]
                {
                    (ExecutionOrderField)fixture.Create(typeof(ExecutionOrderField)),
                    (ExecutionOrderField)fixture.Create(typeof(ExecutionOrderField)),
                }
        );
        factories.Add(
            typeof(ExecutionOrderField),
            static fixture => new ExecutionOrderField(
                (string)fixture.Create(typeof(string)),
                (int)fixture.Create(typeof(int)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (bool)fixture.Create(typeof(bool)),
                (global::Musoq.Evaluator.IR.Bindings.NullOrdering)
                    fixture.Create(typeof(global::Musoq.Evaluator.IR.Bindings.NullOrdering))
            )
        );
        factories.Add(typeof(ExecutionOrderRecordSelection), static fixture => default(ExecutionOrderRecordSelection)!);
        factories.Add(
            typeof(global::Musoq.Evaluator.QueryPhase),
            static fixture => default(global::Musoq.Evaluator.QueryPhase)
        );
        factories.Add(
            typeof(global::Musoq.Evaluator.IR.Logical.Nodes.SetOpKind),
            static fixture => default(global::Musoq.Evaluator.IR.Logical.Nodes.SetOpKind)
        );
        factories.Add(typeof(ExecutionSetOperationStrategy), static fixture => default(ExecutionSetOperationStrategy));
        factories.Add(
            typeof(global::Musoq.Evaluator.IR.PagingCount),
            static fixture => default(global::Musoq.Evaluator.IR.PagingCount)
        );
        factories.Add(
            typeof(ExecutionColumnMetadata),
            static fixture => new ExecutionColumnMetadata(
                (string)fixture.Create(typeof(string)),
                (IReadOnlyList<ExecutionColumnMetadataField>)
                    fixture.Create(typeof(IReadOnlyList<ExecutionColumnMetadataField>)),
                (ExecutionColumnMetadataKind)fixture.Create(typeof(ExecutionColumnMetadataKind))
            )
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionColumnMetadataField>),
            static fixture =>
                new ExecutionColumnMetadataField[]
                {
                    (ExecutionColumnMetadataField)fixture.Create(typeof(ExecutionColumnMetadataField)),
                    (ExecutionColumnMetadataField)fixture.Create(typeof(ExecutionColumnMetadataField)),
                }
        );
        factories.Add(
            typeof(ExecutionColumnMetadataField),
            static fixture => new ExecutionColumnMetadataField(
                (string)fixture.Create(typeof(string)),
                (int)fixture.Create(typeof(int)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                fixture._includeOptional
                    ? (IReadOnlyDictionary<string, string>)fixture.Create(typeof(IReadOnlyDictionary<string, string>))
                    : null,
                fixture._includeOptional ? (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)) : null,
                fixture._includeOptional
                    ? (global::Musoq.Schema.EnumTypeDescriptor)
                        fixture.Create(typeof(global::Musoq.Schema.EnumTypeDescriptor))
                    : null
            )
        );
        factories.Add(typeof(ExecutionColumnMetadataKind), static fixture => default(ExecutionColumnMetadataKind));
        factories.Add(typeof(ExecutionTopOffsetStrategy), static fixture => default(ExecutionTopOffsetStrategy));
        factories.Add(
            typeof(global::Musoq.Evaluator.IR.Logical.Nodes.DescType),
            static fixture => default(global::Musoq.Evaluator.IR.Logical.Nodes.DescType)
        );
        factories.Add(typeof(global::Musoq.Parser.TextSpan), static fixture => default(global::Musoq.Parser.TextSpan));
        factories.Add(
            typeof(IReadOnlyList<global::Musoq.Evaluator.IR.Bindings.StructuralArgumentDescription>),
            static fixture =>
                new global::Musoq.Evaluator.IR.Bindings.StructuralArgumentDescription[]
                {
                    (global::Musoq.Evaluator.IR.Bindings.StructuralArgumentDescription)
                        fixture.Create(typeof(global::Musoq.Evaluator.IR.Bindings.StructuralArgumentDescription)),
                    (global::Musoq.Evaluator.IR.Bindings.StructuralArgumentDescription)
                        fixture.Create(typeof(global::Musoq.Evaluator.IR.Bindings.StructuralArgumentDescription)),
                }
        );
        factories.Add(
            typeof(global::Musoq.Evaluator.IR.Bindings.StructuralArgumentDescription),
            static fixture => new global::Musoq.Evaluator.IR.Bindings.StructuralArgumentDescription(
                (int)fixture.Create(typeof(int)),
                (string)fixture.Create(typeof(string)),
                (string)fixture.Create(typeof(string)),
                (string)fixture.Create(typeof(string)),
                fixture._includeOptional ? (bool)fixture.Create(typeof(bool)) : null,
                (bool)fixture.Create(typeof(bool)),
                (bool)fixture.Create(typeof(bool)),
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null,
                fixture._includeOptional ? (int)fixture.Create(typeof(int)) : null,
                fixture._includeOptional ? (int)fixture.Create(typeof(int)) : null,
                fixture._includeOptional ? (long)fixture.Create(typeof(long)) : null
            )
        );
        factories.Add(typeof(long), static fixture => default(long));
        factories.Add(typeof(ExecutionEnumerableChunkMode), static fixture => default(ExecutionEnumerableChunkMode));
        factories.Add(
            typeof(global::Musoq.Evaluator.IR.Logical.Nodes.InterpretSourceKind),
            static fixture => default(global::Musoq.Evaluator.IR.Logical.Nodes.InterpretSourceKind)
        );
        factories.Add(
            typeof(global::Musoq.Evaluator.IR.Logical.Nodes.ApplyKind),
            static fixture => default(global::Musoq.Evaluator.IR.Logical.Nodes.ApplyKind)
        );
        factories.Add(
            typeof(ExecutionSourceBinding),
            static fixture => new ExecutionSourceBinding(
                (string)fixture.Create(typeof(string)),
                (string)fixture.Create(typeof(string)),
                (string)fixture.Create(typeof(string)),
                (int)fixture.Create(typeof(int)),
                (IReadOnlyList<ExecutionExpression>)fixture.Create(typeof(IReadOnlyList<ExecutionExpression>)),
                (IReadOnlyList<FieldBinding>)fixture.Create(typeof(IReadOnlyList<FieldBinding>)),
                fixture._includeOptional
                    ? (ExecutionColumnMetadata)fixture.Create(typeof(ExecutionColumnMetadata))
                    : null,
                fixture._includeOptional ? (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)) : null,
                fixture._includeOptional
                    ? (ExecutionQueryRowSourceTransfer)fixture.Create(typeof(ExecutionQueryRowSourceTransfer))
                    : null,
                fixture._includeOptional ? (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)) : null,
                (bool)fixture.Create(typeof(bool)),
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null,
                fixture._includeOptional ? (ExecutionCallableRef)fixture.Create(typeof(ExecutionCallableRef)) : null,
                fixture._includeOptional
                    ? (IReadOnlyList<ExecutionStructuralLimitPlan?>)
                        fixture.Create(typeof(IReadOnlyList<ExecutionStructuralLimitPlan?>))
                    : null
            )
        );
        factories.Add(
            typeof(ExecutionQueryRowSourceTransfer),
            static fixture => new ExecutionQueryRowSourceTransfer(
                ExecutionQueryRowCarrier.ReadonlyStruct,
                new string('A', 64),
                []
            )
        );
        factories.Add(typeof(ExecutionQueryRowCarrier), static fixture => default(ExecutionQueryRowCarrier));
        factories.Add(typeof(ExecutionQueryRowLifetime), static fixture => default(ExecutionQueryRowLifetime));
        factories.Add(
            typeof(IReadOnlyList<ExecutionQueryRowField>),
            static fixture =>
                new ExecutionQueryRowField[]
                {
                    (ExecutionQueryRowField)fixture.Create(typeof(ExecutionQueryRowField)),
                    (ExecutionQueryRowField)fixture.Create(typeof(ExecutionQueryRowField)),
                }
        );
        factories.Add(
            typeof(ExecutionQueryRowField),
            static fixture => new ExecutionQueryRowField(
                (int)fixture.Create(typeof(int)),
                (int)fixture.Create(typeof(int)),
                (string)fixture.Create(typeof(string)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                fixture._includeOptional
                    ? (global::Musoq.Schema.EnumTypeDescriptor)
                        fixture.Create(typeof(global::Musoq.Schema.EnumTypeDescriptor))
                    : null,
                (bool)fixture.Create(typeof(bool)),
                fixture._includeOptional
                    ? (IReadOnlyDictionary<string, string>)fixture.Create(typeof(IReadOnlyDictionary<string, string>))
                    : null
            )
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionStructuralLimitPlan?>),
            static fixture =>
                new ExecutionStructuralLimitPlan?[]
                {
                    ExecutionStructuralLimitPlan.Default,
                    ExecutionStructuralLimitPlan.Default,
                }
        );
        factories.Add(
            typeof(IReadOnlyList<IReadOnlyList<ExecutionRowValue>>),
            static fixture =>
                new IReadOnlyList<ExecutionRowValue>[]
                {
                    (IReadOnlyList<ExecutionRowValue>)fixture.Create(typeof(IReadOnlyList<ExecutionRowValue>)),
                    (IReadOnlyList<ExecutionRowValue>)fixture.Create(typeof(IReadOnlyList<ExecutionRowValue>)),
                }
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionWindowOrderKey>),
            static fixture =>
                new ExecutionWindowOrderKey[]
                {
                    (ExecutionWindowOrderKey)fixture.Create(typeof(ExecutionWindowOrderKey)),
                    (ExecutionWindowOrderKey)fixture.Create(typeof(ExecutionWindowOrderKey)),
                }
        );
        factories.Add(
            typeof(ExecutionWindowOrderKey),
            static fixture => new ExecutionWindowOrderKey(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (bool)fixture.Create(typeof(bool)),
                (global::Musoq.Evaluator.IR.Bindings.NullOrdering)
                    fixture.Create(typeof(global::Musoq.Evaluator.IR.Bindings.NullOrdering))
            )
        );
        factories.Add(typeof(ExecutionOffsetWindowFunction), static fixture => default(ExecutionOffsetWindowFunction));
        factories.Add(
            typeof(ExecutionWindowKeyArray),
            static fixture => new ExecutionWindowKeyArray(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (bool)fixture.Create(typeof(bool)),
                fixture._includeOptional
                    ? (ExecutionWindowKeyShape)fixture.Create(typeof(ExecutionWindowKeyShape))
                    : null,
                (bool)fixture.Create(typeof(bool))
            )
        );
        factories.Add(
            typeof(ExecutionWindowKeyShape),
            static fixture => new ExecutionWindowKeyShape(
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (bool)fixture.Create(typeof(bool)),
                fixture._includeOptional ? (string)fixture.Create(typeof(string)) : null,
                (bool)fixture.Create(typeof(bool)),
                fixture._includeOptional
                    ? (IReadOnlyList<ExecutionWindowGeneratedKeyPart>)
                        fixture.Create(typeof(IReadOnlyList<ExecutionWindowGeneratedKeyPart>))
                    : null
            )
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionWindowGeneratedKeyPart>),
            static fixture =>
                new ExecutionWindowGeneratedKeyPart[]
                {
                    (ExecutionWindowGeneratedKeyPart)fixture.Create(typeof(ExecutionWindowGeneratedKeyPart)),
                    (ExecutionWindowGeneratedKeyPart)fixture.Create(typeof(ExecutionWindowGeneratedKeyPart)),
                }
        );
        factories.Add(
            typeof(ExecutionWindowGeneratedKeyPart),
            static fixture => new ExecutionWindowGeneratedKeyPart(
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (bool)fixture.Create(typeof(bool)),
                (global::Musoq.Evaluator.IR.Bindings.NullOrdering)
                    fixture.Create(typeof(global::Musoq.Evaluator.IR.Bindings.NullOrdering))
            )
        );
        factories.Add(
            typeof(ExecutionWindowPartitionSet),
            static fixture => new ExecutionWindowPartitionSet(
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (bool)fixture.Create(typeof(bool)),
                (bool)fixture.Create(typeof(bool))
            )
        );
        factories.Add(
            typeof(IReadOnlyList<bool>),
            static fixture => new bool[] { (bool)fixture.Create(typeof(bool)), (bool)fixture.Create(typeof(bool)) }
        );
        factories.Add(
            typeof(ExecutionWindowFrame),
            static fixture => new ExecutionWindowFrame(
                (ExecutionWindowFrameKind)fixture.Create(typeof(ExecutionWindowFrameKind)),
                (ExecutionWindowFrameBound)fixture.Create(typeof(ExecutionWindowFrameBound)),
                (ExecutionWindowFrameBound)fixture.Create(typeof(ExecutionWindowFrameBound))
            )
        );
        factories.Add(typeof(ExecutionWindowFrameKind), static fixture => default(ExecutionWindowFrameKind));
        factories.Add(
            typeof(ExecutionWindowFrameBound),
            static fixture => new ExecutionWindowFrameBound(
                (ExecutionWindowFrameBoundKind)fixture.Create(typeof(ExecutionWindowFrameBoundKind)),
                (int)fixture.Create(typeof(int))
            )
        );
        factories.Add(typeof(ExecutionWindowFrameBoundKind), static fixture => default(ExecutionWindowFrameBoundKind));
        factories.Add(
            typeof(IReadOnlyList<ExecutionVariable>),
            static fixture =>
                new ExecutionVariable[]
                {
                    (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                    (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                }
        );
        factories.Add(
            typeof(ExecutionRankingWindowFunction),
            static fixture => default(ExecutionRankingWindowFunction)
        );
        factories.Add(
            typeof(ExecutionWindowAggregateKernelDescriptor),
            static fixture => new ExecutionWindowAggregateKernelDescriptor(
                (ExecutionWindowAggregateFunction)fixture.Create(typeof(ExecutionWindowAggregateFunction)),
                (ExecutionWindowAggregateMode)fixture.Create(typeof(ExecutionWindowAggregateMode)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionWindowAggregateFunction),
            static fixture => default(ExecutionWindowAggregateFunction)
        );
        factories.Add(typeof(ExecutionWindowAggregateMode), static fixture => default(ExecutionWindowAggregateMode));
        factories.Add(
            typeof(ExecutionWindowKernelPlanStrategy),
            static fixture => default(ExecutionWindowKernelPlanStrategy)
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionNode>),
            static fixture =>
                new ExecutionNode[]
                {
                    (ExecutionNode)fixture.Create(typeof(ExecutionNode)),
                    (ExecutionNode)fixture.Create(typeof(ExecutionNode)),
                }
        );
        factories.Add(typeof(ExecutionNode), static fixture => new ExecutionBreak());
        factories.Add(
            typeof(IReadOnlyList<ExecutionCaseWhenBranch>),
            static fixture =>
                new ExecutionCaseWhenBranch[]
                {
                    (ExecutionCaseWhenBranch)fixture.Create(typeof(ExecutionCaseWhenBranch)),
                    (ExecutionCaseWhenBranch)fixture.Create(typeof(ExecutionCaseWhenBranch)),
                }
        );
        factories.Add(
            typeof(ExecutionCaseWhenBranch),
            static fixture => new ExecutionCaseWhenBranch(
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression))
            )
        );
        factories.Add(
            typeof(IEnumerable<ExecutionCteCollectionField>),
            static fixture =>
                new ExecutionCteCollectionField[]
                {
                    (ExecutionCteCollectionField)fixture.Create(typeof(ExecutionCteCollectionField)),
                    (ExecutionCteCollectionField)fixture.Create(typeof(ExecutionCteCollectionField)),
                }
        );
        factories.Add(
            typeof(ExecutionCteCollectionField),
            static fixture => new ExecutionCteCollectionField(
                (string)fixture.Create(typeof(string)),
                (int)fixture.Create(typeof(int)),
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef))
            )
        );
        factories.Add(
            typeof(ExecutionStringMatchComparison),
            static fixture => default(ExecutionStringMatchComparison)
        );
        factories.Add(
            typeof(ExecutionConstantInSet),
            static fixture => new ExecutionConstantInSet(
                (ExecutionTypeRef)fixture.Create(typeof(ExecutionTypeRef)),
                (IEnumerable<ExecutionConstantValue>)fixture.Create(typeof(IEnumerable<ExecutionConstantValue>)),
                (ExecutionConstantInSetKind)fixture.Create(typeof(ExecutionConstantInSetKind))
            )
        );
        factories.Add(
            typeof(IEnumerable<ExecutionConstantValue>),
            static fixture =>
                new ExecutionConstantValue[]
                {
                    (ExecutionConstantValue)fixture.Create(typeof(ExecutionConstantValue)),
                    (ExecutionConstantValue)fixture.Create(typeof(ExecutionConstantValue)),
                }
        );
        factories.Add(
            typeof(ExecutionConstantValue),
            static fixture => ExecutionConstantValue.FromClr(1, ExecutionClrBindingFactory.FromClr(typeof(int)))
        );
        factories.Add(typeof(ExecutionConstantInSetKind), static fixture => default(ExecutionConstantInSetKind));
        factories.Add(
            typeof(global::Musoq.Evaluator.IR.Expressions.PatternKind),
            static fixture => default(global::Musoq.Evaluator.IR.Expressions.PatternKind)
        );
        factories.Add(typeof(ExecutionRowStreamKind), static fixture => default(ExecutionRowStreamKind));
        factories.Add(typeof(ExecutionRowStreamRowsAccess), static fixture => default(ExecutionRowStreamRowsAccess));
        factories.Add(typeof(ExecutionStringMatchKind), static fixture => default(ExecutionStringMatchKind));
        factories.Add(
            typeof(IEnumerable<ExecutionStructuralField>),
            static fixture =>
                new ExecutionStructuralField[]
                {
                    (ExecutionStructuralField)fixture.Create(typeof(ExecutionStructuralField)),
                    (ExecutionStructuralField)fixture.Create(typeof(ExecutionStructuralField)),
                }
        );
        factories.Add(
            typeof(ExecutionStructuralField),
            static fixture => new ExecutionStructuralField(
                (string)fixture.Create(typeof(string)),
                (ExecutionExpression)fixture.Create(typeof(ExecutionExpression)),
                (bool)fixture.Create(typeof(bool))
            )
        );
        factories.Add(
            typeof(global::Musoq.Evaluator.IR.Expressions.SubqueryResultKind),
            static fixture => default(global::Musoq.Evaluator.IR.Expressions.SubqueryResultKind)
        );
        factories.Add(
            typeof(ExecutionPlan),
            static fixture => new ExecutionPlan("plan", [], (ExecutionBlock)fixture.Create(typeof(ExecutionBlock)))
        );
        factories.Add(
            typeof(IReadOnlyList<RowShape>),
            static fixture =>
                new RowShape[]
                {
                    (RowShape)fixture.Create(typeof(RowShape)),
                    (RowShape)fixture.Create(typeof(RowShape)),
                }
        );
        factories.Add(typeof(RowShape), static fixture => new GeneratedRowShape($"shape{++fixture._nextName}", []));
        factories.Add(
            typeof(FinalShapeResult),
            static fixture => new FinalShapeResult(
                (string)fixture.Create(typeof(string)),
                (ExecutionVariable)fixture.Create(typeof(ExecutionVariable)),
                (GeneratedRowShape)fixture.Create(typeof(GeneratedRowShape)),
                (ExecutionColumnMetadata)fixture.Create(typeof(ExecutionColumnMetadata))
            )
        );
        factories.Add(
            typeof(global::Musoq.Targets.Abstractions.ExecutionSemanticsContract),
            static fixture => new global::Musoq.Targets.Abstractions.ExecutionSemanticsContract(
                (int)fixture.Create(typeof(int)),
                (IEnumerable<global::Musoq.Targets.Abstractions.ExecutionSemanticsRule>)
                    fixture.Create(typeof(IEnumerable<global::Musoq.Targets.Abstractions.ExecutionSemanticsRule>))
            )
        );
        factories.Add(
            typeof(IEnumerable<global::Musoq.Targets.Abstractions.ExecutionSemanticsRule>),
            static fixture =>
                new global::Musoq.Targets.Abstractions.ExecutionSemanticsRule[]
                {
                    (global::Musoq.Targets.Abstractions.ExecutionSemanticsRule)
                        fixture.Create(typeof(global::Musoq.Targets.Abstractions.ExecutionSemanticsRule)),
                    (global::Musoq.Targets.Abstractions.ExecutionSemanticsRule)
                        fixture.Create(typeof(global::Musoq.Targets.Abstractions.ExecutionSemanticsRule)),
                }
        );
        factories.Add(
            typeof(global::Musoq.Targets.Abstractions.ExecutionSemanticsRule),
            static fixture => new global::Musoq.Targets.Abstractions.ExecutionSemanticsRule(
                (global::Musoq.Targets.Abstractions.ExecutionSemanticsRuleId)
                    fixture.Create(typeof(global::Musoq.Targets.Abstractions.ExecutionSemanticsRuleId)),
                (string)fixture.Create(typeof(string))
            )
        );
        factories.Add(
            typeof(global::Musoq.Targets.Abstractions.ExecutionSemanticsRuleId),
            static fixture => default(global::Musoq.Targets.Abstractions.ExecutionSemanticsRuleId)
        );
        factories.Add(
            typeof(IEnumerable<ExecutionStoredTableRepresentationPlan>),
            static fixture =>
                new ExecutionStoredTableRepresentationPlan[]
                {
                    (ExecutionStoredTableRepresentationPlan)
                        fixture.Create(typeof(ExecutionStoredTableRepresentationPlan)),
                    (ExecutionStoredTableRepresentationPlan)
                        fixture.Create(typeof(ExecutionStoredTableRepresentationPlan)),
                }
        );
        factories.Add(
            typeof(ExecutionStoredTableRepresentationPlan),
            static fixture => new ExecutionStoredTableRepresentationPlan(
                (int)fixture.Create(typeof(int)),
                (GeneratedRowShape)fixture.Create(typeof(GeneratedRowShape)),
                (ExecutionStoredTableRepresentationKind)fixture.Create(typeof(ExecutionStoredTableRepresentationKind)),
                (ExecutionStructuralOwnershipMode)fixture.Create(typeof(ExecutionStructuralOwnershipMode)),
                (ExecutionStructuralPreparationLifetime)fixture.Create(typeof(ExecutionStructuralPreparationLifetime))
            )
        );
        factories.Add(
            typeof(ExecutionStoredTableRepresentationKind),
            static fixture => default(ExecutionStoredTableRepresentationKind)
        );
        factories.Add(
            typeof(global::Musoq.Evaluator.IR.Expressions.UnaryOpKind),
            static fixture => default(global::Musoq.Evaluator.IR.Expressions.UnaryOpKind)
        );
        factories.Add(
            typeof(IReadOnlyList<ExecutionUnionMemberCase>),
            static fixture =>
                new ExecutionUnionMemberCase[]
                {
                    (ExecutionUnionMemberCase)fixture.Create(typeof(ExecutionUnionMemberCase)),
                    (ExecutionUnionMemberCase)fixture.Create(typeof(ExecutionUnionMemberCase)),
                }
        );
        factories.Add(
            typeof(ExecutionUnionMemberCase),
            static fixture => new ExecutionUnionMemberCase(
                (string)fixture.Create(typeof(string)),
                (string)fixture.Create(typeof(string))
            )
        );
    }
}
