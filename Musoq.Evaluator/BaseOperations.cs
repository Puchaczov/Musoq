using System;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator
{
    public abstract class BaseOperations
    {
        public Table Union(Table first, Table second, Func<Row, Row, bool> comparer)
        {
            var result = new Table($"{first}Union{second}", first.Columns.ToArray());

            foreach (var row in first) result.Add(row);

            foreach (var row in second)
                if (!result.Contains(row, comparer))
                    result.Add(row);

            return result;
        }

        public Table UnionAll(Table first, Table second, Func<Row, Row, bool> comparer)
        {
            var result = new Table($"{first}UnionAll{second}", first.Columns.ToArray());

            foreach (var row in first) result.Add(row);

            foreach (var row in second) result.Add(row);

            return result;
        }

        public Table Except(Table first, Table second, Func<Row, Row, bool> comparer)
        {
            var result = new Table($"{first}Except{second}", first.Columns.ToArray());

            foreach (var row in first)
                if (!second.Contains(row, comparer))
                    result.Add(row);

            return result;
        }

        public Table Intersect(Table first, Table second, Func<Row, Row, bool> comparer)
        {
            var result = new Table($"{first}Except{second}", first.Columns.ToArray());

            foreach (var row in first)
                if (second.Contains(row, comparer))
                    result.Add(row);

            return result;
        }
    }
}