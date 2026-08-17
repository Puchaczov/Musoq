using Musoq.Evaluator;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.InterpretationSchemaDependencyGraph;

namespace Musoq.Converter.Build;

/// <summary>
///     Build chain step that extracts interpretation schema definitions from the query
///     and generates interpreter source code for inclusion in the main assembly.
/// </summary>
public class CompileInterpretationSchemas(BuildChain successor) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var phase = global::Musoq.Converter.EvaluatorPerformanceTelemetry.BeginPhase("interpretation-schema");
        try
        {
            var queryTree = items.RawQueryTree;

            var partition = InterpretationSchemaPartition.Create(queryTree);
            var usedRegistry = partition.HasDefinitions
                ? DeadInterpretationSchemaEliminator.Eliminate(partition.UsageTree, partition.Registry).ResultRegistry
                : partition.Registry;


            if (usedRegistry.Count > 0)
            {
                var sourceCode = GenerateInterpreterSourceCode(usedRegistry);
                items.InterpreterSourceCode = sourceCode;
            }

            if (partition.HasDefinitions)
                items.RawQueryTree = partition.QueryWithoutDefinitions;


            items.SchemaRegistry = usedRegistry;
        }
        finally
        {
            phase.Dispose();
        }

        Successor?.Build(items);
    }

    private static string? GenerateInterpreterSourceCode(SchemaRegistry registry)
    {
        const string interpreterNamespace = "Musoq.Generated.Interpreters";

        var codeGenerator = new InterpreterCodeGenerator(registry);
        var sourceCode = codeGenerator.GenerateAll();

        if (string.IsNullOrWhiteSpace(sourceCode) || !sourceCode.Contains("class", StringComparison.Ordinal))
            return null;


        foreach (var registration in registry.Schemas)
            registration.GeneratedTypeName = $"{interpreterNamespace}.{registration.Name}";

        return sourceCode;
    }
}
