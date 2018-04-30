using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Schema;

namespace Musoq.Evaluator.Utils.Symbols
{
    public class TableSymbol : Symbol
    {
        private readonly Dictionary<string, ISchemaTable> _tables = new Dictionary<string, ISchemaTable>();
        private readonly List<string> _orders = new List<string>();

        public TableSymbol(string alias, ISchemaTable table)
        {
            _tables.Add(alias, table);
            _orders.Add(alias);
        }

        public TableSymbol(string[] aliases, ISchemaTable[] tables)
        {
            for (int i = 0; i < aliases.Length; i++)
            {
                _tables.Add(aliases[i], tables[i]);
                _orders.Add(aliases[i]);
            }
        }

        private TableSymbol() { }

        public string[] CompoundTables => _orders.ToArray();

        public ISchemaTable GetTableByColumnName(string column)
        {
            ISchemaTable score = null;
            foreach (var table in _tables)
            {
                var col = table.Value.Columns.SingleOrDefault(c => c.ColumnName == column);

                if(score != null)
                    throw new NotSupportedException();

                score = table.Value;
            }

            return score;
        }

        public ISchemaTable GetTableByAlias(string alias)
        {
            return _tables[alias];
        }

        public ISchemaColumn GetColumnByAliasAndName(string alias, string columnName)
        {
            return _tables[alias].Columns.Single(c => c.ColumnName == columnName);
        }

        public ISchemaColumn[] GetColumn(string alias)
        {
            return _tables[alias].Columns;
        }

        public int GetColumnIndex(string alias, string columnName)
        {
            int i = 0;
            int count = 0;
            while (_orders[i] != alias)
            {
                count += _tables[_orders[i]].Columns.Length;
                i++;
            }

            var columns = _tables[_orders[i]].Columns;
            int j = 0;
            for (; j < columns.Length; j++)
            {
                if(columns[j].ColumnName == columnName)
                    break;
            }

            return (count + j + 1);
        }

        public TableSymbol MergeSymbols(TableSymbol other)
        {
            var symbol = new TableSymbol();

            foreach (var item in _tables)
            {
                symbol._tables.Add(item.Key, item.Value);
                symbol._orders.Add(item.Key);
            }

            foreach (var item in other._tables)
            {
                symbol._tables.Add(item.Key, item.Value);
                symbol._orders.Add(item.Key);
            }

            return symbol;
        }
    }

    public class ColumnSymbol : Symbol
    {
        public ColumnSymbol(ISchemaColumn column)
        {
            Column = column;
        }

        public ISchemaColumn Column { get; }
    }
}
