using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

public class GetSelectFieldsVisitor : NoOpExpressionVisitor, IQueryPartAwareExpressionVisitor
{
    private readonly List<ISchemaColumn> _collectedFieldNames = [];
    private ISchemaColumn[] _cachedFieldNames;
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
        if (_queryPart == QueryPart.Select && _collectedFieldNames.All(field => field.ColumnName != node.FieldName))
            _collectedFieldNames.Add(new SchemaColumn(node.FieldName, _collectedFieldNames.Count, node.ReturnType));
    }
}
