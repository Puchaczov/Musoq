using System;

namespace Musoq.Schema
{
    public interface ISchemaColumn
    {
        string ColumnName { get; }
        int ColumnIndex { get; }
        Type ColumnType { get; }
    }
}