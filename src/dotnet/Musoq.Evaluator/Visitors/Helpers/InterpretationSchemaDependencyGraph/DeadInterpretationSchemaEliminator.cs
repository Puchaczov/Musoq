using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.InterpretationSchemaDependencyGraph;

/// <summary>
///     Eliminates dead (unused) interpretation schemas from schema registry.
/// </summary>
public static class DeadInterpretationSchemaEliminator
{
    public static EliminationResult Eliminate(RootNode queryTree, SchemaRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var graph = Analyze(queryTree, registry);

        if (!graph.HasDeadSchemas)
            return new EliminationResult
            {
                ResultRegistry = registry,
                WereSchemasEliminated = false,
                AllSchemasEliminated = false,
                EliminatedCount = 0,
                Graph = graph
            };

        var prunedRegistry = new SchemaRegistry();

        foreach (var registration in registry.Schemas)
            if (graph.Nodes.TryGetValue(registration.Name, out var node) && node.IsReachable)
                prunedRegistry.Register(registration.Name, registration.Node);

        return new EliminationResult
        {
            ResultRegistry = prunedRegistry,
            WereSchemasEliminated = true,
            AllSchemasEliminated = prunedRegistry.Count == 0,
            EliminatedCount = graph.DeadSchemas.Count,
            Graph = graph
        };
    }

    public static InterpretationSchemaDependencyGraph Analyze(RootNode queryTree, SchemaRegistry registry)
    {
        var builder = new InterpretationSchemaDependencyGraphBuilder();
        return builder.Build(queryTree, registry);
    }

    public readonly record struct EliminationResult
    {
        public SchemaRegistry ResultRegistry { get; init; }

        public bool WereSchemasEliminated { get; init; }

        public bool AllSchemasEliminated { get; init; }

        public int EliminatedCount { get; init; }

        public InterpretationSchemaDependencyGraph? Graph { get; init; }
    }
}
