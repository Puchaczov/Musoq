using System;

namespace FQL.Schema
{
    public interface ISchemaColumn
    {
        string ColumnName { get; }
        int ColumnIndex { get; }
        Type ColumnType { get; }
    }
}