using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static PhysicalNode UnwrapSingleStatement(PhysicalNode physicalPlan)
    {
        if (physicalPlan is PhysicalMultiStatementNode { Statements.Length: 1 } multiStatement)
            return multiStatement.Statements[0];

        return physicalPlan;
    }

    private static CteSupportedPipeline? DecomposeSupportedPipeline(PhysicalNode node)
    {
        var operations = new List<PostOperation>();
        var current = PeelPostOperations(node, operations);

        if (current is not PhysicalProjectNode project)
            return null;

        var source = DecomposeSourcePipeline(project.Input);
        if (source == null)
            return null;

        return new CteSupportedPipeline(project, source.Source, source.Filter, CreatePostOperations(operations, project.Fields));
    }

    private static WindowPipeline? DecomposeWindowPipeline(PhysicalNode node)
    {
        var operations = new List<PostOperation>();
        var current = PeelPostOperations(node, operations);

        switch (current)
        {
            case PhysicalProjectNode { Input: PhysicalWindowNode { Input: PhysicalMaterializeNode materialize } window } project:
                var source = DecomposeWindowSourcePipeline(materialize.Input);
                if (source == null)
                    return null;

                return new WindowPipeline(
                    project,
                    window.Registrations,
                    null,
                    source,
                    CreatePostOperations(operations, project.Fields));
            case PhysicalProjectNode { Input: PhysicalQualifyFilterNode { Input: PhysicalWindowNode { Input: PhysicalMaterializeNode materialize } window } qualify } project:
                var qualifySource = DecomposeWindowSourcePipeline(materialize.Input);
                if (qualifySource == null)
                    return null;

                return new WindowPipeline(
                    project,
                    window.Registrations,
                    qualify.Predicate,
                    qualifySource,
                    CreatePostOperations(operations, project.Fields));
            default:
                return null;
        }
    }

    private static PhysicalNode PeelPostOperations(PhysicalNode node, List<PostOperation> operations)
    {
        var current = node;

        while (TryPeelPostOperation(current, out var operation, out var input))
        {
            operations.Add(operation);
            current = input;
        }

        return current;
    }

    private static bool TryPeelPostOperation(
        PhysicalNode node,
        out PostOperation operation,
        out PhysicalNode input)
    {
        switch (node)
        {
            case PhysicalTopOffsetNode topOffset:
                operation = new TopOffsetOperation(topOffset.Skip, topOffset.Take, topOffset.Keys);
                input = topOffset.Input;
                return true;
            case PhysicalTopNNode topN:
                operation = new TopNOperation(topN.N, topN.Keys);
                input = topN.Input;
                return true;
            case PhysicalTakeNode take:
                operation = new TakeOperation(take.Count);
                input = take.Input;
                return true;
            case PhysicalSkipNode skip:
                operation = new SkipOperation(skip.Count);
                input = skip.Input;
                return true;
            case PhysicalSortNode sort:
                operation = new SortOperation(sort.Keys);
                input = sort.Input;
                return true;
            default:
                operation = null!;
                input = null!;
                return false;
        }
    }
}
