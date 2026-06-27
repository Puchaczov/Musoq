using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private List<ExecutionNode> CreateSourceSetup(
        PhysicalNode source,
        RowShape sourceShape,
        ExecutionVariable sourceVariable,
        int schemaFromIndex,
        IReadOnlyDictionary<string, int> cteIndexes,
        string? sourceRowsScope = null,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName = null)
    {
        return CreateSourceSetup(
            source,
            sourceShape,
            sourceVariable,
            schemaFromIndex,
            new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
            cteIndexes,
            sourceRowsScope,
            cteShapesByName);
    }

    private List<ExecutionNode> CreateSourceSetup(
        PhysicalNode source,
        RowShape sourceShape,
        ExecutionVariable sourceVariable,
        int schemaFromIndex,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, int> cteIndexes,
        string? sourceRowsScope = null,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName = null)
    {
        GuardSourceBoundaryStrategy(source);

        if (source is not PhysicalSchemaScanNode scan)
        {
            return source switch
            {
                PhysicalInterpretSourceNode interpret => CreateInterpretSourceSetup(interpret, sourceLookup, cteIndexes, sourceRowsScope),
                PhysicalPropertySourceNode property => CreateEnumerableSourceSetup(
                    property.Alias,
                    property.ResultType,
                    CreatePropertySourceExpression(property, sourceLookup),
                    CreateEnumerableChunkMode(sourceShape),
                    sourceRowsScope,
                    ResolvePropertyEnumerableTypeName(property)),
                PhysicalAccessMethodSourceNode accessMethod => CreateEnumerableSourceSetup(
                    accessMethod.Alias,
                    accessMethod.ResultType,
                    ExecutionExpressionConverter.Convert(accessMethod.MethodCallExpression, sourceLookup),
                    CreateEnumerableChunkMode(sourceShape),
                    sourceRowsScope),
                PhysicalValuesScanNode values => CreateValuesSourceSetup(values, sourceShape, sourceRowsScope),
                _ => []
            };
        }

        var sourceRows = new ExecutionVariable(CreateSourceRowsName(scan.Alias, sourceRowsScope), typeof(object));
        return
        [
            new ExecutionSourceScan(
                sourceVariable,
                sourceRows,
                CreateSourceBinding(scan, sourceShape, schemaFromIndex, sourceLookup, cteIndexes))
        ];
    }

    private void GuardSourceBoundaryStrategy(PhysicalNode source)
    {
        var strategy = ResolveSourceBoundaryStrategy(source);
        if (strategy == null || strategy.CachingDecision == SourceBoundaryCachingDecision.NotApplied)
            return;

        throw new InvalidOperationException($"Source boundary strategy {strategy.BoundaryId} requested unsupported source caching.");
    }

    private SourceBoundaryStrategyPlan? ResolveSourceBoundaryStrategy(PhysicalNode source)
    {
        var boundaryId = source switch
        {
            PhysicalInterpretSourceNode interpret => $"interpret:{interpret.Alias}",
            PhysicalPropertySourceNode property => $"property:{property.Alias}",
            PhysicalAccessMethodSourceNode accessMethod => $"access:{accessMethod.Alias}",
            _ => null
        };

        return boundaryId == null ? null : ExecutionStrategies.GetSourceBoundaryStrategy(boundaryId);
    }
}
