using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class DiagnosticCross066CteSubqueryIntegrationTests : BasicEntityTestBase
{
    [TestMethod]
    public void OrdinaryCtes_SetDerivedAndPredicateSubqueries_ShouldPreserveExportedShape()
    {
        const string query = (
            "with eligible as (\n"
            + "    select a.Id as Id, a.Country as Country, a.City as City\n"
            + "    from #A.entities() a\n"
            + "    where a.Country in (\n"
            + "        select b.Country as Country from #B.entities() b\n"
            + "        union (Country)\n"
            + "        select c.Country as Country from #C.entities() c\n"
            + "    )\n"
            + "),\n"
            + "combined as (\n"
            + "    select Id, Country, City from eligible\n"
            + "    union all (Id, Country, City)\n"
            + "    select b.Id as Id, b.Country as Country, b.City as City\n"
            + "    from #B.entities() b\n"
            + ")\n"
            + "select d.City as City,\n"
            + "       (\n"
            + "           select Min(c.Population)\n"
            + "           from #C.entities() c\n"
            + "           where c.Country = d.Country\n"
            + "       ) as CountryFloor\n"
            + "from (\n"
            + "    select x.Id, x.Country, x.City\n"
            + "    from combined x\n"
            + "    where exists (\n"
            + "        select c.City from #C.entities() c\n"
            + "        where c.Country = x.Country\n"
            + "    )\n"
            + ") d\n"
            + "where d.Id >= any (select b.Id from #B.entities() b)\n"
            + "order by City"
        );

        var table = TableMaterializationTestHelper.Materialize(
            CreateAndRunVirtualMachine(query, CreateSources()).Run(TestContext.CancellationToken)
        );

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("CountryFloor", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["KRAKOW", 50m], ["LYON", 75m]);
    }

    [TestMethod]
    public void RecursiveCte_WithOrdinaryDependencyOuterSetAndSubqueries_ShouldEvaluate()
    {
        const string query = (
            "with recursive\n"
            + "    seeds (Id, Country) as (\n"
            + "        select a.Id, a.Country\n"
            + "        from #A.entities() a\n"
            + "        where exists (\n"
            + "            select b.Id from #B.entities() b\n"
            + "            where b.Country = a.Country\n"
            + "        )\n"
            + "    ),\n"
            + "    walk (Id, Country, Depth) as (\n"
            + "        select s.Id, s.Country, 0 from seeds s\n"
            + "        union (Id)\n"
            + "        select w.Id + 1, w.Country, w.Depth + 1\n"
            + "        from walk w\n"
            + "        where w.Depth < 1\n"
            + "    ),\n"
            + "    candidates as (\n"
            + "        select w.Id as Id, w.Country as Country, w.Depth as Depth\n"
            + "        from walk w\n"
            + "        union all (Id, Country, Depth)\n"
            + "        select c.Id as Id, c.Country as Country, 0 as Depth\n"
            + "        from #C.entities() c\n"
            + "    )\n"
            + "select d.Id as Id,\n"
            + "       d.Country as Country,\n"
            + "       (\n"
            + "           select Min(b.Population)\n"
            + "           from #B.entities() b\n"
            + "           where b.Country = d.Country\n"
            + "       ) as CountryFloor\n"
            + "from (\n"
            + "    select x.Id, x.Country, x.Depth\n"
            + "    from candidates x\n"
            + "    where x.Id = any (select s.Id from seeds s)\n"
            + ") d\n"
            + "where exists (\n"
            + "    select c.City from #C.entities() c\n"
            + "    where c.Country = d.Country\n"
            + ")\n"
            + "order by d.Id"
        );

        var table = TableMaterializationTestHelper.Materialize(
            CreateAndRunVirtualMachine(query, CreateSources()).Run(TestContext.CancellationToken)
        );

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Id", typeof(int)),
            ("Country", typeof(string)),
            ("CountryFloor", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "POLAND", 100m], [3, "FRANCE", 450m]);
    }

    [TestMethod]
    public void InvalidSubqueryShapes_ShouldReportPreciseQueryDiagnostics()
    {
        const string multipleInColumns =
            "select a.City from #A.entities() a where a.City in (select b.City, b.Country from #B.entities() b)";
        var inException = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(multipleInColumns, CreateSources())
        );
        AssertDiagnosticContract(
            inException.PrimaryEnvelope,
            DiagnosticCode.MQ3049_InSubqueryMultipleColumns,
            multipleInColumns,
            "select b.City, b.Country",
            "Subquery used with IN must return exactly one column."
        );
        Assert.AreEqual("Core Spec - IN Subqueries", inException.PrimaryEnvelope.DocsReference);

        const string correlatedDerived = (
            "select a.City, d.City\n"
            + "from #A.entities() a\n"
            + "inner join (\n"
            + "    select b.City, b.Country from #B.entities() b\n"
            + "    where b.Country = a.Country\n"
            + ") d on a.Country = d.Country"
        );
        var derivedException = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(correlatedDerived, CreateSources())
        );
        AssertDiagnosticContract(
            derivedException.PrimaryEnvelope,
            DiagnosticCode.MQ2024_InvalidSubquery,
            correlatedDerived,
            "b.City, b.Country",
            "Plain derived tables are not lateral"
        );
        StringAssert.Contains(derivedException.PrimaryEnvelope.Message, "Use CROSS APPLY or OUTER APPLY");
    }

    [TestMethod]
    public void InvalidRecursiveCombinations_ShouldReportFocusedShapeAndScopeDiagnostics()
    {
        const string nestedSelfReference = (
            "with recursive counter (Value) as (\n"
            + "    select seed.Value from values {( Value: 1 )} seed\n"
            + "    union all\n"
            + "    select seed.Value from values {( Value: 1 )} seed\n"
            + "    where exists (select c.Value from counter c)\n"
            + ")\n"
            + "select Value from counter"
        );
        var nestedException = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(nestedSelfReference, CreateSources())
        );
        AssertDiagnosticContract(
            nestedException.PrimaryEnvelope,
            DiagnosticCode.MQ3074_InvalidRecursiveCteReference,
            nestedSelfReference,
            "counter c",
            "nested query"
        );

        const string aggregateMember = (
            "with recursive counter (Value) as (\n"
            + "    select seed.Value from values {( Value: 1 )} seed\n"
            + "    union all\n"
            + "    select Count(c.Value) from counter c\n"
            + ")\n"
            + "select Value from counter"
        );
        var aggregateException = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(aggregateMember, CreateSources())
        );
        AssertDiagnosticContract(
            aggregateException.PrimaryEnvelope,
            DiagnosticCode.MQ3075_UnsupportedRecursiveCteOperator,
            aggregateMember,
            "Count",
            "aggregation"
        );
    }

    private static void AssertDiagnosticContract(
        MusoqErrorEnvelope envelope,
        DiagnosticCode expectedCode,
        string query,
        string spanFragment,
        string messageFragment
    )
    {
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhaseMapping.FromCode(expectedCode), envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.IsNotNull(envelope.Offset);
        Assert.IsNotNull(envelope.EndOffset);
        Assert.IsNotNull(envelope.Length);
        Assert.IsTrue(envelope.Offset >= 0);
        Assert.IsTrue(envelope.Length > 0);
        Assert.AreEqual(envelope.Offset.Value + envelope.Length.Value, envelope.EndOffset.Value);
        Assert.IsTrue(
            query
                .Substring(envelope.Offset.Value, envelope.Length.Value)
                .Contains(spanFragment, StringComparison.OrdinalIgnoreCase),
            $"Diagnostic span did not contain '{spanFragment}'."
        );
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.HasCount(envelope.SuggestedFixes.Count, envelope.Actions);
        Assert.IsTrue(
            envelope.Actions.All(static action =>
                action.Kind == DiagnosticActionKind.Suggestion && action.TextEdit is null
            )
        );
        StringAssert.Contains(envelope.Message, messageFragment);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity
                {
                    Id = 1,
                    City = "WARSAW",
                    Country = "POLAND",
                    Population = 500m,
                },
                new BasicEntity
                {
                    Id = 2,
                    City = "BERLIN",
                    Country = "GERMANY",
                    Population = 250m,
                },
                new BasicEntity
                {
                    Id = 3,
                    City = "PARIS",
                    Country = "FRANCE",
                    Population = 300m,
                },
            ],
            ["#B"] =
            [
                new BasicEntity
                {
                    Id = 10,
                    City = "KRAKOW",
                    Country = "POLAND",
                    Population = 100m,
                },
                new BasicEntity
                {
                    Id = 20,
                    City = "LYON",
                    Country = "FRANCE",
                    Population = 450m,
                },
                new BasicEntity
                {
                    Id = 30,
                    City = "MUNICH",
                    Country = "GERMANY",
                    Population = 700m,
                },
            ],
            ["#C"] =
            [
                new BasicEntity
                {
                    Id = 100,
                    City = "WARSAW-2",
                    Country = "POLAND",
                    Population = 50m,
                },
                new BasicEntity
                {
                    Id = 101,
                    City = "NICE",
                    Country = "FRANCE",
                    Population = 75m,
                },
            ],
        };
    }
}
