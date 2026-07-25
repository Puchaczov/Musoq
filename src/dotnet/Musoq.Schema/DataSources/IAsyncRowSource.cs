using System.Collections.Generic;

namespace Musoq.Schema.DataSources;

/// <summary>
///     Optional asynchronous row-source capability for providers that can stream chunks without
///     blocking a worker thread.
/// </summary>
public interface IAsyncRowSource<out T>
{
    IAsyncEnumerable<IReadOnlyList<T>> ChunksAsync { get; }
}
