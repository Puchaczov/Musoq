using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticRework082SelectContractTests : BasicEntityTestBase
{
    [TestMethod]
    public void ProjectionForms_ShouldPreserveValuesTypesAndExplicitNames()
    {
        const string query =
            "select 42 as Literal, a.Name as QualifiedName, a.Population + 1 as Raised, " +
            "a.GetPopulation() as MethodValue, a.Self.Name as NestedName, a.Array[1] as ArrayItem " +
            "from #A.Entities() a";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(new BasicEntity("Ada") { Population = 9m })).Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Literal", typeof(int)),
            ("QualifiedName", typeof(string)),
            ("Raised", typeof(decimal)),
            ("MethodValue", typeof(decimal)),
            ("NestedName", typeof(string)),
            ("ArrayItem", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [42, "Ada", 10m, 9m, "Ada", 1]);
    }

    [TestMethod]
    public void AliasForms_ShouldPreserveExplicitImplicitBracketedAndDerivedNames()
    {
        const string query =
            "select Name as ExplicitName, City ImplicitName, Country [Country Label], 1 " +
            "from #A.Entities()";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(new BasicEntity("Ada") { City = "Warsaw", Country = "PL" }))
            .Run(TokenSource.Token);

        CollectionAssert.AreEqual(
            new[] { "ExplicitName", "ImplicitName", "Country Label", "1" },
            table.Columns.Select(static column => column.ColumnName).ToArray());
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Ada", "Warsaw", "PL", 1]);
    }

    [TestMethod]
    public void NonAggregateProjectionAlias_ShouldResolveInWhere()
    {
        const string query =
            "select Population + 1 as Adjusted, Name from #A.Entities() " +
            "where Adjusted > 100 order by Adjusted";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(
                new BasicEntity("low") { Population = 25m },
                new BasicEntity("high") { Population = 200m },
                new BasicEntity("middle") { Population = 100m }))
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [101m, "middle"], [201m, "high"]);
    }

    [TestMethod]
    public void GroupedAndAggregateProjectionAliases_ShouldResolveInGroupByAndHaving()
    {
        const string query =
            "select City as GroupCity, Count(Name) as RowCount from #A.Entities() " +
            "group by GroupCity having RowCount > 1 order by GroupCity";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(
                new BasicEntity("one") { City = "Oslo" },
                new BasicEntity("two") { City = "Oslo" },
                new BasicEntity("three") { City = "Rome" }))
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("GroupCity", typeof(string)),
            ("RowCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Oslo", 2L]);
    }

    [TestMethod]
    public void SourceColumn_ShouldWinOverSameNamedProjectionAlias()
    {
        const string query =
            "select Country as City, City as SourceCity from #A.Entities() " +
            "where City = 'Warsaw'";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(
                new BasicEntity("first") { City = "Warsaw", Country = "PL" },
                new BasicEntity("second") { City = "Berlin", Country = "DE" },
                new BasicEntity("third") { City = "Warsaw", Country = "FR" }))
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["PL", "Warsaw"],
            ["FR", "Warsaw"]);
    }

    [TestMethod]
    public void CteProjection_ShouldExposeConsumerNamesInsteadOfSourceQualifiedNames()
    {
        const string query = "with projected as (" +
            "select a.City as [a.City], a.Country from #A.Entities() a) " +
            "select [a.City], Country from projected order by [a.City]";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(
                new BasicEntity("one") { City = "Berlin", Country = "DE" },
                new BasicEntity("two") { City = "Warsaw", Country = "PL" }))
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("Country", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", "DE"],
            ["Warsaw", "PL"]);
    }

    [TestMethod]
    public void WildcardExpansion_ShouldPreservePrimitiveSourceOrderAndExplicitProjection()
    {
        const string query = "select *, Name as Name2 from #A.Entities()";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(new BasicEntity("Ada") { City = "Warsaw", Country = "PL", Id = 7 }))
            .Run(TokenSource.Token);

        CollectionAssert.AreEqual(
            new[] { "Name", "City", "Country", "Population", "Money", "Month", "Time", "Id", "NullableValue", "Name2" },
            table.Columns.Select(static column => column.ColumnName).ToArray());
        Assert.AreEqual("Ada", table[0][0]);
        Assert.AreEqual(7, table[0][7]);
        Assert.AreEqual("Ada", table[0][9]);
    }

    [TestMethod]
    public void QualifiedWildcardExpansion_ShouldPreserveEachSourcePrefix()
    {
        const string query =
            "select a.*, b.* from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] = [new BasicEntity("left") { Id = 7, City = "Warsaw" }],
            ["#B"] = [new BasicEntity("right") { Id = 7, City = "Warsaw" }]
        };

        var table = CreateAndRunVirtualMachine(query, sources).Run(TokenSource.Token);
        var primitiveColumns = new[]
        {
            "Name", "City", "Country", "Population", "Money", "Month", "Time", "Id", "NullableValue"
        };
        var expectedNames = primitiveColumns
            .Select(column => $"a.{column}")
            .Concat(primitiveColumns.Select(column => $"b.{column}"))
            .ToArray();

        CollectionAssert.AreEqual(expectedNames, table.Columns.Select(static column => column.ColumnName).ToArray());
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual("right", table[0][9]);
    }

    [TestMethod]
    public void WildcardThroughCte_ShouldExportUnqualifiedColumns()
    {
        const string query =
            "with projected as (select a.* from #A.Entities() a) select * from projected";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(new BasicEntity("Ada") { City = "Warsaw", Country = "PL" }))
            .Run(TokenSource.Token);

        CollectionAssert.AreEqual(
            new[] { "Name", "City", "Country", "Population", "Money", "Month", "Time", "Id", "NullableValue" },
            table.Columns.Select(static column => column.ColumnName).ToArray());
        Assert.AreEqual("Ada", table[0][0]);
    }

    [TestMethod]
    public void Distinct_ShouldDeduplicateCompleteRowsWithOrdinalComparison()
    {
        const string query =
            "select distinct City, Country from #A.Entities() order by City, Country";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(
                new BasicEntity("one") { City = "Warsaw", Country = "POLAND" },
                new BasicEntity("two") { City = "Warsaw", Country = "POLAND" },
                new BasicEntity("three") { City = "Warsaw", Country = "poland" },
                new BasicEntity("four") { City = "Berlin", Country = "POLAND" }))
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", "POLAND"],
            ["Warsaw", "POLAND"],
            ["Warsaw", "poland"]);
    }

    [TestMethod]
    public void RowNumber_ShouldCountFilteredRowsBeforeOrderedSkipAndTake()
    {
        const string query =
            "select Country, RowNumber() as Ordinal from #A.Entities() " +
            "where Population > 0 order by Country skip 1 take 2";

        var table = CreateAndRunVirtualMachine(
            query,
            CreateSingleSource(
                new BasicEntity("sweden") { Country = "Sweden", Population = 1m },
                new BasicEntity("germany") { Country = "Germany", Population = 2m },
                new BasicEntity("poland") { Country = "Poland", Population = 3m },
                new BasicEntity("norway") { Country = "Norway", Population = 4m },
                new BasicEntity("filtered") { Country = "Austria", Population = 0m }))
            .Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("Ordinal", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Norway", 2],
            ["Poland", 3]);
    }

    [TestMethod]
    [DataRow(
        "select from #A.Entities()",
        "from",
        DiagnosticCode.MQ2005_InvalidSelectList,
        "SELECT list cannot be empty.")]
    [DataRow(
        "select , Name from #A.Entities()",
        ",",
        DiagnosticCode.MQ2015_LeadingComma,
        "A SELECT list cannot begin with a comma.")]
    [DataRow(
        "select Name, from #A.Entities()",
        ",",
        DiagnosticCode.MQ2014_TrailingComma,
        "A SELECT list cannot end with a comma.")]
    public void MalformedSelectLists_ShouldExposeExactParseEnvelopes(
        string query,
        string offendingText,
        DiagnosticCode expectedCode,
        string expectedMessage)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(CreateSingleSource(new BasicEntity("row"))),
            LoggerResolver);
        var envelopes = result.ToEnvelopes().ToArray();

        Assert.HasCount(1, envelopes, string.Join(Environment.NewLine, envelopes.Select(static envelope => envelope.Message)));
        var envelope = envelopes.Single();
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedMessage, envelope.Message);
        Assert.AreEqual(query.IndexOf(offendingText, StringComparison.Ordinal), envelope.Offset);
        Assert.AreEqual(offendingText.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.DocsReference));
    }

    [TestMethod]
    public void UnknownProjectionColumn_ShouldExposeExactBindEnvelope()
    {
        const string query = "select Missing from #A.Entities()";
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(CreateSingleSource(new BasicEntity("row"))),
            LoggerResolver);
        var envelope = result.ToEnvelopes().Single();

        Assert.AreEqual(DiagnosticCode.MQ3001_UnknownColumn, envelope.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, envelope.Severity);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual("Unknown column 'Missing'.", envelope.Message);
        Assert.AreEqual(query.IndexOf("Missing", StringComparison.Ordinal), envelope.Offset);
        Assert.AreEqual("Missing".Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Snippet));
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.DocsReference));
    }
}
