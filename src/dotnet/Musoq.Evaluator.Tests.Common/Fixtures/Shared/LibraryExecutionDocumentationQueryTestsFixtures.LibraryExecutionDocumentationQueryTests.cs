using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.TypedLibraryExamples;

namespace Musoq.Evaluator.Tests;

public partial class LibraryExecutionDocumentationQueryTestsFixtures
{

    internal static GroupedTotalVisitor VisitGroupedTotals(Row[] rows)
    {
        var visitor = new GroupedTotalVisitor();
        foreach (var row in rows)
            row.VisitColumns(ref visitor);
        return visitor;
    }

    internal struct GroupedTotalVisitor : IColumnValueVisitor
    {
        public decimal Total;
        public int Values;

        public void Visit<T>(int columnIndex, in T value)
        {
            if (columnIndex != 1)
                return;

            Assert.AreEqual(typeof(decimal?), typeof(T));
            var copy = value;
            Total += Unsafe.As<T, decimal?>(ref copy).GetValueOrDefault();
            Values++;
        }
    }

    public sealed record Invoice(int Position, string Customer, decimal? Amount, int? Quantity);
    public static Invoice[] Rows() =>
    [
        new(1, "A", 1.25m, 1), new(2, "A", 2.5m, 2),
        new(3, "A", null, null), new(4, "B", -1m, 4)
    ];

}
