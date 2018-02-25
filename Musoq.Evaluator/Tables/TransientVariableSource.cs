using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tables
{
    internal class TransientVariableSource : RowSource
    {
        public TransientVariableSource(string name)
        {
        }

        public override IEnumerable<IObjectResolver> Rows { get; }
    }
}