using System;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator
{
    public abstract class BaseOperations
    {
        public Table Union(Table first, Table second, Func<Row, Row, bool> comparer)
        {
            var result = new Table($"{first.Name}Union{second.Name}", first.Columns.ToArray());

            foreach (var row in first) result.Add(row);

            foreach (var row in second)
                if (!result.Contains(row, comparer))
                    result.Add(row);

            return result;
        }

        public Table UnionAll(Table first, Table second, Func<Row, Row, bool> comparer = null)
        {
            var result = new Table($"{first.Name}UnionAll{second.Name}", first.Columns.ToArray());

            foreach (var row in first) result.Add(row);

            foreach (var row in second) result.Add(row);

            return result;
        }

        public Table Except(Table first, Table second, Func<Row, Row, bool> comparer)
        {
            var result = new Table($"{first.Name}Except{second.Name}", first.Columns.ToArray());

            foreach (var row in first)
                if (!second.Contains(row, comparer))
                    result.Add(row);

            return result;
        }

        public Table Intersect(Table first, Table second, Func<Row, Row, bool> comparer)
        {
            var result = new Table($"{first.Name}Except{second.Name}", first.Columns.ToArray());

            foreach (var row in first)
                if (second.Contains(row, comparer))
                    result.Add(row);

            return result;
        }
    }
}