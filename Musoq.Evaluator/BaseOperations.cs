using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator
{
    public class BaseOperations
    {
        public Table Union(Table first, Table second, Func<Row, Row, bool> comparer)
        {
            return first;
        }
        public Table UnionAll(Table first, Table second, Func<Row, Row, bool> comparer)
        {
            return first;
        }
        public Table Except(Table first, Table second, Func<Row, Row, bool> comparer)
        {
            return first;
        }
        public Table Intersect(Table first, Table second, Func<Row, Row, bool> comparer)
        {
            return first;
        }
    }
}
