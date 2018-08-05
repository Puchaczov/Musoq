using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Schema;

namespace Musoq.Evaluator.Utils.Symbols
{
    public class TableSymbol : Symbol
    {
        private readonly List<string> _orders = new List<string>();

        private readonly Dictionary<string, Tuple<ISchema, ISchemaTable>> _tables =
            new Dictionary<string, Tuple<ISchema, ISchemaTable>>();

        private string _fullTableName;

        private ISchemaTable _fullTable;
        private ISchema _fullSchema;

        public TableSymbol(string alias, ISchema schema, ISchemaTable table, bool hasAlias)
        {
            _tables.Add(alias, new Tuple<ISchema, ISchemaTable>(schema, table));
            _orders.Add(alias);
            HasAlias = hasAlias;
            _fullTableName = alias;

            _fullSchema = schema;
            _fullTable = table;
        }

        private TableSymbol()
        {
            HasAlias = true;
        }

        public bool HasAlias { get; }

        public string[] CompoundTables => _orders.ToArray();

        public (ISchema Schema, ISchemaTable Table, string TableName) GetTableByColumnName(string column)
        {
            (ISchema, ISchemaTable, string) score = (null, null, null);

            foreach (var table in _tables)
            {
                var col = table.Value.Item2.Columns.SingleOrDefault(c => c.ColumnName == column);

                if (col == null)
                    throw new NotSupportedException();

                score = (table.Value.Item1, table.Value.Item2, table.Key);
            }

            return score;
        }

        public (ISchema Schema, ISchemaTable Table, string TableName) GetTableByAlias(string alias)
        {
            if (_fullTableName == alias)
                return (_fullSchema, _fullTable, alias);
            return (_tables[alias].Item1, _tables[alias].Item2, alias);
        }

        public ISchemaColumn GetColumnByAliasAndName(string alias, string columnName)
        {
            if (_fullTableName == alias)
                return _fullTable.Columns.Single(c => c.ColumnName == columnName);

            return _tables[alias].Item2.Columns.Single(c => c.ColumnName == columnName);
        }

        public ISchemaColumn GetColumn(string columnName)
        {
            ISchemaColumn column = null;
            foreach (var table in _orders)
            {
                var tmpColumn = _tables[table].Item2.Columns.SingleOrDefault(col => col.ColumnName == columnName);

                if (column != null)
                    throw new NotSupportedException("Multiple column with the same identifier");

                if (tmpColumn == null)
                    continue;

                column = tmpColumn;
            }

            if (column == null)
                throw new NotSupportedException("No such column.");

            return column;
        }

        public ISchemaColumn[] GetColumns(string alias)
        {
            return _tables[alias].Item2.Columns;
        }

        public ISchemaColumn[] GetColumns()
        {
            var columns = new List<ISchemaColumn>();
            foreach (var table in _orders) columns.AddRange(GetColumns(table));

            return columns.ToArray();
        }

        public int GetColumnIndex(string alias, string columnName)
        {
            var i = 0;
            var count = 0;
            while (_orders[i] != alias)
            {
                count += _tables[_orders[i]].Item2.Columns.Length;
                i++;
            }

            var columns = _tables[_orders[i]].Item2.Columns;
            var j = 0;
            for (; j < columns.Length; j++)
                if (columns[j].ColumnName == columnName)
                    break;

            return count + j + 1;
        }

        public TableSymbol MergeSymbols(TableSymbol other)
        {
            var symbol = new TableSymbol();

            var compundTableColumns = new List<ISchemaColumn>();

            foreach (var item in _tables)
            {
                symbol._tables.Add(item.Key, item.Value);
                symbol._orders.Add(item.Key);

                compundTableColumns.AddRange(item.Value.Item2.Columns);
            }

            foreach (var item in other._tables)
            {
                symbol._tables.Add(item.Key, item.Value);
                symbol._orders.Add(item.Key);

                compundTableColumns.AddRange(item.Value.Item2.Columns);
            }

            symbol._fullTableName = symbol._orders.Aggregate((a, b) => a + b);
            symbol._fullTable = new DynamicTable(compundTableColumns.ToArray());
            symbol._fullSchema = new TransitionSchema(symbol._fullTableName, symbol._fullTable);

            return symbol;
        }
    }
}