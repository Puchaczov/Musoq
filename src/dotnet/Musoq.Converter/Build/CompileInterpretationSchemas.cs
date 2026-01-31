using System;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
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
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        var queryTree = items.RawQueryTree;
        if (queryTree == null)
        {
            Successor?.Build(items);
            return;
        }


        var registry = ExtractSchemaDefinitions(queryTree);


        if (registry.Schemas.Any())
        {
            var sourceCode = GenerateInterpreterSourceCode(registry);
            items.InterpreterSourceCode = sourceCode;

            items.RawQueryTree = RemoveSchemaDefinitions(queryTree);
        }


        items.SchemaRegistry = registry;

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
        var visitor = new SchemaDefinitionVisitor(registry);
        var traverseVisitor = new SchemaDefinitionTraverseVisitor(visitor);

        queryTree.Accept(traverseVisitor);

        return registry;
    }

    private static string? GenerateInterpreterSourceCode(SchemaRegistry registry)
    {
        const string interpreterNamespace = "Musoq.Generated.Interpreters";

        var codeGenerator = new InterpreterCodeGenerator(registry);
        var sourceCode = codeGenerator.GenerateAll();

        if (string.IsNullOrWhiteSpace(sourceCode) || !sourceCode.Contains("class"))
            return null;


        foreach (var registration in registry.Schemas)
            registration.GeneratedTypeName = $"{interpreterNamespace}.{registration.Name}";

        return sourceCode;
    }
}

/// <summary>
///     Traverse visitor for extracting schema definitions from the AST.
/// </summary>
public class SchemaDefinitionTraverseVisitor : NoOpExpressionVisitor
{
    private readonly SchemaDefinitionVisitor _visitor;

    public SchemaDefinitionTraverseVisitor(SchemaDefinitionVisitor visitor)
    {
        _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
    }

    public override void Visit(BinarySchemaNode node)
    {
        _visitor.Visit(node);
    }

    public override void Visit(TextSchemaNode node)
    {
        _visitor.Visit(node);
    }

    public override void Visit(RootNode node)
    {
        node.Expression?.Accept(this);
    }

    public override void Visit(StatementsArrayNode node)
    {
        foreach (var statement in node.Statements) statement.Accept(this);
    }

    public override void Visit(StatementNode node)
    {
        node.Node?.Accept(this);
    }

    public override void Visit(MultiStatementNode node)
    {
        foreach (var statement in node.Nodes) statement.Accept(this);
    }
}
