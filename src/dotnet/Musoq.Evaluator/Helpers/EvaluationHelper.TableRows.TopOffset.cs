using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    private static Row[] CopySlicedRows(IEnumerable<Row> rows, int skipCount, int takeCount)
    {
        var selectedRows = new Row[takeCount];
        var selectedCount = 0;
        var skippedCount = 0;
        var skipLimit = Math.Max(skipCount, 0);

        foreach (var row in rows)
        {
            if (skippedCount < skipLimit)
            {
                skippedCount++;
                continue;
            }

            selectedRows[selectedCount] = row;
            selectedCount++;

            if (selectedCount == takeCount)
                return selectedRows;
        }

        if (selectedCount == 0)
            return Array.Empty<Row>();

        var trimmedRows = new Row[selectedCount];
        Array.Copy(selectedRows, trimmedRows, selectedCount);
        return trimmedRows;
    }

    private static void AppendSlicedRowsDirect(IEnumerable<Row> rows, Table target, int skipCount, int takeCount)
    {
        var selectedCount = 0;
        var skippedCount = 0;
        var skipLimit = Math.Max(skipCount, 0);

        foreach (var row in rows)
        {
            if (skippedCount < skipLimit)
            {
                skippedCount++;
                continue;
            }

            target.AddDirect(row);
            selectedCount++;

            if (selectedCount == takeCount)
                return;
        }
    }

    private static PriorityQueue<TopOffsetRow, TopOffsetRow> CollectTopOffsetCandidates(
        IEnumerable<Row> rows,
        int limit,
        IReadOnlyList<RowOrderKey> orderKeys)
    {
        var rowComparer = new TopOffsetRowComparer(orderKeys);
        var heapComparer = new ReverseTopOffsetRowComparer(rowComparer);
        var queue = new PriorityQueue<TopOffsetRow, TopOffsetRow>(heapComparer);
        var ordinal = 0;

        foreach (var row in rows)
        {
            var candidate = new TopOffsetRow(row, ordinal);
            ordinal++;

            if (queue.Count < limit)
            {
                queue.Enqueue(candidate, candidate);
                continue;
            }

            var worstKept = queue.Peek();
            if (rowComparer.Compare(candidate, worstKept) >= 0)
                continue;

            queue.Dequeue();
            queue.Enqueue(candidate, candidate);
        }

        return queue;
    }

    private static Row[] CopySelectedTopOffsetRows(
        PriorityQueue<TopOffsetRow, TopOffsetRow> queue,
        int skipCount,
        int takeCount)
    {
        var candidates = DrainTopOffsetCandidates(queue);
        var startIndex = Math.Min(Math.Max(skipCount, 0), candidates.Length);
        var selectedCount = Math.Min(takeCount, candidates.Length - startIndex);
        if (selectedCount <= 0)
            return Array.Empty<Row>();

        var selectedRows = new Row[selectedCount];
        for (var index = 0; index < selectedRows.Length; index++)
            selectedRows[index] = candidates[startIndex + index].Row;

        return selectedRows;
    }

    private static void AppendSelectedTopOffsetRowsDirect(
        PriorityQueue<TopOffsetRow, TopOffsetRow> queue,
        Table target,
        int skipCount,
        int takeCount)
    {
        var candidates = DrainTopOffsetCandidates(queue);
        var startIndex = Math.Min(Math.Max(skipCount, 0), candidates.Length);
        var endIndex = Math.Min(startIndex + takeCount, candidates.Length);

        for (var index = startIndex; index < endIndex; index++)
            target.AddDirect(candidates[index].Row);
    }

    private static TopOffsetRow[] DrainTopOffsetCandidates(PriorityQueue<TopOffsetRow, TopOffsetRow> queue)
    {
        if (queue.Count == 0)
            return [];

        var candidates = new TopOffsetRow[queue.Count];
        for (var index = candidates.Length - 1; index >= 0; index--)
            candidates[index] = queue.Dequeue();

        return candidates;
    }

    private static PriorityQueue<TopOffsetRecord<T>, TopOffsetRecord<T>> CollectTopOffsetCandidates<T>(
        IEnumerable<T> rows,
        int limit,
        IComparer<T> comparer)
    {
        var recordComparer = new TopOffsetRecordComparer<T>(comparer);
        var heapComparer = new ReverseTopOffsetRecordComparer<T>(recordComparer);
        var queue = new PriorityQueue<TopOffsetRecord<T>, TopOffsetRecord<T>>(heapComparer);
        var ordinal = 0;

        foreach (var row in rows)
        {
            var candidate = new TopOffsetRecord<T>(row, ordinal);
            ordinal++;

            if (queue.Count < limit)
            {
                queue.Enqueue(candidate, candidate);
                continue;
            }

            var worstKept = queue.Peek();
            if (recordComparer.Compare(candidate, worstKept) >= 0)
                continue;

            queue.Dequeue();
            queue.Enqueue(candidate, candidate);
        }

        return queue;
    }

    private static List<T> CopySelectedTopOffsetRecords<T>(
        PriorityQueue<TopOffsetRecord<T>, TopOffsetRecord<T>> queue,
        int skipCount,
        int takeCount)
    {
        var candidates = DrainTopOffsetCandidates(queue);
        var startIndex = Math.Min(Math.Max(skipCount, 0), candidates.Length);
        var selectedCount = Math.Min(takeCount, candidates.Length - startIndex);
        if (selectedCount <= 0)
            return [];

        var selectedRows = new List<T>(selectedCount);
        for (var index = 0; index < selectedCount; index++)
            selectedRows.Add(candidates[startIndex + index].Record);

        return selectedRows;
    }

    private static List<T> CopySelectedTopOffsetRecords<T>(
        List<T> rows,
        int skipCount,
        int takeCount)
    {
        var startIndex = Math.Min(Math.Max(skipCount, 0), rows.Count);
        var selectedCount = Math.Min(takeCount, rows.Count - startIndex);
        if (selectedCount <= 0)
            return [];

        var selectedRows = new List<T>(selectedCount);
        for (var index = 0; index < selectedCount; index++)
            selectedRows.Add(rows[startIndex + index]);

        return selectedRows;
    }

    private static TopOffsetRecord<T>[] DrainTopOffsetCandidates<T>(
        PriorityQueue<TopOffsetRecord<T>, TopOffsetRecord<T>> queue)
    {
        if (queue.Count == 0)
            return [];

        var candidates = new TopOffsetRecord<T>[queue.Count];
        for (var index = candidates.Length - 1; index >= 0; index--)
            candidates[index] = queue.Dequeue();

        return candidates;
    }

    private static int CalculateTopOffsetLimit(int skipCount, int takeCount)
    {
        var limit = (long)Math.Max(skipCount, 0) + takeCount;
        return limit > int.MaxValue ? int.MaxValue : (int)limit;
    }

}
