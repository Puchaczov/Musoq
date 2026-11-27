using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class NumericBoundaryResultTests : BasicEntityTestBase
{
    [TestMethod]
    public void LeftShift_ShouldMaskCountsToThePromotedOperandWidth()
    {
        const string sql =
            "select v << 32 as I32, v << 33 as I33, l << 64 as I64, l << 65 as I65 from values {(v: 1i, l: 1l)} s";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("I32", typeof(int)),
            ("I33", typeof(int)),
            ("I64", typeof(long)),
            ("I65", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, 2, 1L, 2L]);
    }

    [TestMethod]
    public void RightShift_ShouldPreserveSignedAndUnsignedWidthsAndMaskCounts()
    {
        const string sql =
            "select u >> 32 as U32, u >> 1 as U1, l >> 64 as U64, l >> 1 as Wide, n >> 1 as Negative from values {(u: 4294967295ui, l: 18446744073709551615ul, n: -8i)} s";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("U32", typeof(uint)),
            ("U1", typeof(uint)),
            ("U64", typeof(ulong)),
            ("Wide", typeof(ulong)),
            ("Negative", typeof(int))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [uint.MaxValue, 2147483647u, ulong.MaxValue, 9223372036854775807ul, -4]
        );
    }

    [TestMethod]
    public void WideShiftCounts_ShouldUseOnlyLowBitsAndPropagateNull()
    {
        const string sql = """
            select s.n << s.c as WideLeft, s.b >> s.c as SmallRight, s.b << s.c as SmallLeft
            from values {
                (id: 1, n: 3ul, b: 128ub, c: 4294967297ul),
                (id: 2, n: 3ul, b: 128ub, c: null)
            } s order by id
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("WideLeft", typeof(ulong?)),
            ("SmallRight", typeof(int?)),
            ("SmallLeft", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [6ul, 64, 256], [null, null, null]);
    }

    [TestMethod]
    public void DecimalDivision_ShouldRoundScaleReductionTiesToEven()
    {
        const string sql = """
            select n / 2 as Rounded from values {
                (n: -0.0000000000000000000000000003d), (n: -0.0000000000000000000000000001d),
                (n: 0.0000000000000000000000000001d), (n: 0.0000000000000000000000000003d)
            } s order by n
            """;
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        using var table = query.Run(TestContext.CancellationToken);
        const decimal twoUnits = 0.0000000000000000000000000002m;
        TableMaterializationTestHelper.AssertColumns(table, ("Rounded", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [-twoUnits], [0m], [0m], [twoUnits]);
    }

    [TestMethod]
    public void DecimalAddition_BeyondMaximum_ShouldReportRuntimeOverflow()
    {
        const string sql = "select n + 1 from values {(n: 79228162514264337593543950335d)} s";
        using var query = CreateAndRunVirtualMachine(sql, new Dictionary<string, IEnumerable<BasicEntity>>());
        var error = Assert.ThrowsExactly<QueryExecutionException>(() =>
        {
            using var table = query.Run(TestContext.CancellationToken);
            _ = table.Count;
        });
        Assert.AreEqual(DiagnosticCode.MQ7018_NumericOverflow, error.Envelope!.Code);
    }
}
