namespace Musoq.Evaluator.Tests;

public sealed class ExtractionCases
{
    private const string Prefix = "select ";
    private const string SharedQuery = "select 'shared' as Value from #A.Entities()";

    [TestMethod]
    public void RegularEscapes()
    {
        var query = "select 'a\n\t\"b\\c' as Value from #A.Entities()";
        var result = FixtureRuntime.Compile(query).Run();
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void UnicodeEscapes()
    {
        var query = "select '\x41\u03A9\U0001F600' as Value from #A.Entities()";
        var result = FixtureRuntime.Compile(query).Run();
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void VerbatimQuotes()
    {
        var query = @"select 'a""b\c' as Value from #A.Entities()";
        var result = FixtureRuntime.Compile(query).Run();
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void RawIndentation()
    {
        var query = """
            select 'raw' as Value

            from #A.Entities()
            """;
        var result = FixtureRuntime.Compile(query).Run();
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void ConstantConcatenation()
    {
        const string name = "'constant'";
        var query = Prefix + name + " as Value from #A.Entities()";
        var result = FixtureRuntime.Compile(query).Run();
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void ConstantArgument()
    {
        var result = FixtureRuntime.Compile(SharedQuery).Run();
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void Pivot()
    {
        var result = FixtureRuntime.Compile("pivot #A.Entities() on Name in ('a' as A) using Count(*)").Run();
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void Unpivot()
    {
        var result = FixtureRuntime.Compile("unpivot #A.Entities() e on K in (e.Id as Id) using Value").Run();
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void RepeatedExpression()
    {
        var first = FixtureRuntime.Compile("select 1 from #A.Entities()").Run();
        var second = FixtureRuntime.Compile("select 1 from #A.Entities()").Run();
        Assert.AreEqual(first, second);
    }

    [TestMethod]
    [DataRow("a")]
    [DataRow("b")]
    public void EscapedInterpolationBraces(string name)
    {
        var query = $"select '{{escaped}} {name}' as Value from #A.Entities()";
        var result = FixtureRuntime.Compile(query).Run();
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void UnsupportedInterpolation(string name)
    {
        var query = $"select {name.ToUpperInvariant()} from #A.Entities()";
        var result = FixtureRuntime.Compile(query).Run();
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void UnknownHelper()
    {
        var result = Run("select 'unknown-helper' from #A.Entities()");
        Assert.AreEqual(1, result);
    }

    private static int Run(string query) => FixtureRuntime.Compile(query).Run();
}

internal static class RecursiveCteUnsupportedCaseCatalog
{
    private const string Anchor = "select seed.Value from values {( Value: 1 )} seed";

    public static string RecursiveCast() =>
        "with recursive counter (Value) as ("
        + Anchor
        + " union all select (c.Value + 1)::Decimal from counter c where c.Value < 3) select Value from counter";
}

public sealed class JoinSemiAntiCrossJoinTests
{
    [TestMethod]
    public void RegisteredWrapper()
    {
        var result = RunJoinQuery("select a.Name from #A.Entities() a semi join #B.Entities() b on a.Id = b.Id");
        Assert.AreEqual(1, result);
    }

    private static int RunJoinQuery(string query, CompilationOptions? options = null) =>
        FixtureRuntime.Compile(query).Run();
}

public sealed class Core017SemiAntiPresenceTests
{
    [TestMethod]
    [DataRow("semi join")]
    [DataRow("left semi join")]
    public void SemiJoin(string joinOperator)
    {
        var query = $"select a.Id from #A.Entities() a {joinOperator} #B.Entities() b on a.Id = b.Id";
        var result = Run(query, new Dictionary<string, IEnumerable<Schema.Basic.BasicEntity>>());
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    [DataRow("anti join")]
    [DataRow("anti semi join")]
    [DataRow("left anti semi join")]
    public void AntiJoin(string joinOperator)
    {
        var query = $"select a.Id from #A.Entities() a {joinOperator} #B.Entities() b on a.Id = b.Id";
        var result = Run(query, new Dictionary<string, IEnumerable<Schema.Basic.BasicEntity>>());
        Assert.AreEqual(1, result);
    }

    private static int Run(string query, IDictionary<string, IEnumerable<Schema.Basic.BasicEntity>> sources) =>
        FixtureRuntime.Compile(query).Run();
}

internal static class FixtureRuntime
{
    public static FixtureQuery Compile(string text) => new(text);
}

internal sealed record FixtureQuery(string Text)
{
    public int Run() => 1;
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class TestMethodAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal sealed class DataRowAttribute(string value) : Attribute
{
    public string Value { get; } = value;
}

internal static class Assert
{
    public static void AreEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException("Fixture assertion values differ.");
    }
}
