namespace Musoq.Evaluator.IR.Expressions;

internal enum ByteBufferCallableKind
{
    Skip,
    Take,
    SkipAndTake,
    Materialize,
    Concatenate,
    Encode,
    Decode,
    FixedWidthRead,
    Length,
}

internal enum ByteBufferEncoding
{
    None,
    Base64,
    Hex,
}

internal enum ByteBufferArgumentStorage
{
    None,
    Array,
    ReadOnlySpan,
}

internal enum ByteBufferEncodingRange
{
    Complete,
    CheckedSlice,
}

/// <summary>Declaration semantics established once from an exact bound callable.</summary>
internal sealed record ByteBufferCallableSemantics(
    ByteBufferCallableKind Kind,
    ByteBufferEncoding Encoding = ByteBufferEncoding.None,
    int ReadWidth = 0,
    int PaddingArgument = -1,
    ByteBufferArgumentStorage ArgumentStorage = ByteBufferArgumentStorage.None,
    ByteBufferEncodingRange EncodingRange = ByteBufferEncodingRange.Complete,
    int DelimiterArgument = -1
)
{
    internal const string Contract = "byte-buffer-semantics-v1";
}
