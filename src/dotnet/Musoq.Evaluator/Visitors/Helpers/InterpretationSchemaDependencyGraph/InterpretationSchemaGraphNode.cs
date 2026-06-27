using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.InterpretationSchemaDependencyGraph;

/// <summary>
///     Represents a node in interpretation schema dependency graph.
/// </summary>
public sealed class InterpretationSchemaGraphNode(string name, SchemaRegistration registration)
{
    public string Name { get; } = name;

    public SchemaRegistration Registration { get; } = registration;

    public HashSet<string> Dependencies { get; } = [];

    public HashSet<string> Dependents { get; } = [];

    public bool IsReachable { get; set; }

    public Node Node => Registration.Node;
}
