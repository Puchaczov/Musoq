using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Converter.Build;

/// <summary>
///     The single top-level partition used by interpretation-schema compilation.
///     The executable tree is intentionally shallow: it reuses the already parsed
///     statement nodes and exists only to keep declarations out of reachability scans.
/// </summary>
internal sealed class InterpretationSchemaPartition
{
    private InterpretationSchemaPartition(
        SchemaRegistry registry,
        RootNode usageTree,
        RootNode queryWithoutDefinitions,
        bool hasDefinitions)
    {
        Registry = registry;
        UsageTree = usageTree;
        QueryWithoutDefinitions = queryWithoutDefinitions;
        HasDefinitions = hasDefinitions;
    }

    public SchemaRegistry Registry { get; }

    public RootNode UsageTree { get; }

    public RootNode QueryWithoutDefinitions { get; }

    public bool HasDefinitions { get; }

    public static InterpretationSchemaPartition Create(RootNode queryTree)
    {
        ArgumentNullException.ThrowIfNull(queryTree);

        if (queryTree.Expression is not StatementsArrayNode statementsArray)
            return Empty(queryTree);

        var executableStatements = new List<StatementNode>(statementsArray.Statements.Length);
        var registry = new SchemaRegistry();
        SchemaDefinitionVisitor? visitor = null;
        var hasDefinitions = false;

        foreach (var statement in statementsArray.Statements)
        {
            if (statement.Node is BinarySchemaNode or TextSchemaNode)
            {
                hasDefinitions = true;
                visitor ??= new SchemaDefinitionVisitor(registry);
                statement.Node.Accept(visitor);
            }
            else
                executableStatements.Add(statement);
        }

        if (!hasDefinitions)
            return Empty(queryTree);

        var executableTree = CreateStatementsRoot(
            executableStatements.ToArray(),
            queryTree,
            statementsArray);

        return new InterpretationSchemaPartition(
            registry,
            executableTree,
            executableStatements.Count == 0 ? queryTree : executableTree,
            true);
    }

    private static InterpretationSchemaPartition Empty(RootNode queryTree)
    {
        return new InterpretationSchemaPartition(
            new SchemaRegistry(),
            queryTree,
            queryTree,
            false);
    }

    private static RootNode CreateStatementsRoot(
        StatementNode[] statements,
        RootNode sourceRoot,
        StatementsArrayNode sourceStatements)
    {
        var filteredStatements = new StatementsArrayNode(statements).CopySpansFrom(sourceStatements);
        return new RootNode(filteredStatements).CopySpansFrom(sourceRoot);
    }
}
