using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SingleUseMaterializationPlanner
{
    public static IReadOnlyList<PlanningDecision> Plan(PhysicalNode physicalPlan)
    {
        var decisions = new List<PlanningDecision>();
        Visit(physicalPlan, decisions);
        return decisions;
    }

    private static void Visit(
        PhysicalNode node,
        List<PlanningDecision> decisions)
    {
        if (node is PhysicalCteNode cte)
        {
            AddCteCandidates(cte, decisions);

            foreach (var definition in cte.Definitions)
                Visit(definition.Plan, decisions);
            Visit(cte.Query, decisions);
            return;
        }

        if (node is PhysicalMultiStatementNode multiStatement)
            AddMultiStatementCandidates(multiStatement, decisions);

        foreach (var child in node.Children)
            Visit(child, decisions);
    }

    private static void AddCteCandidates(
        PhysicalCteNode cte,
        List<PlanningDecision> decisions)
    {
        var trackedNames = cte.Definitions
            .Select(static definition => definition.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var referenceCounts = CountTrackedCteReferences(
            cte.Children,
            trackedNames);
        var consumers = CollectConsumers(cte.Children, trackedNames);

        foreach (var definition in cte.Definitions)
        {
            if (!TryCreateCandidate(
                    $"cte:{definition.Name}",
                    referenceCounts.GetValueOrDefault(definition.Name),
                    ClassifyOutput(ExecutionStrategyPipelineDecomposer.UnwrapSingleStatement(definition.Plan)),
                    consumers.GetValueOrDefault(definition.Name),
                    out var decision))
            {
                continue;
            }

            decisions.Add(decision);
        }
    }

    private static void AddMultiStatementCandidates(
        PhysicalMultiStatementNode multiStatement,
        List<PlanningDecision> decisions)
    {
        var producerIndexByName = IntermediateSourceProducers.Resolve(multiStatement);
        if (producerIndexByName.Count == 0)
            return;

        var trackedNames = producerIndexByName.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var referenceCounts = CountTrackedCteReferences(multiStatement.Statements, trackedNames);
        var consumers = CollectConsumers(multiStatement.Statements, trackedNames);

        foreach (var (name, producerIndex) in producerIndexByName)
        {
            if (producerIndex < 0 || producerIndex >= multiStatement.Statements.Length)
                continue;

            if (!TryCreateCandidate(
                    $"statement:{name}",
                    referenceCounts.GetValueOrDefault(name),
                    ClassifyOutput(ExecutionStrategyPipelineDecomposer.UnwrapSingleStatement(multiStatement.Statements[producerIndex])),
                    consumers.GetValueOrDefault(name),
                    out var decision))
            {
                continue;
            }

            decisions.Add(decision);
        }
    }

    private static bool TryCreateCandidate(
        string target,
        int referenceCount,
        CteOutputCharacteristics characteristics,
        IReadOnlyList<SingleUseConsumerKind>? consumers,
        out PlanningDecision decision)
    {
        decision = null!;

        if (referenceCount != 1 ||
            consumers is not { Count: 1 } ||
            !CanPlanSingleUseFusion(characteristics))
        {
            return false;
        }

        var consumer = consumers[0];
        var ruleName = consumer switch
        {
            SingleUseConsumerKind.HashBuild => "SingleUseHashBuildFusion",
            SingleUseConsumerKind.FinalProjection or SingleUseConsumerKind.ProjectionChain => "SingleUseProjectionFusion",
            _ => null
        };

        if (ruleName == null)
            return false;

        decision = new PlanningDecision(
            PlanningDecisionCategory.Materialization,
            ruleName,
            target,
            "Candidate",
            PlanningConfidence.High,
            CreateCandidateReason(consumer, characteristics));
        return true;
    }

    private static string CreateCandidateReason(SingleUseConsumerKind consumer, CteOutputCharacteristics characteristics)
    {
        var consumerText = consumer switch
        {
            SingleUseConsumerKind.HashBuild => "a hash-build boundary",
            SingleUseConsumerKind.FinalProjection => "the final projection",
            SingleUseConsumerKind.ProjectionChain => "another single-use projection/filter stage",
            _ => "a supported consumer"
        };

        return string.Format(CultureInfo.InvariantCulture, "Single-use stage is consumed only by {0}; characteristics: {1}.", consumerText, characteristics == CteOutputCharacteristics.None ? "None" : characteristics);
    }

    private static bool CanPlanSingleUseFusion(CteOutputCharacteristics characteristics)
    {
        return !characteristics.HasFlag(CteOutputCharacteristics.OrderSensitive) &&
               !characteristics.HasFlag(CteOutputCharacteristics.Window) &&
               !characteristics.HasFlag(CteOutputCharacteristics.SetOperation) &&
               !characteristics.HasFlag(CteOutputCharacteristics.SideEffectSensitive);
    }

    private static CteOutputCharacteristics ClassifyOutput(PhysicalNode node)
    {
        var characteristics = CteOutputCharacteristics.None;
        ClassifyOutput(node, ref characteristics);
        return characteristics;
    }

    private static void ClassifyOutput(
        PhysicalNode node,
        ref CteOutputCharacteristics characteristics)
    {
        characteristics |= node switch
        {
            PhysicalSortNode or PhysicalSkipNode or PhysicalTakeNode or PhysicalTopNNode or PhysicalTopOffsetNode => CteOutputCharacteristics.OrderSensitive,
            PhysicalAggregateOnlyNode or PhysicalSingleKeyAggregateNode or PhysicalValueTupleAggregateNode => CteOutputCharacteristics.Aggregate,
            PhysicalWindowNode => CteOutputCharacteristics.Window,
            PhysicalSetOperationNode => CteOutputCharacteristics.SetOperation,
            PhysicalInterpretSourceNode or PhysicalAccessMethodSourceNode or PhysicalPropertySourceNode => CteOutputCharacteristics.SideEffectSensitive,
            _ => CteOutputCharacteristics.None
        };

        foreach (var child in node.Children)
            ClassifyOutput(child, ref characteristics);
    }

    private enum SingleUseConsumerKind
    {
        HashBuild,
        FinalProjection,
        ProjectionChain
    }
}
