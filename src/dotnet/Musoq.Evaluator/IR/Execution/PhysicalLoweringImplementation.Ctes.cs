using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private ExecutionPlanBuildResult BuildCte(
        PhysicalCteNode cte,
        string identifier,
        LoweringScope scope)
    {
        scope = scope.WithScalarSubqueryEmptyResults(
            MergeScalarSubqueryEmptyResults(
                scope.ScalarSubqueryEmptyResults,
                CollectScalarSubqueryEmptyResults(cte)));
        var cteIndexes = CreateCteIndexes(cte);
        var cteDefinitionNames = cte.Definitions.Select(static definition => definition.Name).ToArray();
        var containsRecursiveDefinition = cte.Definitions.Any(static definition =>
            definition.Plan is PhysicalRecursiveCteNode);
        var cteShapesByName = new Dictionary<string, GeneratedRowShape>(StringComparer.OrdinalIgnoreCase);
        var shapes = new List<RowShape>();
        var nodes = new List<ExecutionNode>();
        var schemaFromIndexes = CreateCteDefinitionSchemaFromIndexes(cte);
        var querySchemaFromIndex = schemaFromIndexes.Count == 0
            ? DefaultSchemaFromIndex
            : schemaFromIndexes.Values.Max() + CountSchemaScans(cte.Definitions[^1].Plan);
        var fusedHashBuildSources = containsRecursiveDefinition
            ? new Dictionary<string, FusedCteHashBuildSource>(StringComparer.OrdinalIgnoreCase)
            : TryPlanFusedCteHashBuildSources(
                cte,
                cteDefinitionNames,
                cteIndexes,
                cteShapesByName,
                schemaFromIndexes,
                scope);
        var parallelLevels = fusedHashBuildSources.Count == 0
            ? TryCreateParallelCteLevels(cte)
            : null;
        if (containsRecursiveDefinition && !CanUseParallelLevelsWithRecursiveDefinitions(parallelLevels))
            parallelLevels = null;
        var pruningPlan = (containsRecursiveDefinition || _compilationOptions.UseCteSidecarIndexes)
            ? CreateCteDefinitionPruningPlan(cte)
            : CteDefinitionPruningPlan.Empty;
        var cteReferenceClassifications = !containsRecursiveDefinition && _compilationOptions.UseCteSidecarIndexes
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
            scope);
        if (readOnceProjection != null)
        {
            if (!readOnceProjection.IsBuilt)
                return ExecutionPlanBuildResult.CreateUnsupported(readOnceProjection.UnsupportedReason);

            return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, readOnceProjection));
        }

        var readOnceSidecarJoin = containsRecursiveDefinition
            ? null
            : TryBuildReadOnceCteSidecarJoinTable(
                cte,
                "result",
                "ResultRow0",
                cteDefinitionNames,
                cteIndexes,
                cteShapesByName,
                schemaFromIndexes,
                parallelLevels,
                pruningPlan,
                scope);
        if (readOnceSidecarJoin != null)
        {
            if (!readOnceSidecarJoin.IsBuilt)
                return ExecutionPlanBuildResult.CreateUnsupported(readOnceSidecarJoin.UnsupportedReason);

            return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, readOnceSidecarJoin));
        }

        if (parallelLevels == null)
        {
            for (var index = 0; index < cte.Definitions.Length;)
            {
                var definition = cte.Definitions[index];
                if (definition.Plan is PhysicalRecursiveCteNode recursive)
                {
                    recursive = ApplyRecursiveCteDefinitionPruning(
                        definition.Name, recursive,
                        pruningPlan);
                    var recursiveResult = BuildRecursiveCteDefinitionTable(
                        cte,
                        recursive,
                        index,
                        cteDefinitionNames,
                        cteIndexes,
                        cteShapesByName,
                        schemaFromIndexes[definition.Name],
                        scope);
                    if (!recursiveResult.IsBuilt)
                        return ExecutionPlanBuildResult.CreateUnsupported(recursiveResult.UnsupportedReason);

                    cteShapesByName[definition.Name] = recursiveResult.RowShape;
                    shapes.AddRange(recursiveResult.Shapes);
                    nodes.AddRange(recursiveResult.Nodes);
                    nodes.Add(new ExecutionStoreTable(recursiveResult.Table, index));
                    index++;
                    continue;
                }

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
                    scope,
                    out var siblingUpdatedScope);
                scope = siblingUpdatedScope;
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
                    scope);
                var sidecarSpecs = ExecutionStrategies.GetCteSidecarIndexSpecs(cte, definition.Name);
                result = ApplyCteSidecarOptimizations(
                    definition.Name,
                    sidecarSpecs,
                    cteReferenceClassifications,
                    pruningPlan,
                    result,
                    scope,
                    out var storage,
                    out var sidecarUpdatedScope);
                scope = sidecarUpdatedScope;

                if (!result.IsBuilt)
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
                    var result = BuildSingletonCteLevelDefinition(
                        cte,
                        definition,
                        index,
                        cteDefinitionNames,
                        cteIndexes,
                        cteShapesByName,
                        schemaFromIndexes[definition.Name],
                        pruningPlan,
                        cteReferenceClassifications,
                        scope,
                        out var storeRows,
                        out var singletonUpdatedScope);
                    scope = singletonUpdatedScope;

                    if (!result.IsBuilt)
                        return ExecutionPlanBuildResult.CreateUnsupported(result.UnsupportedReason);

                    cteShapesByName[definition.Name] = result.RowShape;
                    shapes.AddRange(result.Shapes);
                    nodes.AddRange(result.Nodes);
                    if (storeRows)
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
                    scope,
                    out var parallelUpdatedScope);
                scope = parallelUpdatedScope;

                if (!parallelResult.IsBuilt)
                    return ExecutionPlanBuildResult.CreateUnsupported(parallelResult.UnsupportedReason);

                shapes.AddRange(parallelResult.Shapes);
                nodes.AddRange(parallelResult.Nodes);
            }
        }

        var querySession = scope.WithFusedCteHashBuildSources(
            MergeFusedCteHashBuildSources(scope.FusedCteHashBuildSources, fusedHashBuildSources));
        var queryResult = BuildPlanTable(
            cte.Query,
            "result",
            "ResultRow0",
            cteIndexes,
            cteShapesByName,
            querySchemaFromIndex,
            scopeAggregateVariables: true,
            scope: querySession);

        if (!queryResult.IsBuilt)
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
        LoweringScope scope)
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
            scope: scope);

        return _compilationOptions.UseCteSidecarIndexes
            ? ApplyCteRowBufferCapacity(result)
            : result;
    }

}
