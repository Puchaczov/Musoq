using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public class ExtractAccessColumnFromQueryVisitor : CloneQueryVisitor
{
    private readonly Dictionary<string, List<AccessColumnNode>> _accessColumns = new();

    public AccessColumnNode[] GetAll()
    {
        return _accessColumns.SelectMany(a => a.Value).ToArray();
    }

    public AccessColumnNode[] GetForAliases(params string[] aliases)
    {
        return _accessColumns.Where(a => aliases.Contains(a.Key)).SelectMany(a => a.Value).ToArray();
    }

    public AccessColumnNode[] GetForAliases(string first, string second)
    {
        return _accessColumns.Where(a => a.Key == first || a.Key == second).SelectMany(a => a.Value).ToArray();
    }

    public AccessColumnNode[] GetForAlias(string alias)
    {
        return _accessColumns[alias].ToArray();
    }

    public override void Visit(AccessColumnNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (_accessColumns.TryGetValue(node.Alias, out var list))
        {
            if (list.Any(f => f.Name == node.Name))
            {
                base.Visit(node);
                return;
            }

            list.Add(node);
            base.Visit(node);
            return;
        }

        _accessColumns.Add(node.Alias, [node]);
        base.Visit(node);
    }

    public override void Visit(SchemaFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _accessColumns.TryAdd(node.Alias, []);

        base.Visit(node);
    }

    public override void Visit(InMemoryTableFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _accessColumns.TryAdd(node.Alias, []);

        base.Visit(node);
    }

    public override void Visit(PropertyFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _accessColumns.TryAdd(node.SourceAlias, []);
        _accessColumns[node.SourceAlias].Add(
            new AccessColumnNode(node.FirstProperty.PropertyName, node.SourceAlias, node.ReturnType ?? typeof(object), TextSpan.Empty));
        _accessColumns.TryAdd(node.Alias, []);

        base.Visit(node);
    }

    public override void Visit(AccessMethodFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _accessColumns.TryAdd(node.Alias, []);

        base.Visit(node);
    }

    public override void Visit(AccessObjectArrayNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.IsColumnAccess && !string.IsNullOrEmpty(node.TableAlias))
        {
            var alias = node.TableAlias;
            var columnName = node.Token.Name;

            if (_accessColumns.TryGetValue(alias, out var list))
            {
                if (!list.Any(f => f.Name == columnName))
                    list.Add(new AccessColumnNode(columnName, alias, node.ColumnType ?? typeof(object), TextSpan.Empty));
            }
            else
            {
                _accessColumns.Add(alias, [new AccessColumnNode(columnName, alias, node.ColumnType ?? typeof(object), TextSpan.Empty)]);
            }
        }

        base.Visit(node);
    }

    public override void Visit(WindowFunctionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        CollectColumnsFromWindowFunction(node);
        Nodes.Push(node);
    }

    private void CollectColumnsFromWindowFunction(WindowFunctionNode windowNode)
    {
        foreach (var arg in windowNode.FunctionCall.Arguments.Args)
            CollectColumnsFromExpression(arg);
        if (windowNode.FunctionCall.FilterExpression != null)
            CollectColumnsFromExpression(windowNode.FunctionCall.FilterExpression);

        if (windowNode.WindowSpecification == null)
            return;

        foreach (var field in windowNode.WindowSpecification.PartitionFields)
            CollectColumnsFromExpression(field.Expression);
        foreach (var field in windowNode.WindowSpecification.OrderByFields)
            CollectColumnsFromExpression(field.Expression);
    }

    private void CollectColumnsFromExpression(Node node)
    {
        switch (node)
        {
            case AccessColumnNode accessColumn:
                RecordColumn(accessColumn);
                return;
            case BinaryNode binary:
                CollectColumnsFromExpression(binary.Left);
                CollectColumnsFromExpression(binary.Right);
                return;
            case UnaryNode unary:
                CollectColumnsFromExpression(unary.Expression);
                return;
            case AccessMethodNode method:
                foreach (var arg in method.Arguments.Args)
                    CollectColumnsFromExpression(arg);
                if (method.FilterExpression != null)
                    CollectColumnsFromExpression(method.FilterExpression);
                return;
        }
    }

    private void RecordColumn(AccessColumnNode node)
    {
        if (_accessColumns.TryGetValue(node.Alias, out var list))
        {
            if (list.All(f => f.Name != node.Name))
                list.Add(node);
            return;
        }

        _accessColumns.Add(node.Alias, [node]);
    }
}
