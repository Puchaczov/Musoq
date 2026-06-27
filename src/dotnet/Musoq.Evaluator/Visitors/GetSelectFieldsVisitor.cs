using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

public class GetSelectFieldsVisitor : NoOpExpressionVisitor, IQueryPartAwareExpressionVisitor
{
    private readonly List<ISchemaColumn> _collectedFieldNames = [];
    private ISchemaColumn[] _cachedFieldNames = [];
    private bool _fieldNamesCacheValid;
    private QueryPart _queryPart;

    public ISchemaColumn[] CollectedFieldNames
    {
        get
        {
            if (_fieldNamesCacheValid)
                return _cachedFieldNames;

            _cachedFieldNames = _collectedFieldNames.ToArray();
            _fieldNamesCacheValid = true;
            return _cachedFieldNames;
        }
    }

    /// <summary>
    ///     Provides direct access to the list for efficient enumeration when modification is not needed.
    /// </summary>
    public IReadOnlyList<ISchemaColumn> CollectedFieldNamesList => _collectedFieldNames;

    public void SetQueryPart(QueryPart part)
    {
        _queryPart = part;
    }

    public void QueryBegins()
    {
    }

    public void QueryEnds()
    {
    }

    public override void Visit(FieldNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (_queryPart != QueryPart.Select)
            return;

        if (HasCollectedFieldAtPosition(node.FieldOrder))
            return;

        _collectedFieldNames.Add(new SchemaColumn(
            GetCteOutputColumnName(node),
            node.FieldOrder,
            node.ReturnType ?? throw new InvalidOperationException($"Select field '{node.FieldName}' has no inferred return type.")));
        _fieldNamesCacheValid = false;
    }

    private static string GetCteOutputColumnName(FieldNode node)
    {
        if (node.HasExplicitFieldName)
            return node.FieldName;

        return node.Expression is AccessColumnNode accessColumn
            ? accessColumn.Name
            : node.FieldName;
    }

    private bool HasCollectedFieldAtPosition(int position)
    {
        foreach (var field in _collectedFieldNames)
        {
            if (field.ColumnIndex == position)
                return true;
        }

        return false;
    }
}
