namespace Musoq.Evaluator.IR.Planning;

/// <summary>Combined observations of one evaluation occurrence, independent of its producer.</summary>
internal readonly record struct ByteBufferRequirement(int PrefixBytes, bool ObservesLength, bool RequiresCompleteArray)
{
    internal static ByteBufferRequirement Complete => new(0, true, true);
    internal static ByteBufferRequirement Length => new(0, true, false);

    internal static ByteBufferRequirement Prefix(int bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        return new(bytes, false, false);
    }

    internal int? MaximumReadBytes => RequiresCompleteArray ? null : PrefixBytes;

    internal ByteBufferRequirement Combine(ByteBufferRequirement other) =>
        new(
            Math.Max(PrefixBytes, other.PrefixBytes),
            ObservesLength || other.ObservesLength,
            RequiresCompleteArray || other.RequiresCompleteArray
        );
}
