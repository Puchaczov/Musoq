using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private CteDefinitionPruningPlan CreateCteDefinitionPruningPlan(PhysicalCteNode cte)
    {
        var names = cte.Definitions.Select(static definition => definition.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var required = names.ToDictionary(static name => name, static _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        var outputSchemas = cte.Definitions.ToDictionary(static definition => definition.Name, static definition => definition.Plan.OutputSchema, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in cte.Definitions)
        {
            if (definition.Plan is not PhysicalRecursiveCteNode)
            {
                CollectRequiredCteColumns(definition.Plan, names, outputSchemas, required);
                continue;
            }

            var local = names.ToDictionary(
                static name => name,
                static _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
            CollectRequiredCteColumns(definition.Plan, names, outputSchemas, local);
            foreach (var (name, columns) in local)
            {
                if (!string.Equals(name, definition.Name, StringComparison.OrdinalIgnoreCase))
                    required[name].UnionWith(columns);
            }
        }

        CollectRequiredCteColumns(cte.Query, names, outputSchemas, required);
        AddSidecarRequiredColumns(cte, required);
        AddRecursiveCteRequiredColumns(cte, required);

        var compact = required
            .Where(static pair => pair.Value.Count > 0)
            .ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlySet<string>)pair.Value,
                StringComparer.OrdinalIgnoreCase);

        var contextFreeDefinitions = CreateContextFreeCteDefinitions(cte);
        contextFreeDefinitions.ExceptWith(CollectContextRequiredCteDefinitions(cte, names));

        return new CteDefinitionPruningPlan(compact, contextFreeDefinitions);
    }

    private void AddSidecarRequiredColumns(
        PhysicalCteNode cte,
        IReadOnlyDictionary<string, HashSet<string>> required)
    {
        foreach (var definition in cte.Definitions)
        foreach (var spec in ExecutionStrategies.GetCteSidecarIndexSpecs(cte, definition.Name))
        {
            foreach (var column in spec.KeyColumns)
                required[definition.Name].Add(column);
        }
    }

    private HashSet<string> CreateContextFreeCteDefinitions(PhysicalCteNode cte)
    {
        var classifications = ClassifyCteReferences(cte);
        var contextFree = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in cte.Definitions)
        {
            var specs = ExecutionStrategies.GetCteSidecarIndexSpecs(cte, definition.Name);
            if (specs.Count == 0 ||
                specs.Any(static spec => spec.Kind != CteSidecarIndexKind.KeySet) ||
                !classifications.TryGetValue(definition.Name, out var classification) ||
                classification.ReferenceCount != specs.Count)
            {
                continue;
            }

            contextFree.Add(definition.Name);
        }

        return contextFree;
    }

    private static PhysicalCteDefinition ApplyCteDefinitionPruning(
        PhysicalCteDefinition definition,
        CteDefinitionPruningPlan pruningPlan)
    {
        if (!pruningPlan.TryGetRequiredColumns(definition.Name, out var required) || required.Count == 0)
            return definition;

        var plan = UnwrapSingleStatement(definition.Plan);
        if (plan is not PhysicalProjectNode project || project.IsDistinct)
            return definition;

        var fields = project.Fields
            .Where(field => IsRequiredOutputField(field.OutputName, required))
            .Select(static (field, index) => field with { OutputIndex = index })
            .ToArray();

        if (fields.Length == 0 || fields.Length == project.Fields.Length)
            return definition;

        var input = PruneProjectInput(project.Input, fields.Select(static field => field.Expression).ToArray());
        var pruned = new PhysicalProjectNode(fields, input) { IsDistinct = project.IsDistinct };
        PhysicalNode prunedPlan = definition.Plan is PhysicalMultiStatementNode { Statements.Length: 1 }
            ? new PhysicalMultiStatementNode([pruned])
            : pruned;

        return definition with { Plan = prunedPlan };
    }

    private static bool IsRequiredOutputField(string outputName, IReadOnlySet<string> required)
    {
        return required.Contains(outputName) || required.Contains(GetColumnRoot(outputName));
    }
}
