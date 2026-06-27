using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record SupportedPipeline(
        PhysicalProjectNode Project,
        PhysicalNode Source,
        PhysicalFilterNode? Filter,
        IReadOnlyList<PostOperation> PostOperations);

    private sealed record SetOperationPipeline(
        PhysicalSetOperationNode SetOperation,
        IReadOnlyList<PostOperation> PostOperations);

    private sealed record StreamingUnionAllArm(
        RowShape SourceShape,
        IReadOnlyList<ExecutionNode> Setup,
        ExecutionSourceLoop Loop);

    private sealed record WindowPipeline(
        PhysicalProjectNode Project,
        WindowRegistration[] Registrations,
        IrExpression? QualifyPredicate,
        SourcePipeline Source,
        IReadOnlyList<PostOperation> PostOperations);

    private sealed record WindowQualifyTopRankPlan(
        IrExpression? Predicate,
        IReadOnlyDictionary<int, long> UpperBounds);

    private sealed record WindowMaterializationContext(
        SourcePipeline SourcePipeline,
        ExecutionExpression SourceRows,
        ExecutionVariable Buffer,
        ExecutionVariable Source,
        RowShape SourceShape,
        ExecutionRowAccessMode RowAccessMode,
        IReadOnlyDictionary<string, RowShape> SourceLookup,
        GeneratedRowShape? GeneratedRowShape);

    private sealed record WindowComputationContext(
        WindowRegistrationBuildResult RegistrationResult,
        ExecutionVariable Buffer,
        ExecutionVariable Item,
        ExecutionRowAccessMode RowAccessMode,
        ExecutionExpression? PartitionKey,
        IReadOnlyList<ExecutionWindowOrderKey> OrderKeys,
        IReadOnlyDictionary<string, RowShape> SourceLookup,
        string ResultTableName,
        WindowResultNameMode ResultNameMode,
        WindowKeyArrayRegistry KeyArrays,
        WindowPartitionSetRegistry Partitions,
        WindowPartitionSetRegistry SortedPartitions,
        string? PartitionSignature,
        string? OrderSignature,
        string PartitionListSignature,
        string? SortedPartitionListSignature,
        IReadOnlySet<string> InPlaceSortableSortedPartitionSignatures,
        IReadOnlySet<string> SingleUsePartitionKeySignatures,
        long? QualifyUpperBound);

    private enum WindowResultNameMode
    {
        Standard,
        IndexedByWindow
    }

    private sealed class WindowKeyArrayRegistry
    {
        private readonly Dictionary<string, ExecutionWindowKeyArray> _arrays = new(StringComparer.Ordinal);

        public ExecutionWindowKeyArray GetOrAdd(
            string signature,
            ExecutionVariable candidate,
            ExecutionWindowKeyShape? shape = null,
            bool shouldMaterialize = true)
        {
            if (_arrays.TryGetValue(signature, out var array))
                return array with { ShouldExtract = false };

            var created = new ExecutionWindowKeyArray(candidate, true, shape, shouldMaterialize);
            _arrays.Add(signature, created);
            return created;
        }
    }

    private sealed class WindowPartitionSetRegistry
    {
        private readonly Dictionary<string, ExecutionVariable> _variables = new(StringComparer.Ordinal);

        public ExecutionWindowPartitionSet GetOrAdd(
            string signature,
            ExecutionVariable candidate,
            bool sortInPlace = false)
        {
            if (_variables.TryGetValue(signature, out var variable))
                return new ExecutionWindowPartitionSet(variable, false, sortInPlace);

            _variables.Add(signature, candidate);
            return new ExecutionWindowPartitionSet(candidate, true, sortInPlace);
        }
    }

    private sealed record SetOperationArmNames(
        string LeftTableName,
        string LeftShapeName,
        string RightTableName,
        string RightShapeName);
}
