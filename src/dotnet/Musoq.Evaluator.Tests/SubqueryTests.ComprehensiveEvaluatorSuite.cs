using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public partial class SubqueryTests
{
    [TestMethod]
    public void Comprehensive_CteValuesSetOperatorsDistinctExistsInPagination_ShouldEvaluate()
    {
        const string query = @"
            WITH allowed AS (
                FROM values {
                    { Country: 'POLAND' },
                    { Country: 'FRANCE' },
                    { Country: 'FRANCE' }
                } v
                SELECT DISTINCT v.Country
            ),
            filtered AS (
                SELECT a.City, a.Country, a.Population
                FROM #A.entities() a
                WHERE a.Country IN (
                    SELECT al.Country FROM allowed al
                    UNION (Country)
                    SELECT b.Country FROM #B.entities() b
                    WHERE b.Population > 600
                )
            )
            SELECT f.City
            FROM filtered f
            WHERE EXISTS (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = f.Country
            )
            ORDER BY f.City
            SKIP 1
            TAKE 2";

        var table = CreateAndRunVirtualMachine(query, CreateComprehensiveSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("f.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["GDANSK"], ["PARIS"]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("PhysicalCte", inspection.PhysicalPlanText);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void Comprehensive_ParametersFunctionsCaseNullsAndCoercion_ShouldEvaluate()
    {
        const string query = @"
            param(matchCountry: string, minScore: decimal)
            SELECT a.Name,
                   CASE
                       WHEN ToUpper(a.Country) = ToUpper($matchCountry)
                       THEN Concat(a.Name, ':match')
                       ELSE 'miss'
                   END AS Verdict
            FROM #A.entities() a
            WHERE a.Population >= $minScore
              AND (
                   a.NullableValue IN (
                       SELECT b.Id FROM #B.entities() b
                       WHERE b.Country = $matchCountry
                   )
                   OR a.NullableValue IS NULL
              )
            ORDER BY a.Name";

        var vm = CreateAndRunVirtualMachine(query, CreateComprehensiveSources());
        vm.Parameters["matchCountry"] = "POLAND";
        vm.Parameters["minScore"] = 200m;

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("Verdict", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Adam", "Adam:match"],
            ["Alice", "Alice:match"],
            ["Cara", "miss"]);
    }

    [TestMethod]
    public void Comprehensive_ScalarSubqueriesWithGroupingHavingAndOrderBy_ShouldEvaluate()
    {
        const string query = @"
            SELECT a.Country,
                   Sum(a.Population) AS TotalPopulation
            FROM #A.entities() a
            GROUP BY a.Country
            HAVING Sum(a.Population) > (
                SELECT Min(c.Population) FROM #C.entities() c
                WHERE c.Country = a.Country
            )
            ORDER BY a.Country DESC";

        var table = CreateAndRunVirtualMachine(query, CreateComprehensiveSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Country", typeof(string)),
            ("TotalPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["POLAND", 700m],
            ["FRANCE", 300m]);

        var inspection = CompileSubqueryForInspection(query);
        Assert.Contains("-> ScalarHashSingle", inspection.PlanningText);
        Assert.Contains("PhysicalHashJoin [LeftSingle]", inspection.PhysicalPlanText);
    }

    [TestMethod]
    public void Comprehensive_WindowQualifyAndQuantifiedSubquery_ShouldEvaluate()
    {
        const string query = @"
            SELECT a.City,
                   RowNumber() OVER (PARTITION BY a.Country ORDER BY a.Population DESC) AS RankInCountry
            FROM #A.entities() a
            WHERE a.Population >= ANY (
                SELECT b.Population FROM #B.entities() b
                WHERE b.Country = a.Country
            )
            QUALIFY RowNumber() OVER (PARTITION BY a.Country ORDER BY a.Population DESC) = (
                SELECT 1 FROM #C.entities() c
                WHERE c.City = a.City
            )
            ORDER BY a.City";

        var table = CreateAndRunVirtualMachine(query, CreateComprehensiveSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("RankInCountry", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["BERLIN", 1L],
            ["WARSAW", 1L]);
    }

    [TestMethod]
    public void Comprehensive_DerivedTablesJoinAndApply_ShouldEvaluate()
    {
        const string query = @"
            SELECT a.City, j.City, applied.MatchCount
            FROM (
                SELECT src.City, src.Country
                FROM #A.entities() src
                WHERE src.City IN ('WARSAW', 'PARIS')
            ) a
            INNER JOIN (
                WITH local AS (
                    SELECT b.City, b.Country FROM #B.entities() b
                )
                SELECT l.City, l.Country
                FROM local l
                WHERE l.City <> 'LODZ' AND l.City <> 'HAMBURG'
            ) j ON a.Country = j.Country
            OUTER APPLY (
                SELECT c.Country, Count(c.City) AS MatchCount
                FROM #C.entities() c
                WHERE c.Country = a.Country
                GROUP BY c.Country
            ) applied
            ORDER BY a.City, j.City";

        var table = CreateAndRunVirtualMachine(query, CreateComprehensiveSources()).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("j.City", typeof(string)),
            ("applied.MatchCount", typeof(long?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["PARIS", "LYON", 2L],
            ["WARSAW", "KRAKOW", 2L]);
    }

    [TestMethod]
    public void Comprehensive_InvalidSubqueryDiagnostics_ShouldUseSpecificCodes()
    {
        AssertDiagnostic(
            @"
            SELECT (
                SELECT b.City, b.Country FROM #B.entities() b
            ) AS BadScalar
            FROM #A.entities() a",
            DiagnosticCode.MQ2024_InvalidSubquery);

        AssertDiagnostic(
            @"
            SELECT a.City FROM #A.entities() a
            WHERE a.Population > ALL (
                SELECT b.Population FROM #B.entities() b
                UNION (Population)
                SELECT c.Population FROM #C.entities() c
            )",
            DiagnosticCode.MQ2024_InvalidSubquery);

        AssertDiagnostic(
            @"
            SELECT a.City, d.City FROM #A.entities() a
            INNER JOIN (
                SELECT b.City, b.Country FROM #B.entities() b
                WHERE b.Country = a.Country
            ) d ON a.Country = d.Country",
            DiagnosticCode.MQ2024_InvalidSubquery);

        AssertDiagnostic(
            @"
            SELECT a.City, d.City FROM #A.entities() a
            CROSS APPLY (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = a.Country
            ) d",
            DiagnosticCode.MQ2024_InvalidSubquery);
    }

    [TestMethod]
    public void Comprehensive_ScalarSubqueryReturningMultipleRows_ShouldFailAtRuntime()
    {
        const string query = @"
            SELECT (
                SELECT b.City FROM #B.entities() b
                WHERE b.Country = 'POLAND'
            ) AS City
            FROM #A.entities() a";

        var vm = CreateAndRunVirtualMachine(query, CreateComprehensiveSources());

        Assert.Throws<InvalidOperationException>(() => _ = vm.Run(TestContext.CancellationToken).Count);
    }

    private void AssertDiagnostic(string query, DiagnosticCode expectedCode)
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, CreateComprehensiveSources()));

        Assert.IsTrue(
            exception.Envelopes.Any(envelope => envelope.Code == expectedCode),
            $"Expected diagnostic {expectedCode}, got {string.Join(", ", exception.Envelopes.Select(envelope => envelope.Code))}.");
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateComprehensiveSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity { Name = "Alice", City = "WARSAW", Country = "POLAND", Population = 500m, NullableValue = 1, Id = 10 },
                    new BasicEntity { Name = "Adam", City = "GDANSK", Country = "POLAND", Population = 200m, NullableValue = 3, Id = 11 },
                    new BasicEntity { Name = "Bob", City = "BERLIN", Country = "GERMANY", Population = 250m, NullableValue = 2, Id = 12 },
                    new BasicEntity { Name = "Cara", City = "PARIS", Country = "FRANCE", Population = 300m, NullableValue = null, Id = 13 },
                    new BasicEntity { Name = "Dino", City = "ROME", Country = "ITALY", Population = 150m, NullableValue = 4, Id = 14 }
                ]
            },
            {
                "#B", [
                    new BasicEntity { Name = "P1", City = "KRAKOW", Country = "POLAND", Population = 100m, Id = 1 },
                    new BasicEntity { Name = "P2", City = "LODZ", Country = "POLAND", Population = 120m, Id = 3 },
                    new BasicEntity { Name = "G1", City = "MUNICH", Country = "GERMANY", Population = 700m, Id = 2 },
                    new BasicEntity { Name = "G2", City = "HAMBURG", Country = "GERMANY", Population = 200m, Id = 4 },
                    new BasicEntity { Name = "F1", City = "LYON", Country = "FRANCE", Population = 450m, Id = 5 }
                ]
            },
            {
                "#C", [
                    new BasicEntity { Name = "CP1", City = "WARSAW", Country = "POLAND", Population = 700m, Id = 1 },
                    new BasicEntity { Name = "CP2", City = "GDANSK", Country = "POLAND", Population = 100m, Id = 2 },
                    new BasicEntity { Name = "CG1", City = "BERLIN", Country = "GERMANY", Population = 300m, Id = 3 },
                    new BasicEntity { Name = "CF1", City = "PARIS", Country = "FRANCE", Population = 250m, Id = 4 },
                    new BasicEntity { Name = "CF2", City = "NICE", Country = "FRANCE", Population = 260m, Id = 5 }
                ]
            }
        };
    }
}
