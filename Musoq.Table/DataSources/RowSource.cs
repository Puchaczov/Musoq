using System.Collections.Generic;

namespace Musoq.Schema.DataSources
{
    public abstract class RowSource
    {
        public abstract IEnumerable<IObjectResolver> Rows { get; }
    }
}