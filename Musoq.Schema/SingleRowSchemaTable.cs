using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
