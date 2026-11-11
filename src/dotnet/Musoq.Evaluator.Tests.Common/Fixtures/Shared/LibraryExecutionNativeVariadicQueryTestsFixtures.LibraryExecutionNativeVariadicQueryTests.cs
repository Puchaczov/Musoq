using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionNativeVariadicQueryTestsFixtures
{
    internal const string Query =
        "select Position, NativeTotal(Value, 1, 2, 3, 4, 5) as Total, "
        + "Concat(Position, Position, Position, Position, Position, Position) as Repeated "
        + "from #schema.first() order by Position";

    public sealed record Input(int Position, int Value);

    public sealed class SpanLibrary : LibraryBase
    {
        [BindableMethod]
        public T NativeNumberTotal<T>(params ReadOnlySpan<T> values)
            where T : struct, System.Numerics.INumber<T>
        {
            var total = T.Zero;
            foreach (var value in values)
                total = checked(total + value);
            return total;
        }

        [BindableMethod]
        public int OldArrayTotal(params int[] values) => values.Sum();

        [BindableMethod]
        public int NativeTotal(params ReadOnlySpan<int> values)
        {
            var total = 0;
            foreach (var value in values)
                total = checked(total + value);
            return total;
        }
    }
}
