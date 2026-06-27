using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Visitors.Helpers.InterpretationSchemaDependencyGraph;

/// <summary>
///     Represents interpretation schema dependency graph and reachability from query usage roots.
/// </summary>
public sealed class InterpretationSchemaDependencyGraph
{
    private readonly Dictionary<string, InterpretationSchemaGraphNode> _nodes;
    private readonly List<InterpretationSchemaGraphNode> _deadSchemas;

    public InterpretationSchemaDependencyGraph(
        Dictionary<string, InterpretationSchemaGraphNode> nodes,
        HashSet<string> directlyUsedSchemaNames)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        _nodes = nodes;
        DirectlyUsedSchemaNames = directlyUsedSchemaNames;
        _deadSchemas = nodes.Values.Where(node => !node.IsReachable).ToList();
    }

    public IReadOnlyDictionary<string, InterpretationSchemaGraphNode> Nodes => _nodes;

    public IReadOnlySet<string> DirectlyUsedSchemaNames { get; }

    public IReadOnlyList<InterpretationSchemaGraphNode> DeadSchemas => _deadSchemas;

    public IReadOnlyList<InterpretationSchemaGraphNode> ReachableSchemas =>
        _nodes.Values.Where(node => node.IsReachable).ToList();

    public bool HasDeadSchemas => _deadSchemas.Count > 0;

    public int SchemaCount => _nodes.Count;
}
