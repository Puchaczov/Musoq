using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class ResultAssertionsTests
{
    [TestMethod]
    [DataRow("value")]
    [DataRow("missing")]
    [DataRow("extra")]
    [DataRow("multiplicity")]
    [DataRow("type")]
    [DataRow("null")]
    [DataRow("columns")]
    [DataRow("width")]
    public void Rows_WhenOutputIsCorrupted_ShouldRejectIt(string corruption)
    {
        object?[][] expected = [[1, "a"], [1, "a"], [2, null]];
        object?[][] actual = corruption switch
        {
            "value" => [[1, "a"], [1, "a"], [99, null]],
            "missing" => [[1, "a"], [2, null]],
            "extra" => [[1, "a"], [1, "a"], [2, null], [3, "b"]],
            "multiplicity" => [[1, "a"], [2, null], [2, null]],
            "type" => [[1, "a"], [1, "a"], [2L, null]],
            "null" => [[1, "a"], [1, "a"], [2, ""]],
            "columns" => [["a", 1], ["a", 1], [null, 2]],
            "width" => [[1, "a"], [1, "a"], [2]],
            _ => throw new ArgumentOutOfRangeException(nameof(corruption))
        };

        Assert.ThrowsExactly<AssertFailedException>(() => ResultAssertions.Rows(expected, actual, false, corruption));
    }

    [TestMethod]
    public void Rows_WhenUnordered_ShouldAcceptPermutationAndPreserveDuplicates()
    {
        ResultAssertions.Rows([[1, null], [1, null], [2, "b"]], [[2, "b"], [1, null], [1, null]], false, "permutation");
        ResultAssertions.Rows([], [], false, "empty");
    }

    [TestMethod]
    public void Rows_WhenOrdered_ShouldRejectPermutation()
    {
        Assert.ThrowsExactly<AssertFailedException>(() =>
            ResultAssertions.Rows([[1], [2]], [[2], [1]], true, "order"));
    }

    [TestMethod]
    public void Rows_WhenOrderedValuesAreCorrupted_ShouldRejectThem()
    {
        Assert.ThrowsExactly<AssertFailedException>(() =>
            ResultAssertions.Rows([[1, "a"]], [[2, "a"]], true, "ordered-value"));
    }

    [TestMethod]
    public void Rows_FloatingValues_ShouldUseExactTypedEquality()
    {
        ResultAssertions.Rows([[double.NaN], [double.PositiveInfinity]], [[double.NaN], [double.PositiveInfinity]], true, "special-values");
        Assert.ThrowsExactly<AssertFailedException>(() => ResultAssertions.Rows([[1d]], [[1d + 1e-12]], true, "no-implicit-tolerance"));
        Assert.ThrowsExactly<AssertFailedException>(() => ResultAssertions.Rows([[1d]], [[1f]], false, "numeric-type"));
    }

    [TestMethod]
    [DataRow("name")]
    [DataRow("type")]
    [DataRow("missing")]
    [DataRow("extra")]
    [DataRow("index")]
    public void Table_WhenSchemaIsCorrupted_ShouldRejectIt(string corruption)
    {
        var scenario = new Scenario("schema-control", "control", "schema", "", "");
        ExpectedResult expected = new([("Value", typeof(int))], []);
        Column[] columns = corruption switch
        {
            "name" => [new("Other", typeof(int), 0)],
            "type" => [new("Value", typeof(long), 0)],
            "missing" => [],
            "extra" => [new("Value", typeof(int), 0), new("Extra", typeof(int), 1)],
            "index" => [new("Value", typeof(int), 1)],
            _ => throw new ArgumentOutOfRangeException(nameof(corruption))
        };
        using var table = new Table("control", columns);

        Assert.ThrowsExactly<AssertFailedException>(() => ResultAssertions.Table(expected, table, scenario));
    }

    [TestMethod]
    [DataRow("missing")]
    [DataRow("duplicate-and-missing")]
    [DataRow("negative")]
    [DataRow("outside")]
    public void Values_WhenVisitationIsMalformed_ShouldRejectIt(string mode)
    {
        Assert.ThrowsExactly<AssertFailedException>(() => ResultAssertions.Values(new MalformedRow(mode)));
    }

    private sealed class MalformedRow(string mode) : Row
    {
        public override int Count => mode == "duplicate-and-missing" ? 2 : 1;

        public override void VisitColumns<TVisitor>(ref TVisitor visitor)
        {
            if (mode == "missing") return;
            object? value = null;
            visitor.Visit(mode switch { "negative" => -1, "outside" => Count, _ => 0 }, in value);
            if (mode == "duplicate-and-missing") visitor.Visit(0, in value);
        }
    }
}
