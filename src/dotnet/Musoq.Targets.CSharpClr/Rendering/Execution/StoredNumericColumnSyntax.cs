using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class StoredNumericColumnSyntax
{
    internal static MemberDeclarationSyntax Create() =>
        SyntaxFactory.ParseMemberDeclaration(
            """
            private struct StoredNumericColumn<T>
            {
                private const int ChunkSize = 1024;
                private T[] first;
                private T[][] following;
                private int chunks;
                public int Count { get; private set; }

                public StoredNumericColumn(int capacity)
                {
                    this = default;
                    EnsureCapacity(capacity);
                }

                public T this[int index] => index < ChunkSize ? first[index] : following[(index >> 10) - 1][index & (ChunkSize - 1)];

                public ReadOnlySpan<T> GetSpan(int start, int length) =>
                    start < ChunkSize ? first.AsSpan(start, length) : following[(start >> 10) - 1].AsSpan(start & (ChunkSize - 1), length);

                public void Add(T value)
                {
                    int index = Count;
                    if (index < ChunkSize)
                    {
                        if (first == null || index == first.Length)
                            Array.Resize(ref first, first == null ? 4 : Math.Min(ChunkSize, first.Length * 2));
                        first[index] = value;
                    }
                    else
                    {
                        int chunk = (index >> 10) - 1;
                        if (chunk == chunks) AddChunk();
                        following[chunk][index & (ChunkSize - 1)] = value;
                    }
                Count = checked(index + 1);
                }

                public void EnsureCapacity(int capacity)
                {
                    if (capacity <= 0) return;
                    int size = Math.Min(capacity, ChunkSize);
                    if (first == null || first.Length < size) Array.Resize(ref first, size);
                    int required = (capacity - 1) >> 10;
                    while (chunks < required) AddChunk();
                }

                private void AddChunk()
                {
                    if (following == null || chunks == following.Length)
                        Array.Resize(ref following, chunks == 0 ? 4 : chunks * 2);
                    following[chunks++] = new T[ChunkSize];
                }
            }
            """
        )!;
}
