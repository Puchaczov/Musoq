using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionCoverageQueryTestsFixtures
{
    public const string JoinedFrameQuery = """
        with joined as (
            select a.Position as Position, a.Partition as Partition,
                a.Value + b.Value as Value
            from #schema.first() a left join #schema.first() b on b.Position = a.Position - 1)
        select Position, Partition,
            Sum(Value) over (partition by Partition order by Position
                rows between 2 preceding and 1 following) as Total,
            Avg(Value) over (partition by Partition order by Position
                rows between 2 preceding and 1 following) as Average,
            Count(Value) over (partition by Partition order by Position
                rows between 2 preceding and 1 following) as Present,
            Lag(Value, 1) over (partition by Partition order by Position) as Previous,
            RowNumber() over (partition by Partition order by Position) as Number
        from joined qualify Number <= 5 order by Position
        """;

    public static CoverageRow[] CreateRows(int seed)
    {
        var random = new Random(seed);
        return Enumerable.Range(0, 180).Select(position => new CoverageRow(position,
            position % 9 == 8 ? null : position % 9,
            position % 9 == 7 || random.Next(5) == 0 ? null : random.Next(-100, 101))).ToArray();
    }

    internal static FrameReference[] CalculateJoinedFrames(CoverageRow[] input)
    {
        var joined = input.Select(row => row with
        {
            Value = row.Position == 0 ? null : row.Value + input[row.Position - 1].Value
        });
        return joined.GroupBy(row => row.Partition).SelectMany(partition =>
        {
            var ordered = partition.OrderBy(row => row.Position).ToArray();
            return Enumerable.Range(0, Math.Min(5, ordered.Length)).Select(index => CalculateFrame(ordered, index));
        }).OrderBy(row => row.Position).ToArray();
    }

    internal static FrameReference CalculateFrame(CoverageRow[] ordered, int index)
    {
        int? total = null;
        long count = 0;
        for (var frame = Math.Max(0, index - 2); frame <= Math.Min(ordered.Length - 1, index + 1); frame++)
        {
            if (ordered[frame].Value is not { } value)
                continue;
            total = checked(total.GetValueOrDefault() + value);
            count++;
        }
        decimal? average = count == 0 ? null : (decimal)total!.Value / count;
        return new FrameReference(ordered[index].Position, ordered[index].Partition, total, average, count,
            index == 0 ? null : ordered[index - 1].Value, index + 1L);
    }

    public sealed record CoverageRow(int Position, int? Partition, int? Value);
    internal sealed record FrameReference(int Position, int? Partition, int? Total, decimal? Average,
        long Present, int? Previous, long Number);

}
