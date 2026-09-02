using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(CreateTransformationTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = CreateFields(node.Fields);

        PushSemanticNode(new CreateTransformationTableNode(node.Name, node.Keys, fields, node.ForGrouping));
    }

    public override void Visit(RenameTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        PushSemanticNode(new RenameTableNode(node.TableSourceName, node.TableDestinationName));
    }

    public override void Visit(IntoNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        PushSemanticNode(new IntoNode(node.Name));
    }

    public override void Visit(ShouldBePresentInTheTable node)
    {
        ArgumentNullException.ThrowIfNull(node);
        PushSemanticNode(new ShouldBePresentInTheTable(node.Table, node.ExpectedResult, node.Keys));
    }

    public override void Visit(JoinNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _sourceBinding.Identifier = node.Alias;
        PushSemanticNode(new Parser.JoinNode((Parser.JoinFromNode)PopSemanticNode()));
    }

    public override void Visit(ApplyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _sourceBinding.Identifier = node.Alias;
        PushSemanticNode(new Parser.ApplyNode((Parser.ApplyFromNode)PopSemanticNode()));
    }

    public void SetScope(Scope scope)
    {
        _sourceBinding.CurrentScope = scope;
    }

    public override void Visit(CoupleNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.TableName != null &&
            !_sourceBinding.ExplicitlyDefinedTables.Keys.Any(
                tableName => string.Equals(tableName, node.TableName, StringComparison.Ordinal)))
        {
            var undefinedTable = new TableIsNotDefinedException(node.TableName, node.SpanOrEmpty());
            TryReportException(undefinedTable, node);
        }

        if (_sourceBinding.ExplicitlyCoupledSources.Keys.Any(
                alias => string.Equals(alias, node.MappedSchemaName, StringComparison.OrdinalIgnoreCase)))
        {
            var duplicateAlias = new VisitorException(
                nameof(BuildMetadataAndInferTypesVisitor),
                "ValidateCoupleDefinition",
                $"COUPLE alias '{node.MappedSchemaName}' is already defined in this query batch.",
                DiagnosticCode.MQ3021_DuplicateAlias,
                node.SpanOrEmpty());
            TryReportException(duplicateAlias, node);
        }

        if (!_sourceBinding.ExplicitlyCoupledSources.Keys.Any(
                alias => string.Equals(alias, node.MappedSchemaName, StringComparison.OrdinalIgnoreCase)))
            _sourceBinding.ExplicitlyCoupledSources.Add(
                node.MappedSchemaName,
                new CoupledSourceDefinition(node.SchemaMethodNode, node.TableName, node.ProfileName));
        PushSemanticNode(((CoupleNode)new CoupleNode(node.SchemaMethodNode, node.TableName, node.ProfileName, node.MappedSchemaName))
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
    }

    public override void Visit(StatementsArrayNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var statements = new StatementNode[node.Statements.Length];
        for (var i = 0; i < node.Statements.Length; ++i)
            statements[node.Statements.Length - 1 - i] = (StatementNode)PopSemanticNode();

        PushSemanticNode(new StatementsArrayNode(statements));
    }

    internal void ValidateStatementOrder(RootNode root)
    {
        if (root.Expression is not StatementsArrayNode statements)
            return;

        var orderedStatements = statements.Statements
            .OrderBy(static statement => GetStatementSpan(statement).IsEmpty
                ? int.MaxValue
                : GetStatementSpan(statement).Start)
            .ToArray();
        var declaredTables = orderedStatements
            .Select(static statement => statement.Node)
            .OfType<CreateTableNode>()
            .Select(static table => table.Name)
            .ToHashSet(StringComparer.Ordinal);
        ValidateEnumDeclarationVisibility(orderedStatements);
        var phase = 0;
        foreach (var statement in orderedStatements)
        {
            switch (statement.Node)
            {
                case CreateTableNode table when phase != 0:
                    throw CreateStatementOrderException(
                        $"TABLE '{table.Name}' must appear before COUPLE statements and executable queries.",
                        table);
                case CreateTableNode:
                    break;
                case EnumDeclarationNode declaration when phase != 0:
                    throw CreateStatementOrderException(
                        $"ENUM '{declaration.Name}' must appear before COUPLE statements and executable queries.",
                        declaration);
                case EnumDeclarationNode:
                    break;
                case CoupleNode couple when phase >= 2:
                    throw CreateStatementOrderException(
                        $"COUPLE '{couple.MappedSchemaName}' must appear before CTEs and executable queries.",
                        couple);
                case CoupleNode couple when couple.TableName != null &&
                    declaredTables.Contains(couple.TableName) &&
                    !TableWasDeclaredBefore(orderedStatements, statement, couple.TableName):
                    throw CreateStatementOrderException(
                        $"TABLE '{couple.TableName}' must be declared before its COUPLE statement.",
                        couple);
                case CoupleNode:
                    phase = 1;
                    break;
                case ParameterBlockNode or ScriptVariableDeclarationNode or BinarySchemaNode or TextSchemaNode:
                    break;
                default:
                    phase = 2;
                    break;
            }
        }
    }

    private static void ValidateEnumDeclarationVisibility(IReadOnlyList<StatementNode> statements)
    {
        var enumPositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < statements.Count; index++)
            if (statements[index].Node is EnumDeclarationNode declaration)
                enumPositions.TryAdd(declaration.Name, index);

        for (var statementIndex = 0; statementIndex < statements.Count; statementIndex++)
        {
            if (statements[statementIndex].Node is not CreateTableNode table)
                continue;

            foreach (var column in table.Columns)
            {
                var typeName = column.TypeName.EndsWith("?", StringComparison.Ordinal)
                    ? column.TypeName[..^1]
                    : column.TypeName;
                if (!enumPositions.TryGetValue(typeName, out var declarationIndex) ||
                    declarationIndex < statementIndex)
                    continue;

                var span = column.Span.IsEmpty ? table.SpanOrEmpty() : column.Span;
                throw new VisitorException(
                    nameof(BuildMetadataAndInferTypesVisitor),
                    "ValidateEnumDeclarationVisibility",
                    $"Enum type '{typeName}' must be declared before TABLE '{table.Name}' uses it. Enum declarations are visible only to following statements.",
                    DiagnosticCode.MQ3107_UnknownEnumType,
                    span);
            }
        }
    }

    private static bool TableWasDeclaredBefore(
        IReadOnlyList<StatementNode> statements,
        StatementNode current,
        string tableName)
    {
        foreach (var statement in statements)
        {
            if (ReferenceEquals(statement, current))
                return false;

            if (statement.Node is CreateTableNode table &&
                string.Equals(table.Name, tableName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static TextSpan GetStatementSpan(StatementNode statement)
        => GetNodeSpan(statement.Node);

    private static TextSpan GetNodeSpan(Node node)
    {
        var span = node.SpanOrEmpty();
        if (!span.IsEmpty)
            return span;

        foreach (var child in ParserNodeChildTraversal.EnumerateChildren(node))
        {
            span = GetNodeSpan(child);
            if (!span.IsEmpty)
                return span;
        }

        return TextSpan.Empty;
    }

    private static VisitorException CreateStatementOrderException(string message, Node node) =>
        new(
            nameof(BuildMetadataAndInferTypesVisitor),
            "ValidateStatementOrder",
            message,
            DiagnosticCode.MQ3102_InvalidStatementOrder,
            node.SpanOrEmpty());

    public override void Visit(StatementNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.Node is not ParameterBlockNode)
            _diagnostics.HasSeenNonParameterStatement = true;

        // Interpretation schema definitions are intentionally skipped by the
        // metadata traverser. They still arrive wrapped in a StatementNode,
        // so there is no semantic child on the stack to pop.
        if (node.Node is BinarySchemaNode or TextSchemaNode)
        {
            PushSemanticNode(new StatementNode(node.Node));
            return;
        }

        PushSemanticNode(new StatementNode(PopSemanticNode()));
    }
}
