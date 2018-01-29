using System;

namespace FQL.Schema.DataSources
{
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
}