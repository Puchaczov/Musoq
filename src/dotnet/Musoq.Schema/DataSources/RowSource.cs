using System.Collections.Generic;

namespace Musoq.Schema.DataSources;

public abstract class RowSource<T>
{
    public abstract IEnumerable<IReadOnlyList<T>> Chunks { get; }
}
