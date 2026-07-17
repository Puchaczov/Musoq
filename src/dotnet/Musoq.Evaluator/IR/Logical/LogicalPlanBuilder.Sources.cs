using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using IrNodes = Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    public void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = _nodeStack.Pop();
        var left = CreateInMemoryCteRef(
            node.InMemoryTableAlias,
            (node as Parser.JoinInMemoryWithSourceTableFromNode)?.InMemoryTableVariableName);
        var onPredicate = _converter.Convert(node.Expression);
        var kind = MapJoinKind(node.JoinType);
        _nodeStack.Push(new IrNodes.JoinNode(kind, onPredicate, left, right, ConvertTieBreak(node.TieBreak, kind)));
    }

    public void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = _nodeStack.Pop();
        var left = CreateInMemoryCteRef(node.InMemoryTableAlias);
        var kind = MapApplyKind(node.ApplyType);
        _nodeStack.Push(new IrNodes.ApplyNode(kind, left, right, node.WithOrdinality));
    }

    public void Visit(SchemaFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var args = ConvertArguments(node.Parameters);
        var schema = BuildOutputSchema(node.Alias);
        _nodeStack.Push(new IrNodes.SchemaScanNode(node.Schema, node.Method, args, node.Alias, schema, node.Id));
    }

    public void Visit(JoinSourcesTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = _nodeStack.Pop();
        var left = _nodeStack.Pop();
        var onPredicate = _converter.Convert(node.Expression);
        var kind = MapJoinKind(node.JoinType);
        _nodeStack.Push(new IrNodes.JoinNode(kind, onPredicate, left, right, ConvertTieBreak(node.TieBreak, kind)));
    }

    public void Visit(ApplySourcesTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = _nodeStack.Pop();
        var left = _nodeStack.Pop();
        var kind = MapApplyKind(node.ApplyType);
        _nodeStack.Push(new IrNodes.ApplyNode(kind, left, right, node.WithOrdinality));
    }

    public void Visit(InMemoryTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var schema = BuildOutputSchema(node.Alias, node.VariableName, node.VariableName.ToScoreTable());
        _nodeStack.Push(new IrNodes.CteRefNode(node.VariableName, node.Alias, schema));
    }

    public void Visit(ValuesFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var rows = node.Rows
            .Select(row => new IrNodes.ValuesScanRow(
                row.Fields
                    .Select(field => new IrNodes.ValuesScanField(field.Name, _converter.Convert(field.Expression)))
                    .ToArray()))
            .ToArray();

        _nodeStack.Push(new IrNodes.ValuesScanNode(node.Alias, rows, BuildOutputSchema(node.Alias)));
    }

    public void Visit(JoinFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = _nodeStack.Pop();
        var left = _nodeStack.Pop();
        var onPredicate = _converter.Convert(node.Expression);
        var kind = MapJoinKind(node.JoinType);
        _nodeStack.Push(new IrNodes.JoinNode(kind, onPredicate, left, right, ConvertTieBreak(node.TieBreak, kind)));
    }

    public void Visit(ApplyFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = _nodeStack.Pop();
        var left = _nodeStack.Pop();
        var kind = MapApplyKind(node.ApplyType);
        _nodeStack.Push(new IrNodes.ApplyNode(kind, left, right, node.WithOrdinality));
    }

    public void Visit(ExpressionFromNode node) { }

    public void Visit(InterpretFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var schema = BuildOutputSchema(node.Alias);
        _nodeStack.Push(CreateInterpretSource(node, schema));
    }

    public void Visit(SchemaMethodFromNode node) { }

    public void Visit(AccessMethodFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var schema = BuildOutputSchema(node.Alias);
        var resultType = node.ReturnType ?? typeof(object);
        var methodCallExpression = _converter.Convert(node.AccessMethod);
        _nodeStack.Push(new IrNodes.AccessMethodSourceNode(
            node.SourceAlias,
            methodCallExpression,
            node.Alias,
            resultType,
            IrNodes.ApplyKind.Cross,
            schema));
    }
    public void Visit(PropertyFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var schema = BuildOutputSchema(node.Alias);
        var columnIndex = ResolvePropertyColumnIndex(node);
        var resultType = node.ReturnType ?? typeof(object);
        _nodeStack.Push(new IrNodes.PropertySourceNode(
            node.SourceAlias,
            node.PropertiesChain,
            node.Alias,
            columnIndex,
            resultType,
            IrNodes.ApplyKind.Cross,
            schema));
    }
    public void Visit(AliasedFromNode node) { }

    public void Visit(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node is InMemoryGroupedFromNode groupedFrom)
        {
            var schema = BuildOutputSchema(groupedFrom.Alias, groupedFrom.Alias.ToScoreTable());
            _nodeStack.Push(new IrNodes.CteRefNode(groupedFrom.Alias, groupedFrom.Alias, schema));
            return;
        }

        throw UnsupportedShape.Of($"AST node type '{node.GetType().Name}'");
    }

    private int ResolvePropertyColumnIndex(PropertyFromNode node)
    {
        if (!_inferredColumns.TryGetValue(node.SourceAlias, out var sourceColumns))
            return -1;

        var propertyName = node.PropertiesChain[0].PropertyName;

        foreach (var column in sourceColumns)
        {
            if (string.Equals(column.ColumnName, propertyName, StringComparison.OrdinalIgnoreCase))
                return column.ColumnIndex;
        }

        return -1;
    }

    private OutputSchema BuildOutputSchema(params string?[] aliases)
    {
        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias))
                continue;

            if (!_inferredColumns.TryGetValue(alias, out var columns))
                continue;

            var schemaColumns = new ColumnSchema[columns.Length];
            for (var i = 0; i < columns.Length; i++)
                schemaColumns[i] = new ColumnSchema(columns[i].ColumnName, columns[i].ColumnType, columns[i].ColumnIndex);

            return new OutputSchema(schemaColumns);
        }

        return OutputSchema.Empty;
    }

    private IrNodes.CteRefNode CreateInMemoryCteRef(string alias, string? cteName = null)
    {
        return new IrNodes.CteRefNode(cteName ?? alias, alias, BuildOutputSchema(alias, cteName, cteName?.ToScoreTable()));
    }

    private static IrNodes.JoinKind MapJoinKind(JoinType joinType) =>
        joinType switch
        {
            JoinType.Inner => IrNodes.JoinKind.Inner,
            JoinType.OuterLeft => IrNodes.JoinKind.LeftOuter,
            JoinType.OuterRight => IrNodes.JoinKind.RightOuter,
            JoinType.OuterFull => IrNodes.JoinKind.FullOuter,
            JoinType.AsOf => IrNodes.JoinKind.AsofInner,
            JoinType.AsOfLeft => IrNodes.JoinKind.AsofLeft,
            JoinType.Cross => IrNodes.JoinKind.Cross,
            JoinType.LeftSemi => IrNodes.JoinKind.LeftSemi,
            JoinType.LeftAntiSemi => IrNodes.JoinKind.LeftAntiSemi,
            JoinType.LeftMark => IrNodes.JoinKind.LeftMark,
            JoinType.LeftSingle => IrNodes.JoinKind.LeftSingle,
            _ => throw UnsupportedShape.Of($"Join type '{joinType}'")
        };

    private OrderField? ConvertTieBreak(FieldOrderedNode? tieBreak, IrNodes.JoinKind kind)
    {
        if (tieBreak == null)
            return null;

        if (kind is not (IrNodes.JoinKind.AsofInner or IrNodes.JoinKind.AsofLeft))
            throw UnsupportedShape.Of("ASOF tie-break metadata was attached to a non-ASOF join.");

        return new OrderField(
            _converter.Convert(tieBreak.Expression),
            tieBreak.Order == Order.Descending,
            ConvertNullOrdering(tieBreak.NullOrdering));
    }

    private static IrNodes.ApplyKind MapApplyKind(ApplyType applyType)
    {
        return applyType switch
        {
            ApplyType.Cross => IrNodes.ApplyKind.Cross,
            ApplyType.Outer => IrNodes.ApplyKind.Outer,
            _ => throw new NotSupportedException($"Unsupported apply type: {applyType}")
        };
    }

    private IrExpression[] ConvertArguments(ArgsListNode args)
    {
        if (args == null || args.Args.Length == 0)
            return [];

        var result = new IrExpression[args.Args.Length];
        for (var i = 0; i < args.Args.Length; i++)
            result[i] = _converter.Convert(args.Args[i]);

        return result;
    }
}
