using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.RuntimeScripts
{
    public partial class MainMethod
    {
        public string Source { get; set; }

        public string Schema { get; set; }

        public string Library { get; set; }

        public string Where { get; set; }

        private bool ContainsGroupingOperator()
        {
            return false;
        }
        private bool ContainsWhereOperator()
        {
            return string.IsNullOrEmpty(Where);
        }

        public string[] RefreshMethods { get; set; }

        public FieldNode[] Select { get; set; }

        public string RowSourceArguments { get; set; }

        public string RowSourceMethod { get; set; }
    }
}
