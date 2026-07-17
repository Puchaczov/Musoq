using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenCorrelatedScalarSubquery_UsesUnion_ShouldDeduplicatePerCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country AND (b.City = 'KRAKOW' OR b.City = 'PARIS')
                UNION (City)
                SELECT c.City FROM #C.entities() c
                WHERE c.Country = a.Country
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_UsesUnionAll_ShouldPreserveDuplicateCardinality()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country AND b.City = 'KRAKOW'
                UNION ALL (City)
                SELECT c.City FROM #C.entities() c
                WHERE c.Country = a.Country
            ) AS MatchCity
            FROM #A.entities() a";

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _ = CreateAndRunVirtualMachine(query, CreateScalarSources())
                .Run(TestContext.CancellationToken)
                .Count);

        Assert.AreEqual("Scalar subquery returned more than one row.", exception.Message);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_UsesExcept_ShouldSubtractInsideEachCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                EXCEPT (City)
                SELECT c.City FROM #C.entities() c
                WHERE c.Country = a.Country
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "GDANSK"],
            new object?[] { "BERLIN", null },
            new object?[] { "PARIS", null });
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_UsesIntersect_ShouldIntersectInsideEachCorrelationKey()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                INTERSECT (City)
                SELECT c.City FROM #C.entities() c
                WHERE c.Country = a.Country
            ) AS MatchCity
            FROM #A.entities() a";

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSetBranches_UseDifferentOuterKeys_ShouldExplainSharedKeyRequirement()
    {
        const string query = @"
            SELECT a.City, (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
                UNION (City)
                SELECT c.City FROM #C.entities() c
                WHERE c.City = a.City
            ) AS MatchCity
            FROM #A.entities() a";

        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateScalarSources()));

        Assert.IsTrue(exception.Envelopes.Any(envelope => envelope.Code == DiagnosticCode.MQ2024_InvalidSubquery));
        Assert.Contains("same equality correlation key", exception.Message);
    }
}
