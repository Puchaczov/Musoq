using System;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Simple schema column implementation.
/// </summary>
public class SchemaColumn : ISchemaColumn
{
    public SchemaColumn(string columnName, int columnIndex, Type columnType)
    {
        ColumnName = columnName;
        ColumnIndex = columnIndex;
        ColumnType = columnType;
    }

    public string ColumnName { get; }
    public int ColumnIndex { get; }
    public Type ColumnType { get; }
}
