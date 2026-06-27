using System;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Components;

/// <summary>
///     Simple schema column implementation.
/// </summary>
public class SchemaColumn(string columnName, int columnIndex, Type columnType) : ISchemaColumn
{
    public string ColumnName { get; } = columnName;
    public int ColumnIndex { get; } = columnIndex;
    public Type ColumnType { get; } = columnType;
}
