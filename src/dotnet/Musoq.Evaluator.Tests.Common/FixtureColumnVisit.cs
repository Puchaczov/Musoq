using System;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

internal static class FixtureColumnVisit
{
    internal static void Apply<TVisitor>(int index, object? value, ref TVisitor visitor)
        where TVisitor : IColumnValueVisitor, allows ref struct
    {
        switch (value)
        {
            case int typed: TypedColumnVisit.Apply(index, typed, ref visitor); break;
            case long typed: TypedColumnVisit.Apply(index, typed, ref visitor); break;
            case decimal typed: TypedColumnVisit.Apply(index, typed, ref visitor); break;
            case double typed: TypedColumnVisit.Apply(index, typed, ref visitor); break;
            case float typed: TypedColumnVisit.Apply(index, typed, ref visitor); break;
            case string typed: TypedColumnVisit.Apply(index, typed, ref visitor); break;
            case bool typed: TypedColumnVisit.Apply(index, typed, ref visitor); break;
            case DateTime typed: TypedColumnVisit.Apply(index, typed, ref visitor); break;
            default: TypedColumnVisit.Apply(index, value, ref visitor); break;
        }
    }
}
