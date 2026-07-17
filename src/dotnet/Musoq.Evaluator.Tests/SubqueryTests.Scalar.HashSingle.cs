using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void WhenCorrelatedScalarSubquery_DisablesGeneralHashJoins_ShouldStillUseHashSingle()
    {
        const string query = @"
            SELECT a.City, (
                SELECT c.City FROM #C.entities() c
                WHERE c.Country = a.Country
            ) AS MatchCity
            FROM #A.entities() a";
        var options = new CompilationOptions(
            useHashJoin: false,
            useSortMergeJoin: false,
            usePrimitiveTypeValidation: false);

        var table = CreateAndRunVirtualMachine(query, CreateScalarSources(), options)
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["WARSAW", "KRAKOW"],
            new object?[] { "BERLIN", null },
            ["PARIS", "PARIS"]);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_HasDuplicateRowsForUnprobedKey_ShouldNotThrow()
    {
        const string query = @"
            SELECT a.City, (
                SELECT c.City FROM #C.entities() c
                WHERE c.Country = a.Country
            ) AS MatchCity
            FROM #A.entities() a";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("WARSAW", "POLAND", 500)],
            ["#C"] =
            [
                new BasicEntity("KRAKOW", "POLAND", 10),
                new BasicEntity("PARIS", "FRANCE", 20),
                new BasicEntity("LYON", "FRANCE", 30)
            ]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertRowsUnordered(table, ["WARSAW", "KRAKOW"]);
    }

    [TestMethod]
    public void WhenCorrelatedScalarSubquery_HasDuplicateRowsForProbedKey_ShouldThrowAtProbe()
    {
        const string query = @"
            SELECT a.City, (
                SELECT c.City FROM #C.entities() c
                WHERE c.Country = a.Country
            ) AS MatchCity
            FROM #A.entities() a";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("PARIS", "FRANCE", 300)],
            ["#C"] =
            [
                new BasicEntity("PARIS", "FRANCE", 20),
                new BasicEntity("LYON", "FRANCE", 30)
            ]
        };
        var vm = CreateAndRunVirtualMachine(query, sources);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _ = vm.Run(TestContext.CancellationToken).Count);

        Assert.AreEqual("Scalar subquery returned more than one row.", exception.Message);
    }
}
