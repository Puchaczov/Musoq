using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private ExecutionPlanBuildResult BuildCte(
        PhysicalCteNode cte,
        string identifier,
        PhysicalToExecutionLoweringSession session)
    {
        var cteIndexes = CreateCteIndexes(cte);
        var cteDefinitionNames = cte.Definitions.Select(static definition => definition.Name).ToArray();
        var cteShapesByName = new Dictionary<string, GeneratedRowShape>(StringComparer.OrdinalIgnoreCase);
        var shapes = new List<RowShape>();
        var nodes = new List<ExecutionNode>();
        var schemaFromIndexes = CreateCteDefinitionSchemaFromIndexes(cte);
        var querySchemaFromIndex = schemaFromIndexes.Count == 0
            ? DefaultSchemaFromIndex
            : schemaFromIndexes.Values.Max() + CountSchemaScans(cte.Definitions[^1].Plan);
        var fusedHashBuildSources = TryPlanFusedCteHashBuildSources(
            cte,
            cteDefinitionNames,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes);
        var parallelLevels = fusedHashBuildSources.Count == 0 ? TryCreateParallelCteLevels(cte) : null;
        var pruningPlan = _compilationOptions.UseCteSidecarIndexes
            ? CreateCteDefinitionPruningPlan(cte)
            : CteDefinitionPruningPlan.Empty;
        var cteReferenceClassifications = _compilationOptions.UseCteSidecarIndexes
            ? ClassifyCteReferences(cte)
            : new Dictionary<string, CteReferenceClassification>(StringComparer.OrdinalIgnoreCase);
        var readOnceProjection = TryBuildReadOnceCteProjectionTable(
            cte,
            "result",
            "ResultRow0",
            cteIndexes,
            cteDefinitionNames,
            cteShapesByName,
            schemaFromIndexes,
            parallelLevels,
            pruningPlan,
            session);
        if (readOnceProjection != null)
        {
            if (!readOnceProjection.Supported)
                return ExecutionPlanBuildResult.CreateUnsupported(readOnceProjection.UnsupportedReason);

            return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, readOnceProjection));
        }

        var readOnceSidecarJoin = TryBuildReadOnceCteSidecarJoinTable(
            cte,
            "result",
            "ResultRow0",
            cteDefinitionNames,
            cteIndexes,
            cteShapesByName,
            schemaFromIndexes,
            parallelLevels,
            pruningPlan,
            session);
        if (readOnceSidecarJoin != null)
        {
            if (!readOnceSidecarJoin.Supported)
                return ExecutionPlanBuildResult.CreateUnsupported(readOnceSidecarJoin.UnsupportedReason);

            return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, readOnceSidecarJoin));
        }

        if (parallelLevels == null)
        {
            for (var index = 0; index < cte.Definitions.Length;)
            {
                var definition = cte.Definitions[index];
                if (fusedHashBuildSources.TryGetValue(definition.Name, out var fused))
                {
                    var fusedSidecarSpecs = ExecutionStrategies.GetCteSidecarIndexSpecs(cte, definition.Name);
                    if (fusedSidecarSpecs.Count > 0)
                    {
                        var slots = string.Join(", ", fusedSidecarSpecs.Select(static spec => spec.IndexSlot.ToString(CultureInfo.InvariantCulture)));
                        return ExecutionPlanBuildResult.CreateUnsupported(
                            $"Execution IR CTE sidecar lowering cannot silently drop planner-selected sidecar index slot(s) [{slots}] for fused hash-build CTE '{definition.Name}'.");
                    }

                    cteShapesByName[definition.Name] = fused.RowShape;
                    nodes.Add(CreateSingleUsePipelineFusionCandidate(index, []));
                    index++;
                    continue;
                }

                var siblingFusion = TryBuildFusedSiblingCteProducers(
                    cte,
                    index,
                    cte.Definitions.Length,
                    cteDefinitionNames,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndexes,
                    pruningPlan,
                    cteReferenceClassifications,
                    fusedHashBuildSources,
                    session);
                if (siblingFusion != null)
                {
                    foreach (var (name, rowShape) in siblingFusion.RowShapesByName)
                        cteShapesByName[name] = rowShape;

                    var producer = CteSourceBackedSiblingFusion.TryRewrite(
                        nodes,
                        shapes,
                        cteIndexes,
                        cteReferenceClassifications,
                        SourceInteractionPlans,
                        siblingFusion.Producer) ?? siblingFusion.Producer;
                    shapes.AddRange(siblingFusion.Shapes);
                    nodes.Add(CreateCteFusedProducerCandidate(producer));
                    index += siblingFusion.DefinitionCount;
                    continue;
                }

                var result = BuildCteDefinitionTable(
                    definition,
                    index,
                    cteDefinitionNames,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndexes[definition.Name],
                    pruningPlan,
                    session);
                var sidecarSpecs = ExecutionStrategies.GetCteSidecarIndexSpecs(cte, definition.Name);
                result = ApplyCteSidecarOptimizations(
                    definition.Name,
                    sidecarSpecs,
                    cteReferenceClassifications,
                    pruningPlan,
                    result,
                    session,
                    out var storage);

                if (!result.Supported)
                    return ExecutionPlanBuildResult.CreateUnsupported(result.UnsupportedReason);

                cteShapesByName[definition.Name] = result.RowShape;
                shapes.AddRange(result.Shapes);
                nodes.AddRange(result.Nodes);
                if (storage.StoreRows)
                    nodes.Add(new ExecutionStoreTable(result.Table, index));
                index++;
            }
        }
        else
        {
            foreach (var level in parallelLevels)
            {
                if (level.Definitions.Count == 1)
                {
                    var definition = level.Definitions[0];
                    var index = cteIndexes[definition.Name];
                    var result = BuildCteDefinitionTable(
                        definition,
                        index,
                        cteDefinitionNames,
                        cteIndexes,
                        cteShapesByName,
                        schemaFromIndexes[definition.Name],
                        pruningPlan,
                        session);
                    var sidecarSpecs = ExecutionStrategies.GetCteSidecarIndexSpecs(cte, definition.Name);
                    result = ApplyCteSidecarOptimizations(
                        definition.Name,
                        sidecarSpecs,
                        cteReferenceClassifications,
                        pruningPlan,
                        result,
                        session,
                        out var storage);

                    if (!result.Supported)
                        return ExecutionPlanBuildResult.CreateUnsupported(result.UnsupportedReason);

                    cteShapesByName[definition.Name] = result.RowShape;
                    shapes.AddRange(result.Shapes);
                    nodes.AddRange(result.Nodes);
                    if (storage.StoreRows)
                        nodes.Add(new ExecutionStoreTable(result.Table, index));
                    continue;
                }

                var parallelResult = BuildParallelCteLevel(
                    cte,
                    level,
                    identifier,
                    cteDefinitionNames,
                    cteIndexes,
                    cteShapesByName,
                    schemaFromIndexes,
                    pruningPlan,
                    cteReferenceClassifications,
                    session);

                if (!parallelResult.Supported)
                    return ExecutionPlanBuildResult.CreateUnsupported(parallelResult.UnsupportedReason);

                shapes.AddRange(parallelResult.Shapes);
                nodes.AddRange(parallelResult.Nodes);
            }
        }

        var querySession = session.WithFusedCteHashBuildSources(
            MergeFusedCteHashBuildSources(session.FusedCteHashBuildSources, fusedHashBuildSources));
        var queryResult = BuildPlanTable(
            cte.Query,
            "result",
            "ResultRow0",
            cteIndexes,
            cteShapesByName,
            querySchemaFromIndex,
            scopeAggregateVariables: true,
            session: querySession);

        if (!queryResult.Supported)
            return ExecutionPlanBuildResult.CreateUnsupported(queryResult.UnsupportedReason);

        shapes.AddRange(queryResult.Shapes);
        nodes.AddRange(queryResult.Nodes);
        nodes.Add(new ExecutionReturnTable(queryResult.Table));

        return ExecutionPlanBuildResult.CreateSupported(new ExecutionPlan(
            identifier,
            shapes,
            new ExecutionBlock(nodes),
            queryResult.FinalResult));
    }

    private TableBuildResult BuildCteDefinitionTable(
        PhysicalCteDefinition definition,
        int index,
        IReadOnlyCollection<string> cteDefinitionNames,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape> cteShapesByName,
        int schemaFromIndex,
        CteDefinitionPruningPlan pruningPlan,
        PhysicalToExecutionLoweringSession session)
    {
        definition = ApplyCteDefinitionPruning(definition, pruningPlan);
        var cteName = CreateCteTableName(index, cteDefinitionNames);
        var result = BuildPlanTable(
            definition.Plan,
            cteName,
            $"Cte{index.ToString(CultureInfo.InvariantCulture)}Row0",
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scopeAggregateVariables: true,
            session: session);

        return _compilationOptions.UseCteSidecarIndexes
            ? ApplyCteRowBufferCapacity(result)
            : result;
    }

}
