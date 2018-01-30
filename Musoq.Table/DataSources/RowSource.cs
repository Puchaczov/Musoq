using System.Collections.Generic;

namespace FQL.Schema.DataSources
{
    public abstract class RowSource
    {
        public abstract IEnumerable<IObjectResolver> Rows { get; }
    }
}