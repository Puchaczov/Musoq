using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class SidecarJoinCteLowerer(
    Func<PhysicalNode, PhysicalNode> unwrapSingleStatement,
    Func<PhysicalNode, SupportedPipeline?> decomposeSupportedPipeline,
    Func<PhysicalMultiStatementNode, MultiStatementIndexes, Dictionary<string, CteReferenceClassification>> classifyMultiStatementCteReferences,
    Func<string, IReadOnlyDictionary<string, CteReferenceClassification>, bool> canFuseReadOnceCte,
    Func<int, MultiStatementIndexes, string?> resolveStatementCteName,
    Func<PhysicalMultiStatementNode, IReadOnlyDictionary<string, int>, IReadOnlyDictionary<string, GeneratedRowShape>, string, MultiStatementIndexes> createMultiStatementIndexes,
    Func<int, IReadOnlyCollection<string>, string> createCteTableName)
{
    public bool TryCreateSingleUseChainStages(
        PhysicalMultiStatementNode multiStatement,
        MultiStatementIndexes indexes,
        out IReadOnlyList<SidecarJoinPipelineStage> stages)
    {
        stages = [];
        if (multiStatement.Statements.Length < 2)
            return false;

        var classifications = classifyMultiStatementCteReferences(multiStatement, indexes);
        var collected = new List<SidecarJoinPipelineStage>(multiStatement.Statements.Length);
        string? previousOutputName = null;

        for (var index = 0; index < multiStatement.Statements.Length; index++)
        {
            var pipeline = decomposeSupportedPipeline(unwrapSingleStatement(multiStatement.Statements[index]));
            if (pipeline == null)
                return false;

            var isFinalStatement = index == multiStatement.Statements.Length - 1;
            string? outputName = null;

            if (!isFinalStatement)
            {
                outputName = resolveStatementCteName(index, indexes);
                if (string.IsNullOrWhiteSpace(outputName) ||
                    !canFuseReadOnceCte(outputName, classifications))
                {
                    return false;
                }
            }

            collected.Add(new SidecarJoinPipelineStage(pipeline, previousOutputName, outputName));
            previousOutputName = outputName;
        }

        stages = collected;
        return true;
    }

    public bool TryCreateReadOnceCteStages(
        PhysicalCteDefinition producer,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        SupportedPipeline finalPipeline,
        out IReadOnlyList<SidecarJoinPipelineStage> stages)
    {
        stages = [];
        var producerPlan = unwrapSingleStatement(producer.Plan);
        var collected = new List<SidecarJoinPipelineStage>();

        if (producerPlan is not PhysicalMultiStatementNode producerMultiStatement)
            return false;

        var producerDefinitionIndex = cteIndexes[producer.Name];
        var producerIndexes = createMultiStatementIndexes(
            producerMultiStatement,
            cteIndexes,
            cteShapesByName,
            createCteTableName(producerDefinitionIndex, cteDefinitionNames));
        string? previousOutputName = null;

        for (var index = 0; index < producerMultiStatement.Statements.Length; index++)
        {
            var pipeline = decomposeSupportedPipeline(unwrapSingleStatement(producerMultiStatement.Statements[index]));
            if (pipeline == null)
                return false;

            var outputName = index == producerMultiStatement.Statements.Length - 1
                ? producer.Name
                : resolveStatementCteName(index, producerIndexes);
            if (string.IsNullOrWhiteSpace(outputName))
                return false;

            collected.Add(new SidecarJoinPipelineStage(pipeline, previousOutputName, outputName));
            previousOutputName = outputName;
        }

        collected.Add(new SidecarJoinPipelineStage(finalPipeline, producer.Name, null));
        stages = collected;
        return true;
    }
}
