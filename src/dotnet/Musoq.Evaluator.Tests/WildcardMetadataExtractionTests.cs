using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Lexing;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class WildcardMetadataExtractionTests
{
    [TestMethod]
    public void DirectStar_WithPredicate_ShouldRequestCompleteSchema()
    {
        var columns = ExtractColumns("select * from #A.entities() a where a.Id > 0");

        var sourceColumns = columns.Single().Value;

        Assert.AreEqual(0, sourceColumns.Length);
    }

    [TestMethod]
    public void QualifiedStar_WithJoinPredicate_ShouldRequestCompleteSchemaOnlyForQualifiedSource()
    {
        var columns = ExtractColumns(
            "select a.* from #A.entities() a inner join #B.entities() b on a.Id = b.Id");

        var qualifiedSource = columns.Single(pair => pair.Key.StartsWith("a", System.StringComparison.OrdinalIgnoreCase));
        var joinedSource = columns.Single(pair => pair.Key.StartsWith("b", System.StringComparison.OrdinalIgnoreCase));

        Assert.AreEqual(0, qualifiedSource.Value.Length);
        CollectionAssert.AreEquivalent(new[] { "Id" }, joinedSource.Value);
    }

    [TestMethod]
    public void BareStar_WithJoinPredicate_ShouldRequestCompleteSchemaForEveryCurrentSource()
    {
        var columns = ExtractColumns(
            "select * from #A.entities() a inner join #B.entities() b on a.Id = b.Id");

        Assert.HasCount(2, columns);
        Assert.IsTrue(columns.Values.All(static sourceColumns => sourceColumns.Length == 0));
    }

    [TestMethod]
    public void CountStar_WithPredicate_ShouldRetainPredicateColumnHint()
    {
        var columns = ExtractColumns("select Count(*) from #A.entities() a where a.Id > 0");

        var sourceColumns = columns.Single().Value;

        CollectionAssert.AreEquivalent(new[] { "Id" }, sourceColumns);
    }

    [TestMethod]
    public void CustomAggregateStar_WithPredicate_ShouldRetainPredicateColumnHint()
    {
        var columns = ExtractColumns("select CustomRowCount(*) from #A.entities() a where a.Id > 0");

        var sourceColumns = columns.Single().Value;

        CollectionAssert.AreEquivalent(new[] { "Id" }, sourceColumns);
    }

    [TestMethod]
    public void ConstantProjection_WithPredicate_ShouldNotRequestCompleteSchema()
    {
        var columns = ExtractColumns("select 1 from #A.entities() a where a.Id > 0");

        var sourceColumns = columns.Single().Value;

        CollectionAssert.AreEquivalent(new[] { "Id" }, sourceColumns);
    }

    [TestMethod]
    public void PivotMeasureStar_ShouldRetainPivotColumnHints()
    {
        var columns = ExtractColumns(
            "pivot #A.entities() on Name in ('Ada' as Ada) using Count(*) as Matches group by Id");

        var sourceColumns = columns.Single().Value;

        Assert.IsTrue(sourceColumns.Length > 0);
        CollectionAssert.AreEquivalent(new[] { "Name", "Id" }, sourceColumns);
    }

    private static IReadOnlyDictionary<string, string[]> ExtractColumns(string query)
    {
        var parser = new Musoq.Parser.Parser(new Lexer(query, true));
        var tree = parser.ComposeAll();
        var visitor = new ExtractRawColumnsVisitor();

        tree.Accept(new ExtractRawColumnsTraverseVisitor(visitor));

        return visitor.Columns;
    }
}
