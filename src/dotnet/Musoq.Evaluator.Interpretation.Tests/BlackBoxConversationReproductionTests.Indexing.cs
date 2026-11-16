using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

public sealed partial class BlackBoxConversationReproductionTests
{
    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void InlineScalarCountBesideInterpretation_ShouldReturnValues(QueryInstrumentationMode mode)
    {
        const string sql = """
            binary Rec { Value: byte };
            select d.Value as Value, (select Count(*) from #test.files() counted) as Population
            from #test.files() f cross apply Interpret<Rec>(f.Content) d
            """;

        using var table = RunQuery(sql, mode, [7]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, 1L]);
    }

    [TestMethod]
    [TestCategory("BlackBoxControl")]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void IntegerCastProjection_ShouldExposeNullableIntegerMetadata(QueryInstrumentationMode mode)
    {
        const string sql = """
            binary Rec { Position: byte };
            select (r.Position - 1)::int as Position
            from #test.files() f cross apply Interpret<Rec>(f.Content) r
            """;

        using var table = RunQuery(sql, mode, [2]);

        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1]);
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled, "")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, "")]
    [DataRow(QueryInstrumentationMode.Full, "")]
    [DataRow(QueryInstrumentationMode.Disabled, "holders h cross join ")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, "holders h cross join ")]
    [DataRow(QueryInstrumentationMode.Full, "holders h cross join ")]
    public void MinimalInterpretedProjectionWithScalarCount_ShouldPreserveSchemaBinding(
        QueryInstrumentationMode mode,
        string joinedSource
    )
    {
        var sql = $$"""
            binary Rec { Value: byte };
            with holders as (select 1 as Holder from #test.files() source)
            select d.Value as Value, (select Count(*) from holders counted) as HolderCount
            from {{joinedSource}}#test.files() f cross apply Interpret<Rec>(f.Content) d
            """;

        using var table = RunQuery(sql, mode, [7]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, 1L]);
    }

    [TestMethod]
    [TestCategory("BlackBoxControl")]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void ScalarCountWithoutInterpretation_ShouldReturnTheCtePopulation(QueryInstrumentationMode mode)
    {
        const string sql = """
            with holders as (select 1 as Holder from #test.files() source)
            select 7 as Value, (select Count(*) from holders counted) as HolderCount
            from #test.files() f
            """;

        using var table = RunQuery(sql, mode, [7]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [7, 1L]);
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    [TestCategory("BlackBoxControl")]
    public void CoalescedSourceFieldIndexExpression_ShouldSelectTheSecondActor(QueryInstrumentationMode mode)
    {
        var sql =
            CatalogSchema
            + """

                select d.Actors[((d.Position - 1)::int ?? 0)].Label as Label
                from #test.files() f cross apply Interpret<Rec>(f.Content) d
                """;

        using var table = RunQuery(sql, mode, CatalogRecord);

        TableMaterializationTestHelper.AssertColumns(table, ("Label", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["B"]);
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled, "r.Position - 1")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, "r.Position - 1")]
    [DataRow(QueryInstrumentationMode.Full, "r.Position - 1")]
    [DataRow(QueryInstrumentationMode.Disabled, "((r.Position - 1)::int ?? 0)")]
    [DataRow(QueryInstrumentationMode.SourceBoundaries, "((r.Position - 1)::int ?? 0)")]
    [DataRow(QueryInstrumentationMode.Full, "((r.Position - 1)::int ?? 0)")]
    public void MinimalIndexExpression_ShouldSelectTheRequestedMember(QueryInstrumentationMode mode, string index)
    {
        var sql = $$"""
            binary Item { Value: byte }; binary Rec { Position: byte, Items: Item[2] };
            select r.Items[{{index}}].Value as Value
            from #test.files() f cross apply Interpret<Rec>(f.Content) r
            """;

        using var table = RunQuery(sql, mode, [2, 10, 20]);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(byte?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)20]);
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void CteHolderUsedToIndexJoinedDictionary_ShouldSelectTheSecondActor(QueryInstrumentationMode mode)
    {
        var sql =
            CatalogSchema
            + HolderCte
            + """

                select d.Actors[((h.Holder - 1)::int ?? 0)].Label as Label
                from holders h cross join #test.files() f
                cross apply Interpret<Rec>(f.Content) d
                """;

        using var table = RunQuery(sql, mode, CatalogRecord);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["B"]);
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void JoinedDictionaryIndexWithScalarCount_ShouldPreserveCollectionType(QueryInstrumentationMode mode)
    {
        var sql =
            CatalogSchema
            + HolderCte
            + """

                select d.Actors[((h.Holder - 1)::int ?? 0)].Label as Label,
                       (select Count(*) from holders counted) as HolderCount
                from holders h cross join #test.files() f
                cross apply Interpret<Rec>(f.Content) d
                """;

        using var table = RunQuery(sql, mode, CatalogRecord);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["B", 1L]);
    }

    [TestMethod]
    [TestCategory("BlackBoxControl")]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void NullableIndexWithoutCoalescing_ShouldReportIndexTypeDiagnostic(QueryInstrumentationMode mode)
    {
        const string sql = """
            binary Item { Value: byte }; binary Rec { Position: byte, Items: Item[2] };
            select r.Items[(r.Position - 1)::int].Value
            from #test.files() f cross apply Interpret<Rec>(f.Content) r
            """;

        AssertIndexTypeDiagnostic(sql, mode, [2, 10, 20]);
    }

    [TestMethod]
    [DataRow(QueryInstrumentationMode.Disabled)]
    [DataRow(QueryInstrumentationMode.SourceBoundaries)]
    [DataRow(QueryInstrumentationMode.Full)]
    public void NullableIndexWithScalarCount_ShouldReportIndexTypeDiagnostic(QueryInstrumentationMode mode)
    {
        var sql =
            CatalogSchema
            + HolderCte
            + """

                select d.Actors[(h.Holder - 1)::int].Label as Label,
                       (select Count(*) from holders counted) as HolderCount
                from holders h cross join #test.files() f
                cross apply Interpret<Rec>(f.Content) d
                """;

        AssertIndexTypeDiagnostic(sql, mode, CatalogRecord);
    }

    private void AssertIndexTypeDiagnostic(string sql, QueryInstrumentationMode mode, params byte[][] records)
    {
        var exception = Assert.Throws<MusoqQueryException>(() =>
        {
            using var table = RunQuery(sql, mode, records);
        });
        TestContext.WriteLine($"DIAGNOSTIC: {exception}");
        Assert.AreEqual(DiagnosticCode.MQ3018_NoIndexer, exception.PrimaryEnvelope.Code);
    }
}
