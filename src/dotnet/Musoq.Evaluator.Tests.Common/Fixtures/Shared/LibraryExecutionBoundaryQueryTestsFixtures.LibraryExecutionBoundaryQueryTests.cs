using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.LibraryExecution;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionBoundaryQueryTestsFixtures
{
    public readonly record struct ErasedAmountRow(object Amount);

    public sealed record FrameworkCollectionEntity(ArrayList Value);

    public readonly record struct TextRow(string Text);

    internal static ColumnValueVisitor VisitColumnValues(Row[] rows)
    {
        var visitor = new ColumnValueVisitor();
        foreach (var row in rows)
            row.VisitColumns(ref visitor);
        return visitor;
    }

    internal struct ColumnValueVisitor : IColumnValueVisitor
    {
        public int Positions;
        public decimal Amounts;

        public void Visit<T>(int columnIndex, in T value)
        {
            if (columnIndex == 0)
                Positions += Unsafe.As<T, int>(ref Unsafe.AsRef(in value));
            else
                Amounts += Unsafe.As<T, decimal?>(ref Unsafe.AsRef(in value)).GetValueOrDefault();
        }
    }

    public readonly record struct PositionRow(int Position);

    public sealed record ObjectFieldEntity(object Value);

    public sealed record NestedObjectEntity(ObjectFieldEntity Payload);

    public readonly record struct AmountRow(decimal? Amount);

    public struct FunctionRow
    {
        public string Method { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Source { get; set; }
    }

    internal struct NumericVisitor : IColumnValueVisitor
    {
        public int Values;
        public int Positions;
        public decimal Amounts;
        public int Nulls;

        public void Visit<T>(int columnIndex, in T value)
        {
            Values++;
            if (columnIndex == 0)
            {
                Assert.AreEqual(typeof(int), typeof(T));
                Positions += Unsafe.As<T, int>(ref Unsafe.AsRef(in value));
                return;
            }

            Assert.AreEqual(typeof(decimal?), typeof(T));
            var amount = Unsafe.As<T, decimal?>(ref Unsafe.AsRef(in value));
            Amounts += amount.GetValueOrDefault();
            if (!amount.HasValue)
                Nulls++;
        }
    }
}
