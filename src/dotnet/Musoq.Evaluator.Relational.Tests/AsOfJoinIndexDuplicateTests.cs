using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AsOfJoinIndexDuplicateTests
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void BackwardLookupFindsWinnerInLogarithmicComparisons(bool strict, bool tieBreak)
    {
        const int runLength = 4096;
        var counter = new ComparisonCounter();
        var rows = Enumerable
            .Range(0, runLength + 2)
            .Select(i => new Candidate(
                i,
                new Key(
                    i == 0 ? 0
                        : i > runLength ? 3
                        : 1,
                    counter
                )
            ))
            .ToArray();
        var comparison = strict ? BinaryOpKind.GreaterThan : BinaryOpKind.GreaterOrEqual;
        var index = tieBreak
            ? AsOfJoinIndex<Candidate, Key, int>.Create(
                rows,
                null,
                r => r.Key,
                comparison,
                r => r.Ordinal,
                true,
                default
            )
            : AsOfJoinIndex<Candidate, Key, int>.Create(rows, null, r => r.Key, comparison);

        counter.Count = 0;
        var match = index.Find(default, new Key(strict ? 2 : 1, counter));

        Assert.AreSame(rows[tieBreak ? runLength : 1], match);
        Assert.IsLessThanOrEqualTo(32, counter.Count);
    }

    private sealed record Candidate(int Ordinal, Key Key);

    private sealed class ComparisonCounter
    {
        public int Count { get; set; }
    }

    private readonly record struct Key(int Value, ComparisonCounter Counter) : IComparable<Key>
    {
        public int CompareTo(Key other)
        {
            Counter.Count++;
            return Value.CompareTo(other.Value);
        }
    }
}
