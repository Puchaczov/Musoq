using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks;

internal static class RecursiveCteHandwrittenBaseline
{
    public static Table Execute(
        RecursiveCteBenchmarkFixture fixture,
        CancellationToken cancellationToken = default) =>
        fixture.IsWide
            ? ExecuteWide(fixture, cancellationToken)
            : ExecuteNarrow(fixture, cancellationToken);

    private static Table ExecuteNarrow(
        RecursiveCteBenchmarkFixture fixture,
        CancellationToken cancellationToken)
    {
        var result = new List<State>();
        var current = new List<State>();
        var next = new List<State>();
        var seen = fixture.UsesIdentity ? new HashSet<int>() : null;
        var maximumRows = Math.Max(32, fixture.Edges.Length + 2);
        var maximumIterations = Math.Max(32, maximumRows);
        var candidateCount = 0;
        var iteration = 0;
        if (fixture.HasAnchor)
        {
            var anchor = new State(1, 0);
            EnsureRowLimit(result.Count + current.Count, maximumRows);
            result.Add(anchor);
            current.Add(anchor);
            seen?.Add(anchor.Id);
        }

        if (current.Count == 0)
            return CreateNarrowTable(result);

        var snapshot = fixture.Edges.ToArray();
        var lookup = fixture.Scenario is
            RecursiveCteBenchmarkScenario.InvariantSnapshot or
            RecursiveCteBenchmarkScenario.CorrelatedApply
                ? null
                : BuildLookup(snapshot);
        while (current.Count > 0)
        {
            if ((iteration & 63) == 0)
                cancellationToken.ThrowIfCancellationRequested();
            EnsureIterationLimit(iteration, maximumIterations);
            iteration++;
            next.Clear();
            foreach (var state in current)
            {
                if (lookup != null)
                {
                    if (!lookup.TryGetValue(state.Id, out var matches))
                        continue;
                    foreach (var edge in matches)
                        TryAccept(
                            new State(edge.TargetId, state.Depth + 1),
                            seen,
                            result,
                            next,
                            maximumRows,
                            cancellationToken,
                            ref candidateCount);
                }
                else
                {
                    foreach (var edge in snapshot)
                        if (edge.SourceId == state.Id)
                            TryAccept(
                                new State(edge.TargetId, state.Depth + 1),
                                seen,
                                result,
                                next,
                                maximumRows,
                                cancellationToken,
                                ref candidateCount);
                }
            }

            result.AddRange(next);
            (current, next) = (next, current);
        }

        return CreateNarrowTable(result);
    }

    private static Table ExecuteWide(
        RecursiveCteBenchmarkFixture fixture,
        CancellationToken cancellationToken)
    {
        var result = new List<WideState>();
        var current = new List<WideState>();
        var next = new List<WideState>();
        var seen = new HashSet<int> { 1 };
        var anchor = new WideState(1, 0, 10, 20, 30, 40, 50, 60);
        var maximumRows = Math.Max(32, fixture.Edges.Length + 2);
        var maximumIterations = Math.Max(32, maximumRows);
        var candidateCount = 0;
        var iteration = 0;
        EnsureRowLimit(result.Count + current.Count, maximumRows);
        result.Add(anchor);
        current.Add(anchor);
        var lookup = BuildLookup(fixture.Edges.ToArray());

        while (current.Count > 0)
        {
            if ((iteration & 63) == 0)
                cancellationToken.ThrowIfCancellationRequested();
            EnsureIterationLimit(iteration, maximumIterations);
            iteration++;
            next.Clear();
            foreach (var state in current)
            {
                if (!lookup.TryGetValue(state.Id, out var matches))
                    continue;
                foreach (var edge in matches)
                {
                    candidateCount++;
                    if ((candidateCount & 1023) == 0)
                        cancellationToken.ThrowIfCancellationRequested();
                    if (!seen.Add(edge.TargetId))
                        continue;
                    EnsureRowLimit(result.Count + next.Count, maximumRows);
                    next.Add(new WideState(
                        edge.TargetId,
                        state.Depth + 1,
                        state.P1 + edge.Weight,
                        state.P2 + edge.Weight,
                        state.P3 + edge.Weight,
                        state.P4 + edge.Weight,
                        state.P5 + edge.Weight,
                        state.P6 + edge.Weight));
                }
            }

            result.AddRange(next);
            (current, next) = (next, current);
        }

        return CreateWideTable(result);
    }

