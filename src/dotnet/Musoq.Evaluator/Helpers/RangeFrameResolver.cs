using System.Numerics;
using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ResolveRangeFrameStart<TNumber, TOrder>(
        TNumber[] orderKeys, TOrder[] peerOrderKeys, int[] partitionIndices,
        int partitionStart, int partitionCount, int currentPartitionIndex,
        int offsetInSortDirection, bool descending, bool nullsFirst)
        where TNumber : struct, INumber<TNumber>
        => ResolveNumericRangeFrame(new RangeNumericKeys<TNumber>(orderKeys),
            new RangeFrameRequest<TOrder>(peerOrderKeys, partitionIndices, partitionStart, partitionCount,
                currentPartitionIndex, offsetInSortDirection, descending, nullsFirst), RangeBoundary.Start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ResolveRangeFrameStart<TNumber, TOrder>(
        TNumber?[] orderKeys, TOrder[] peerOrderKeys, int[] partitionIndices,
        int partitionStart, int partitionCount, int currentPartitionIndex,
        int offsetInSortDirection, bool descending, bool nullsFirst)
        where TNumber : struct, INumber<TNumber>
        => ResolveNumericRangeFrame(new NullableRangeNumericKeys<TNumber>(orderKeys),
            new RangeFrameRequest<TOrder>(peerOrderKeys, partitionIndices, partitionStart, partitionCount,
                currentPartitionIndex, offsetInSortDirection, descending, nullsFirst), RangeBoundary.Start);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ResolveRangeFrameEnd<TNumber, TOrder>(
        TNumber[] orderKeys, TOrder[] peerOrderKeys, int[] partitionIndices,
        int partitionStart, int partitionCount, int currentPartitionIndex,
        int offsetInSortDirection, bool descending, bool nullsFirst)
        where TNumber : struct, INumber<TNumber>
        => ResolveNumericRangeFrame(new RangeNumericKeys<TNumber>(orderKeys),
            new RangeFrameRequest<TOrder>(peerOrderKeys, partitionIndices, partitionStart, partitionCount,
                currentPartitionIndex, offsetInSortDirection, descending, nullsFirst), RangeBoundary.End);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ResolveRangeFrameEnd<TNumber, TOrder>(
        TNumber?[] orderKeys, TOrder[] peerOrderKeys, int[] partitionIndices,
        int partitionStart, int partitionCount, int currentPartitionIndex,
        int offsetInSortDirection, bool descending, bool nullsFirst)
        where TNumber : struct, INumber<TNumber>
        => ResolveNumericRangeFrame(new NullableRangeNumericKeys<TNumber>(orderKeys),
            new RangeFrameRequest<TOrder>(peerOrderKeys, partitionIndices, partitionStart, partitionCount,
                currentPartitionIndex, offsetInSortDirection, descending, nullsFirst), RangeBoundary.End);

    public static int ResolveRangePeerFrameStart<TOrder>(
        TOrder[] orderKeys,
        int[] partitionIndices,
        int partitionStart,
        int partitionCount,
        int currentPartitionIndex)
    {
        var current = orderKeys[partitionIndices[partitionStart + currentPartitionIndex]];
        var low = 0;
        var high = partitionCount;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            var candidate = orderKeys[partitionIndices[partitionStart + middle]];
            if (System.Collections.Generic.Comparer<TOrder>.Default.Compare(candidate, current) < 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    public static int ResolveRangePeerFrameEnd<TOrder>(
        TOrder[] orderKeys,
        int[] partitionIndices,
        int partitionStart,
        int partitionCount,
        int currentPartitionIndex)
    {
        var current = orderKeys[partitionIndices[partitionStart + currentPartitionIndex]];
        var low = 0;
        var high = partitionCount;

        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            var candidate = orderKeys[partitionIndices[partitionStart + middle]];
            if (System.Collections.Generic.Comparer<TOrder>.Default.Compare(candidate, current) <= 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ResolveNumericRangeFrame<TOrder, TKeys>(TKeys keys,
        in RangeFrameRequest<TOrder> request, RangeBoundary boundary)
        where TKeys : struct, IRangeNumericKeys
    {
        var current = keys.ReadDecimal(request.Indices[request.Start + request.Current]);
        if (!current.HasValue && request.PeerKeys.Length != 0)
            return ResolveNullRangePeers(request, boundary);
        var target = ShiftRangeTarget(current, request.Offset, request.Descending);
        var low = 0;
        var high = request.Count;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            var candidate = keys.ReadDecimal(request.Indices[request.Start + middle]);
            var comparison = CompareNumericRangeKeys(candidate, target, request.Descending, request.NullsFirst);
            if (comparison < 0 || comparison == 0 && boundary == RangeBoundary.End)
                low = middle + 1;
            else
                high = middle;
        }
        return boundary == RangeBoundary.Start ? low : low - 1;
    }

    private static int ResolveNullRangePeers<TOrder>(in RangeFrameRequest<TOrder> request, RangeBoundary boundary)
        => boundary switch
        {
            RangeBoundary.Start => ResolveRangePeerFrameStart(request.PeerKeys, request.Indices,
                request.Start, request.Count, request.Current),
            _ => ResolveRangePeerFrameEnd(request.PeerKeys, request.Indices,
                request.Start, request.Count, request.Current)
        };

    private static decimal? ShiftRangeTarget(decimal? current, int offset, bool descending)
    {
        if (!current.HasValue || offset == 0)
            return current;
        if (descending)
            return checked(current.Value - offset);
        return checked(current.Value + offset);
    }

    private static int CompareNumericRangeKeys(decimal? left, decimal? right, bool descending, bool nullsFirst)
    {
        if (!left.HasValue)
            return !right.HasValue ? 0 : nullsFirst ? -1 : 1;
        if (!right.HasValue)
            return nullsFirst ? 1 : -1;
        if (descending)
            return right.Value.CompareTo(left.Value);
        return left.Value.CompareTo(right.Value);
    }

    private enum RangeBoundary { Start, End }
    private readonly record struct RangeFrameRequest<TOrder>(TOrder[] PeerKeys, int[] Indices,
        int Start, int Count, int Current, int Offset, bool Descending, bool NullsFirst);

    private interface IRangeNumericKeys
    {
        decimal? ReadDecimal(int index);
    }

    private readonly struct RangeNumericKeys<TNumber>(TNumber[] values) : IRangeNumericKeys
        where TNumber : struct, INumber<TNumber>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal? ReadDecimal(int index) => decimal.CreateChecked(values[index]);
    }

    private readonly struct NullableRangeNumericKeys<TNumber>(TNumber?[] values) : IRangeNumericKeys
        where TNumber : struct, INumber<TNumber>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal? ReadDecimal(int index)
        {
            var value = values[index];
            if (!value.HasValue)
                return null;
            return decimal.CreateChecked(value.Value);
        }
    }
}
