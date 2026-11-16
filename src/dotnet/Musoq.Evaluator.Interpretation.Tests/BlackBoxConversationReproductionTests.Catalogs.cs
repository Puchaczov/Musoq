using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class BlackBoxConversationReproductionTests
{
    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled, "")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, "")]
    [DataRow(QueryInstrumentationMode.Full, "")]
    [DataRow(QueryInstrumentationMode.Disabled, " order by e.Catalog, e.Id")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, " order by e.Catalog, e.Id")]
    [DataRow(QueryInstrumentationMode.Full, " order by e.Catalog, e.Id")]
    public void MinimalNamedCatalogUnion_ShouldPreserveBindings(QueryInstrumentationMode mode, string ordering)
    {
        var sql =
            """
                binary Entry { Id: byte, Label: string[1] ascii };
                binary Rec { Actors: Entry[1], Badges: Entry[1] };
                with entries as (
                    select 'actor' as Catalog, e.Id::int as Id, e.Label as Label
                    from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Actors e
                    union all
                    select 'badge' as Catalog, e.Id::int as Id, e.Label as Label
                    from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Badges e
                )
                select e.Catalog, e.Id, e.Label from entries e
                """ + ordering;

        using var table = RunQuery(sql, mode, [7, 65, 8, 66]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["actor", 7, "A"], ["badge", 8, "B"]);
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled, 1, "e", "")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, 1, "e", "")]
    [DataRow(QueryInstrumentationMode.Full, 1, "e", "")]
    [DataRow(QueryInstrumentationMode.Disabled, 2, "e", "")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, 2, "e", "")]
    [DataRow(QueryInstrumentationMode.Full, 2, "e", "")]
    [DataRow(QueryInstrumentationMode.Disabled, 2, "e", " order by e.Id")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, 2, "e", " order by e.Id")]
    [DataRow(QueryInstrumentationMode.Full, 2, "e", " order by e.Id")]
    [DataRow(QueryInstrumentationMode.Disabled, 2, "i", "")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, 2, "i", "")]
    [DataRow(QueryInstrumentationMode.Full, 2, "i", "")]
    public void MinimalInterpretedMemberCte_ShouldPreserveBindings(
        QueryInstrumentationMode mode,
        int branches,
        string innerAlias,
        string ordering
    )
    {
        const string schema = "binary Entry { Id: byte }; binary Rec { Items: Entry[1] };";
        var branch = $"""
            select {innerAlias}.Id::int as Id from #test.files() f
            cross apply Interpret<Rec>(f.Content) r cross apply r.Items {innerAlias}
            """;
        var union = string.Join("\nunion all\n", Enumerable.Repeat(branch, branches));
        var sql = $"{schema} with entries as ({union}) select e.Id from entries e{ordering}";

        using var table = RunQuery(sql, mode, [7]);

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            Enumerable.Repeat(new object?[] { 7 }, branches).ToArray()
        );
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled, "")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, "")]
    [DataRow(QueryInstrumentationMode.Full, "")]
    [DataRow(QueryInstrumentationMode.Disabled, " with ordinality")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, " with ordinality")]
    [DataRow(QueryInstrumentationMode.Full, " with ordinality")]
    public void InterpretedCollectionExpansion_ShouldReturnOrderedMembers(
        QueryInstrumentationMode mode,
        string ordinality
    )
    {
        var sql =
            CatalogSchema
            + $"""

                select e.Id::int as Id, e.Label as Label
                from #test.files() f cross apply Interpret<Rec>(f.Content) d
                cross apply d.Actors e{ordinality} order by e.Id
                """;

        using var table = RunQuery(sql, mode, CatalogRecord);

        TableMaterializationTestHelper.AssertColumns(table, ("Id", typeof(int?)), ("Label", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "A"], [2, "B"]);
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void InterpretedCatalogUnionCte_ShouldPreserveColumnBindings(QueryInstrumentationMode mode)
    {
        var sql =
            CatalogSchema
            + """

                with entries as (
                    select 'actor' as Catalog, e.Id::int as Id, e.Label as Label
                    from #test.files() f cross apply Interpret<Rec>(f.Content) d cross apply d.Actors e
                    union all
                    select 'badge' as Catalog, e.Id::int as Id, e.Label as Label
                    from #test.files() f cross apply Interpret<Rec>(f.Content) d cross apply d.Badges e
                )
                select e.Catalog as Catalog, e.Id as Id, e.Label as Label
                from entries e order by e.Catalog, e.Id
                """;

        using var table = RunQuery(sql, mode, CatalogRecord);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Catalog", typeof(string)),
            ("Id", typeof(int?)),
            ("Label", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["actor", 1, "A"],
            ["actor", 2, "B"],
            ["badge", 11, "X"],
            ["badge", 12, "Y"]
        );
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void CatalogArrayCarriedThroughCte_ShouldExpandItsMembers(QueryInstrumentationMode mode)
    {
        var sql =
            CatalogSchema
            + """

                with catalogs as (
                    select d.Actors as Actors
                    from #test.files() f cross apply Interpret<Rec>(f.Content) d
                )
                select e.Id::int as Id, e.Label as Label
                from catalogs c cross apply c.Actors e order by e.Id
                """;

        using var table = RunQuery(sql, mode, CatalogRecord);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "A"], [2, "B"]);
    }

    [TestMethod]
    [TestCategory("BlackBoxControl")]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void LiteralCatalogIndexes_ShouldReadBothCollections(QueryInstrumentationMode mode)
    {
        var sql =
            CatalogSchema
            + """

                select d.Actors[0].Id::int as ActorId, d.Actors[0].Label as Actor,
                       d.Badges[1].Id::int as BadgeId, d.Badges[1].Label as Badge
                from #test.files() f cross apply Interpret<Rec>(f.Content) d
                """;

        using var table = RunQuery(sql, mode, CatalogRecord);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [1, "A", 12, "Y"]);
    }

    [TestMethod]
    [TestCategory("BlackBoxControl")]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void IndividualBadgeCatalogBranch_ShouldReturnOrderedMembers(QueryInstrumentationMode mode)
    {
        var sql =
            CatalogSchema
            + """

                select e.Id::int as Id, e.Label as Label
                from #test.files() f cross apply Interpret<Rec>(f.Content) d
                cross apply d.Badges e order by e.Id
                """;

        using var table = RunQuery(sql, mode, CatalogRecord);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [11, "X"], [12, "Y"]);
    }
}
