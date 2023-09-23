using System;
using System.Linq;

namespace Musoq.Schema
{
    public class SingleRowSchemaTable : ISchemaTable
    {
        private class SimpleColumn : ISchemaColumn
        {
            public string ColumnName => "Column1";

            public int ColumnIndex => 0;

            public Type ColumnType => typeof(string);
        }

        public ISchemaColumn[] Columns => new ISchemaColumn[]{
            new SimpleColumn()
        };

        public ISchemaColumn GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }

        public SchemaTableMetadata Metadata { get; }
    }
}