    private static Dictionary<int, List<RecursiveCteBenchmarkEdge>> BuildLookup(
        IEnumerable<RecursiveCteBenchmarkEdge> edges)
    {
        var lookup = new Dictionary<int, List<RecursiveCteBenchmarkEdge>>();
        foreach (var edge in edges)
        {
            if (!lookup.TryGetValue(edge.SourceId, out var matches))
            {
                matches = [];
                lookup.Add(edge.SourceId, matches);
            }

            matches.Add(edge);
        }

        return lookup;
    }

    private static void TryAccept(
        State candidate,
        ISet<int>? seen,
        IReadOnlyCollection<State> result,
        ICollection<State> target,
        int maximumRows,
        CancellationToken cancellationToken,
        ref int candidateCount)
    {
        candidateCount++;
        if ((candidateCount & 1023) == 0)
            cancellationToken.ThrowIfCancellationRequested();
        if (seen != null && !seen.Add(candidate.Id))
            return;
        EnsureRowLimit(result.Count + target.Count, maximumRows);
        target.Add(candidate);
    }

    private static void EnsureIterationLimit(int iteration, int maximumIterations)
    {
        if (iteration >= maximumIterations)
            throw new InvalidOperationException("Recursive CTE benchmark iteration limit exceeded.");
    }

    private static void EnsureRowLimit(int acceptedRows, int maximumRows)
    {
        if (acceptedRows >= maximumRows)
            throw new InvalidOperationException("Recursive CTE benchmark row limit exceeded.");
    }

    private static Table CreateNarrowTable(IReadOnlyList<State> states)
    {
        var projected = new List<NarrowProjection>();
        for (var index = 0; index < states.Count; index++)
            projected.Add(new NarrowProjection(states[index].Id, states[index].Depth));
        var table = new Table("result",
        [
            new Column("Id", typeof(int), 0),
            new Column("Depth", typeof(int), 1)
        ]);
        foreach (var state in projected)
            table.Add(new NarrowResultRow(state.Id, state.Depth));
        return table;
    }

    private static Table CreateWideTable(IReadOnlyList<WideState> states)
    {
        var projected = new List<WideProjection>();
        for (var index = 0; index < states.Count; index++)
            projected.Add(new WideProjection(states[index]));
        var table = new Table("result",
        [
            new Column("Id", typeof(int), 0),
            new Column("Depth", typeof(int), 1),
            new Column("P1", typeof(int), 2),
            new Column("P2", typeof(int), 3),
            new Column("P3", typeof(int), 4),
            new Column("P4", typeof(int), 5),
            new Column("P5", typeof(int), 6),
            new Column("P6", typeof(int), 7)
        ]);
        foreach (var state in projected)
            table.Add(new WideResultRow(state.State));
        return table;
    }

    private readonly record struct State(int Id, int Depth);

    private sealed record NarrowProjection(int Id, int Depth);

    private readonly record struct WideState(
        int Id,
        int Depth,
        int P1,
        int P2,
        int P3,
        int P4,
        int P5,
        int P6);

    private sealed record WideProjection(WideState State);

    private sealed class NarrowResultRow(int id, int depth) : Row
    {
        public override int Count => 2;

        public override object this[int columnNumber] => columnNumber switch
        {
            0 => id,
            1 => depth,
            _ => throw new IndexOutOfRangeException()
        };
    }

    private sealed class WideResultRow(WideState state) : Row
    {
        public override int Count => 8;

        public override object this[int columnNumber] => columnNumber switch
        {
            0 => state.Id,
            1 => state.Depth,
            2 => state.P1,
            3 => state.P2,
            4 => state.P3,
            5 => state.P4,
            6 => state.P5,
            7 => state.P6,
            _ => throw new IndexOutOfRangeException()
        };
    }
}
