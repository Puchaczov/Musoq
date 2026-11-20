using System.Collections;
using System.Collections.Generic;

namespace Musoq.Schema.DataSources;

// Retain the storage identity for capacity hints while enumerating through the normal lifecycle boundary.
internal sealed class StoredSourceChunks<T>(IReadOnlyList<T>[] storage, IEnumerable<IReadOnlyList<T>> guarded)
    : IEnumerable<IReadOnlyList<T>>
{
    internal IReadOnlyList<T>[] Storage { get; } = storage;

    public IEnumerator<IReadOnlyList<T>> GetEnumerator() => guarded.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
