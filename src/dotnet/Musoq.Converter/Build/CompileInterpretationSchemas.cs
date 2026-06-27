using System.Linq;
using Musoq.Evaluator;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.InterpretationSchemaDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

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

        var queryTree = items.RawQueryTree;

        var registry = ExtractSchemaDefinitions(queryTree);
        var eliminationResult = DeadInterpretationSchemaEliminator.Eliminate(queryTree, registry);
        var usedRegistry = eliminationResult.ResultRegistry;


        if (usedRegistry.Schemas.Any())
        {
            var sourceCode = GenerateInterpreterSourceCode(usedRegistry);
            items.InterpreterSourceCode = sourceCode;
        }

        if (registry.Schemas.Any())
            items.RawQueryTree = RemoveSchemaDefinitions(queryTree);


        items.SchemaRegistry = usedRegistry;

        Successor?.Build(items);
    }

    private static RootNode RemoveSchemaDefinitions(RootNode queryTree)
    {
        if (queryTree.Expression is StatementsArrayNode statementsArray)
        {
            var filteredStatements = statementsArray.Statements
                .Where(s => s.Node is not BinarySchemaNode and not TextSchemaNode)
                .ToArray();

            if (filteredStatements.Length == 0) return queryTree;

            if (filteredStatements.Length != statementsArray.Statements.Length)
            {
                var newStatementsArray = new StatementsArrayNode(filteredStatements);
                return new RootNode(newStatementsArray);
            }
        }

        return queryTree;
    }

    private static SchemaRegistry ExtractSchemaDefinitions(RootNode queryTree)
    {
        var registry = new SchemaRegistry();

        if (queryTree.Expression is StatementsArrayNode statementsArray)
        {
            var hasSchemaNodes = false;
            foreach (var statement in statementsArray.Statements)
            {
                if (statement.Node is BinarySchemaNode or TextSchemaNode)
                {
                    hasSchemaNodes = true;
                    break;
                }
            }

            if (!hasSchemaNodes)
                return registry;
        }

        var visitor = new SchemaDefinitionVisitor(registry);
        var traverseVisitor = new SchemaDefinitionTraverseVisitor(visitor);

        traverseVisitor.Walk(queryTree);

        return registry;
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
