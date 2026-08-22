using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
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
