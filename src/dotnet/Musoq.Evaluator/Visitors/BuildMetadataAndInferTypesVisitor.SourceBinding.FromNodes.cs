using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;
using ExpressionFromNode = Musoq.Parser.Nodes.From.ExpressionFromNode;
using InMemoryTableFromNode = Musoq.Parser.Nodes.From.InMemoryTableFromNode;
using InterpretFromNode = Musoq.Parser.Nodes.From.InterpretFromNode;
using JoinInMemoryWithSourceTableFromNode = Musoq.Parser.Nodes.From.JoinInMemoryWithSourceTableFromNode;
using JoinSourcesTableFromNode = Musoq.Parser.Nodes.From.JoinSourcesTableFromNode;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(JoinSourcesTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var tieBreak = node.TieBreak == null ? null : (FieldOrderedNode)PopSemanticNode();
        var b = (FromNode)PopSemanticNode();
        var a = (FromNode)PopSemanticNode();
        var exp = PopSemanticNode();

        PushSemanticNode(new Parser.JoinSourcesTableFromNode(a, b, exp, node.JoinType, tieBreak));
    }

    public override void Visit(ApplySourcesTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var b = (FromNode)PopSemanticNode();
        var a = (FromNode)PopSemanticNode();

        PushSemanticNode(new Parser.ApplySourcesTableFromNode(a, b, node.ApplyType, node.WithOrdinality));
    }

    public override void Visit(InMemoryTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _sourceBinding.QueryAlias = string.IsNullOrEmpty(node.Alias) ? node.VariableName : node.Alias;
        var hasExplicitAlias = !string.IsNullOrEmpty(node.Alias);

        if (HasAlreadyUsedAlias(_sourceBinding.QueryAlias) &&
            TryReportDuplicateAlias(node, _sourceBinding.QueryAlias, node))
            return;

        _resultShape.GeneratedAliases.Add(_sourceBinding.QueryAlias);

        TableSymbol tableSymbol;

        var parentScope = _sourceBinding.CurrentScope.Parent ??
                          throw new VisitorException(
                              VisitorName,
                              "VisitInMemoryTableFromNode",
                              "In-memory table source requires a parent scope.");

        if (parentScope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(node.VariableName))
        {
            tableSymbol = parentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(node.VariableName) ??
                          throw new TableIsNotDefinedException(node.VariableName, node.SpanOrEmpty());
        }
        else
        {
            var scope = _sourceBinding.CurrentScope;

            while (scope != null && scope.Name != "CTE") scope = scope.Parent;

            if (scope is null)
            {
                if (TryReportTableNotDefined(node.VariableName, node))
                    return;
                var span = node.SpanOrEmpty();
                throw new TableIsNotDefinedException(node.VariableName, span);
            }

            tableSymbol = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(node.VariableName);
        }

        var tableSchemaPair = tableSymbol.GetTableByAlias(node.VariableName);
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(_sourceBinding.QueryAlias,
            new TableSymbol(_sourceBinding.QueryAlias, tableSchemaPair.Schema, tableSchemaPair.Table, hasExplicitAlias));
        _sourceBinding.CurrentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(_sourceBinding.QueryAlias);
        _sourceBinding.CurrentScope[node.Id] = _sourceBinding.QueryAlias;

        _sourceBinding.AliasMapToInMemoryTableMap.Add(_sourceBinding.QueryAlias, node.VariableName);
        _sourceBinding.UsedSchemasQuantity += 1;

        PushSemanticNode(new Parser.InMemoryTableFromNode(node.VariableName, _sourceBinding.QueryAlias));
    }

    public override void Visit(ApplyFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var appliedTable = (FromNode)PopSemanticNode();
        var source = (FromNode)PopSemanticNode();
        var appliedFrom = new Parser.ApplyFromNode(source, appliedTable, node.ApplyType, node.WithOrdinality);
        _sourceBinding.Identifier = appliedFrom.Alias;
        PushSemanticNode(appliedFrom);
    }

    public override void Visit(ExpressionFromNode node)
    {
        var from = (FromNode)PopSemanticNode();
        _sourceBinding.Identifier = from.Alias;
        PushSemanticNode(new Parser.ExpressionFromNode(from));

        var tableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_sourceBinding.Identifier);

        foreach (var tableAlias in tableSymbol.CompoundTables)
        {
            var tuple = tableSymbol.GetTableByAlias(tableAlias);

            foreach (var column in tuple.Table.Columns)
                AddAssembly(column.ColumnType.Assembly);
        }
    }

    public override void Visit(InterpretFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var interpretCall = PopSemanticNode();
        _sourceBinding.Identifier = node.Alias;


        string? schemaName = null;
        if (interpretCall is InterpretCallNode icn)
            schemaName = icn.SchemaName;
        else if (interpretCall is TryInterpretCallNode ticn)
            schemaName = ticn.SchemaName;
        else if (interpretCall is PartialInterpretCallNode picn)
            schemaName = picn.SchemaName;
        else if (interpretCall is PartialParseCallNode ppcn)
            schemaName = ppcn.SchemaName;
        else if (interpretCall is ParseCallNode pcn)
            schemaName = pcn.SchemaName;
        else if (interpretCall is InterpretAtCallNode iacn)
            schemaName = iacn.SchemaName;
        else if (interpretCall is ArgsListNode argsNode) schemaName = ExtractSchemaNameFromArgs(argsNode);


        if (schemaName != null && SchemaRegistry != null)
        {
            var interpretTable = interpretCall is PartialInterpretCallNode or PartialParseCallNode
                ? CreatePartialInterpretTable()
                : CreateInterpretTable(schemaName);

            Type? returnType = null;
            if (SchemaRegistry.TryGetSchema(schemaName, out var schemaRegistration))
            {
                returnType = schemaRegistration?.GeneratedType;
                if (returnType != null && interpretCall is PartialInterpretCallNode or PartialParseCallNode)
                    returnType = typeof(Musoq.Schema.Interpreters.PartialInterpretResult<>).MakeGenericType(returnType);
            }

            var interpretTableSymbol = new TableSymbol(
                node.Alias,
                new TransitionSchema(node.Alias, interpretTable),
                interpretTable,
                !string.IsNullOrEmpty(node.Alias)
            );

            _sourceBinding.CurrentScope.ScopeSymbolTable.AddSymbol(node.Alias, interpretTableSymbol);
            _sourceBinding.CurrentScope.ScopeSymbolTable.AddOrGetSymbol<AliasesSymbol>(MetaAttributes.Aliases).AddAlias(node.Alias);

            var newInterpretFromNode = new Parser.InterpretFromNode(node.Alias, interpretCall, node.ApplyType,
                returnType ?? node.ReturnType);
            _sourceBinding.CurrentScope[newInterpretFromNode.Id] = node.Alias;
            PushSemanticNode(newInterpretFromNode);
        }
        else
        {
            var newInterpretFromNode =
                new Parser.InterpretFromNode(node.Alias, interpretCall, node.ApplyType, node.ReturnType);
            _sourceBinding.CurrentScope[newInterpretFromNode.Id] = node.Alias;
            PushSemanticNode(newInterpretFromNode);
        }

        if (node.ReturnType != null) AddAssembly(node.ReturnType.Assembly);
    }

    public override void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var tieBreak = node.TieBreak == null ? null : (FieldOrderedNode)PopSemanticNode();
        var exp = PopSemanticNode();
        var from = (FromNode)PopSemanticNode();
        PushSemanticNode(
            new Parser.JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp, node.JoinType, tieBreak: tieBreak));
    }

    public override void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var from = (FromNode)PopSemanticNode();
        PushSemanticNode(
            new Parser.ApplyInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, node.ApplyType, node.WithOrdinality));
    }
}
