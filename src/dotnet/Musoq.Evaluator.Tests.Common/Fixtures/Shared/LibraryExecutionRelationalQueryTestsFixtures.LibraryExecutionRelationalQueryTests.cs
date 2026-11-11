using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionRelationalQueryTestsFixtures
{
    public sealed record StructListEntity(NumericList Amounts);

    public readonly struct NumericList(decimal[] values) : IReadOnlyList<decimal>
    {
        public int Count => values.Length;
        public decimal this[int index] => values[index];

        public IEnumerator<decimal> GetEnumerator() => ((IEnumerable<decimal>)values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed record IndexedEntity(
        int Position,
        int[] Numbers,
        decimal?[] Amounts,
        Dictionary<string, decimal> Costs
    );

    public readonly record struct IndexedOutput(
        int Position,
        int Last,
        int Missing,
        decimal? NullableAmount,
        decimal Cost,
        decimal MissingCost
    );

    public sealed record EnumerableEntity(IEnumerable<int> Items);

    public readonly record struct IntegerOutput(int Value);

    public readonly record struct ScalarOutput(string Value);
}
