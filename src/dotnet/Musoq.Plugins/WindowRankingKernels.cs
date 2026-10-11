using System.Runtime.CompilerServices;

namespace Musoq.Plugins;

#pragma warning disable CS1591

public static class RowNumberKernel
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Get(in WindowRankingContext context) => context.Position + 1L;
}

public static class RankKernel
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Get(in WindowRankingContext context) => context.PeerStart + 1L;
}

public static class DenseRankKernel
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Get(in WindowRankingContext context) => context.DenseRank;
}

public static class PercentRankKernel
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Get(in WindowRankingContext context)
        => context.PartitionCount == 1 ? 0d : (double)context.PeerStart / (context.PartitionCount - 1);
}

public static class CumeDistKernel
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Get(in WindowRankingContext context)
        => (double)(context.PeerEnd + 1) / context.PartitionCount;
}
