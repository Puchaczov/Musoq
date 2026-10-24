using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.IR.Planning;

internal sealed class ParallelSourcePermissionPolicy
{
    private readonly IReadOnlyDictionary<string, SourceDescriptor> _descriptors;
    private readonly Dictionary<string, PhysicalNode[]> _definitions;

    public ParallelSourcePermissionPolicy(PhysicalNode plan,
        IReadOnlyDictionary<string, SourceDescriptor>? descriptors)
    {
        _descriptors = descriptors ?? new Dictionary<string, SourceDescriptor>();
        _definitions = Flatten(plan).OfType<PhysicalCteNode>().SelectMany(static node => node.Definitions)
            .GroupBy(static definition => definition.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.Select(definition => definition.Plan).ToArray(),
                StringComparer.OrdinalIgnoreCase);
        foreach (var statements in Flatten(plan).OfType<PhysicalMultiStatementNode>())
        {
            foreach (var producer in IntermediateSourceProducers.Resolve(statements))
            {
                if (producer.Value >= statements.Statements.Length)
                    continue;
                var input = statements.Statements[producer.Value];
                if (_definitions.TryGetValue(producer.Key, out var existing))
                {
                    _definitions[producer.Key] = [..existing, input];
                    continue;
                }
                _definitions[producer.Key] = [input];
            }
        }
    }

    public ParallelEligibilityCheck Check(PhysicalNode source, SourceExecutionPermissions required)
        => Check(source, required, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private ParallelEligibilityCheck Check(PhysicalNode source, SourceExecutionPermissions required, HashSet<string> references)
    {
        if (source is PhysicalSchemaScanNode scan)
        {
            if (scan.SourceContextId is null || !_descriptors.TryGetValue(scan.SourceContextId, out var descriptor) ||
                (descriptor.ExecutionPermissions & required) != required)
                return ParallelEligibilityCheck.Skipped($"Source '{scan.Alias}' does not explicitly permit {required}.");
            return ParallelEligibilityCheck.Enabled;
        }
        if (source is PhysicalCteRefNode reference)
        {
            if (!references.Add(reference.CteName) || !_definitions.TryGetValue(reference.CteName, out var definitions))
                return ParallelEligibilityCheck.Skipped($"CTE '{reference.CteName}' has unresolved scheduling permissions.");
            foreach (var definition in definitions)
            {
                var permission = Check(definition, required, references);
                if (!permission.IsEligible)
                    return permission;
            }
            references.Remove(reference.CteName);
            return ParallelEligibilityCheck.Enabled;
        }
        if (source.Children.Count == 0 && source is not PhysicalValuesScanNode)
            return ParallelEligibilityCheck.Skipped($"Source operation '{source.GetType().Name}' has no declared scheduling permissions.");
        foreach (var child in source.Children)
        {
            var permission = Check(child, required, references);
            if (!permission.IsEligible)
                return permission;
        }
        return ParallelEligibilityCheck.Enabled;
    }

    private static IEnumerable<PhysicalNode> Flatten(PhysicalNode node)
    {
        yield return node;
        foreach (var child in node.Children)
        foreach (var descendant in Flatten(child))
            yield return descendant;
    }
}
