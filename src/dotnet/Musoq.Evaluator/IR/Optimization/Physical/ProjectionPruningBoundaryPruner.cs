using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using ColumnUsage = Musoq.Evaluator.IR.Optimization.Physical.PhysicalColumnUsageFacts;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class ProjectionPruningBoundaryPruner(Func<PhysicalNode, PhysicalNode> rewrite)
{
    public int PrunedFields { get; private set; }

    public int RewrittenAggregateInputs { get; private set; }

    public int RewrittenJoinInputs { get; private set; }

    public int RewrittenWindowInputs { get; private set; }

    public int RewrittenSetOperationInputs { get; private set; }

    public PhysicalNode RewriteAggregateOnly(PhysicalAggregateOnlyNode aggregate)
    {
        var input = rewrite(aggregate.Input);
        var requiredNames = ColumnUsage.CollectAggregateRequiredNames(aggregate.Bindings);
        var prunedInput = PruneBoundaryInput(input, requiredNames, RewriteBoundaryKind.Aggregate);

        return ReferenceEquals(prunedInput, aggregate.Input)
            ? aggregate
            : aggregate with { Input = prunedInput };
    }

    public PhysicalNode RewriteSingleKeyAggregate(PhysicalSingleKeyAggregateNode aggregate)
    {
        var input = rewrite(aggregate.Input);
        var requiredNames = ColumnUsage.CollectAggregateRequiredNames([aggregate.GroupKey], aggregate.Bindings);
        var prunedInput = PruneBoundaryInput(input, requiredNames, RewriteBoundaryKind.Aggregate);

        return ReferenceEquals(prunedInput, aggregate.Input)
            ? aggregate
            : aggregate with { Input = prunedInput };
    }

    public PhysicalNode RewriteValueTupleAggregate(PhysicalValueTupleAggregateNode aggregate)
    {
        var input = rewrite(aggregate.Input);
        var requiredNames = ColumnUsage.CollectAggregateRequiredNames(aggregate.GroupKeys, aggregate.Bindings);
        var prunedInput = PruneBoundaryInput(input, requiredNames, RewriteBoundaryKind.Aggregate);

        return ReferenceEquals(prunedInput, aggregate.Input)
            ? aggregate
            : aggregate with { Input = prunedInput };
    }

    public PhysicalNode RewriteSetOperation(PhysicalSetOperationNode setOperation)
    {
        var left = rewrite(setOperation.Left);
        var right = rewrite(setOperation.Right);

        return ReferenceEquals(left, setOperation.Left) && ReferenceEquals(right, setOperation.Right)
            ? setOperation
            : new PhysicalSetOperationNode(
                setOperation.Kind,
                left,
                right,
                setOperation.FieldIndexes,
                setOperation.FieldTypes);
    }

    public PhysicalProjectNode RewriteProjectBoundaries(PhysicalProjectNode project)
    {
        project = RewriteJoinInputProjects(project);
        project = RewriteWindowInputProjects(project);
        return RewriteSetOperationInputProjects(project);
    }

    private PhysicalProjectNode RewriteJoinInputProjects(PhysicalProjectNode project)
    {
        if (!PhysicalProjectionBoundaryClassifier.TryGetPrunableJoin(
                project.Input,
                out var kind,
                out var leftInput,
                out var rightInput,
                out var joinRefs,
                out var createJoin))
        {
            return project;
        }

        var downstreamRefs = ColumnUsage.CollectReferencedColumns(project.Fields);
        var allRefs = downstreamRefs.Concat(joinRefs).ToArray();
        var leftRefs = allRefs;
        var rightRefs = JoinKindSemantics.ProducesLeftOnly(kind)
            ? joinRefs
            : allRefs;
        var leftRequired = ColumnUsage.CollectRequiredNamesForSide(leftInput, leftRefs);
        var rightRequired = ColumnUsage.CollectRequiredNamesForSide(rightInput, rightRefs);
        var left = PruneBoundaryInput(leftInput, leftRequired, RewriteBoundaryKind.JoinInput);
        var right = PruneBoundaryInput(rightInput, rightRequired, RewriteBoundaryKind.JoinInput);

        if (ReferenceEquals(left, leftInput) && ReferenceEquals(right, rightInput))
            return project;

        return new PhysicalProjectNode(
            project.Fields,
            createJoin(left, right))
        {
            IsDistinct = project.IsDistinct
        };
    }

    private PhysicalProjectNode RewriteWindowInputProjects(PhysicalProjectNode project)
    {
        var requiredNames = ColumnUsage.CollectReferencedNames(project.Fields);

        return TryRewriteWindowInput(project.Input, requiredNames, out var rewrittenInput)
            ? new PhysicalProjectNode(project.Fields, rewrittenInput) { IsDistinct = project.IsDistinct }
            : project;
    }

    private PhysicalProjectNode RewriteSetOperationInputProjects(PhysicalProjectNode project)
    {
        if (project.Input is not PhysicalSetOperationNode setOperation)
            return project;

        var requiredNames = ColumnUsage.CollectReferencedNames(project.Fields);
        if (!ColumnUsage.TrySelectSetOperationRetainedIndexes(setOperation, requiredNames, out var retainedIndexes))
            return project;

        var left = PhysicalProjectionBoundaryInputPruner.PruneSetOperationArm(
            setOperation.Left,
            retainedIndexes,
            out var leftPruned);
        var right = PhysicalProjectionBoundaryInputPruner.PruneSetOperationArm(
            setOperation.Right,
            retainedIndexes,
            out var rightPruned);
        if (!leftPruned && !rightPruned)
            return project;

        var indexMap = PhysicalProjectionBoundaryInputPruner.CreateSetOperationIndexMap(retainedIndexes);
        var fieldIndexes = new List<int>();
        var fieldTypes = new List<Type>();

        for (var index = 0; index < setOperation.FieldIndexes.Length; index++)
        {
            var fieldIndex = setOperation.FieldIndexes[index];
            if (!indexMap.TryGetValue(fieldIndex, out var remappedIndex))
                continue;

            fieldIndexes.Add(remappedIndex);
            fieldTypes.Add(setOperation.FieldTypes[index]);
        }

        RewrittenSetOperationInputs++;
        return new PhysicalProjectNode(
            project.Fields,
            new PhysicalSetOperationNode(
                setOperation.Kind,
                left,
                right,
                fieldIndexes.ToArray(),
                fieldTypes.ToArray()))
        {
            IsDistinct = project.IsDistinct
        };
    }

    private bool TryRewriteWindowInput(
        PhysicalNode input,
        HashSet<string> requiredNames,
        out PhysicalNode rewrittenInput)
    {
        if (input is PhysicalQualifyFilterNode qualify)
        {
            var requiredWithQualify = new HashSet<string>(requiredNames, StringComparer.OrdinalIgnoreCase);
            ColumnUsage.AddExpressionColumns(requiredWithQualify, qualify.Predicate);

            if (TryRewriteWindowInput(qualify.Input, requiredWithQualify, out var rewrittenQualifyInput))
            {
                rewrittenInput = new PhysicalQualifyFilterNode(qualify.Predicate, rewrittenQualifyInput);
                return true;
            }

            rewrittenInput = input;
            return false;
        }

        if (input is not PhysicalWindowNode window)
        {
            rewrittenInput = input;
            return false;
        }

        var requiredWithWindow = new HashSet<string>(requiredNames, StringComparer.OrdinalIgnoreCase);
        ColumnUsage.AddWindowRegistrationColumns(requiredWithWindow, window.Registrations);
        var prunedInput = PruneBoundaryInput(window.Input, requiredWithWindow, RewriteBoundaryKind.Window);

        if (ReferenceEquals(prunedInput, window.Input))
        {
            rewrittenInput = input;
            return false;
        }

        rewrittenInput = window with { Input = prunedInput };
        return true;
    }

    private PhysicalNode PruneBoundaryInput(
        PhysicalNode input,
        HashSet<string> requiredNames,
        RewriteBoundaryKind kind)
    {
        var pruned = PhysicalProjectionBoundaryInputPruner.Prune(input, requiredNames);
        if (ReferenceEquals(pruned.Node, input))
            return input;

        PrunedFields += pruned.PrunedFields;
        if (kind == RewriteBoundaryKind.Aggregate)
            RewrittenAggregateInputs++;
        else if (kind == RewriteBoundaryKind.Window)
            RewrittenWindowInputs++;
        else
            RewrittenJoinInputs++;

        return pruned.Node;
    }

    private enum RewriteBoundaryKind
    {
        Aggregate,
        Window,
        JoinInput
    }
}

