using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Tests;

public partial class RowAndKeyEqualityTests
{
    [TestMethod]
    public void TypedContextQuery_WhenSingleContext_ShouldPreserveItsConcreteValues()
    {
        const string query = "select s.ContextOne() as One, s.ContextText() as Text, s.ContextFraction() as Fraction " +
            "from #schema.first() s where s.Position = 1";
        using var compiled = CreateAndRunVirtualMachine<ContextInput, ContextLibrary>(query, ContextRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        var row = table.Single();
        TableMaterializationTestHelper.AssertColumns(table, ("One", typeof(int)), ("Text", typeof(string)), ("Fraction", typeof(double)));
        Assert.AreEqual(1, row.GetValue<int>(0));
        Assert.AreEqual("hello", row.GetValue<string>(1));
        Assert.AreEqual(3.14d, row.GetValue<double>(2));
    }

    [TestMethod]
    public void TypedContextQuery_WhenSingleMissingContext_ShouldExposeTypedNulls()
    {
        AssertMissingContext("select b.ContextOne() as One, b.ContextText() as Text, b.ContextFraction() as Fraction " +
            "from #schema.first() a left join #schema.first() b on b.Position = 99 where a.Position = 1");
    }

    [TestMethod]
    public void TypedContextQuery_WhenTheRightSourceIsEmpty_ShouldExposeTypedNulls()
    {
        AssertMissingContext("select b.ContextOne() as One, b.ContextText() as Text, b.ContextFraction() as Fraction " +
            "from #schema.first() a left join #schema.first(99) b " +
            "on a.Position = b.Position where a.Position = 1", emptyRightSource: true);
    }

    [TestMethod]
    public void TypedContextQuery_WhenMultipleContexts_ShouldPreserveSourceAndFieldOrder()
    {
        const string query = "select a.ContextOne() as A1, a.ContextTwo() as A2, b.ContextOne() as B1, " +
            "b.ContextTwo() as B2, b.ContextThree() as B3 from #schema.first() a " +
            "inner join #schema.first() b on a.Position = 1 and b.Position = 2";
        using var compiled = CreateAndRunVirtualMachine<ContextInput, ContextLibrary>(query, ContextRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        var row = table.Single();
        TableMaterializationTestHelper.AssertColumns(table, ("A1", typeof(int)), ("A2", typeof(int)),
            ("B1", typeof(int)), ("B2", typeof(int)), ("B3", typeof(int)));
        for (var index = 0; index < 5; index++) Assert.AreEqual(index + 1, row.GetValue<int>(index));
    }

    [TestMethod]
    public void TypedContextQuery_WhenAJoinedContextIsMissing_ShouldPreserveTheExistingSegment()
    {
        const string query = "select a.ContextOne() as A1, a.ContextTwo() as A2, b.ContextOne() as Missing " +
            "from #schema.first() a left join #schema.first() b on b.Position = 99 where a.Position = 1";
        using var compiled = CreateAndRunVirtualMachine<ContextInput, ContextLibrary>(query, ContextRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        var row = table.Single();
        TableMaterializationTestHelper.AssertColumns(table, ("A1", typeof(int)), ("A2", typeof(int)), ("Missing", typeof(int?)));
        Assert.AreEqual(1, row.GetValue<int>(0));
        Assert.AreEqual(2, row.GetValue<int>(1));
        Assert.IsNull(row.GetValue<int?>(2));
    }

    private void AssertMissingContext(string query, bool emptyRightSource = false)
    {
        using var compiled = CreateAndRunVirtualMachine<ContextInput, ContextLibrary>(query, ContextRows(),
            filter: (parameters, _) => emptyRightSource && parameters.Length > 0
                ? new[] { System.Array.Empty<ContextInput>() }
                : new[] { ContextRows() });
        using var table = compiled.Run(TestContext.CancellationToken);
        var row = table.Single();
        TableMaterializationTestHelper.AssertColumns(table, ("One", typeof(int?)), ("Text", typeof(string)), ("Fraction", typeof(double?)));
        Assert.IsNull(row.GetValue<int?>(0));
        Assert.IsNull(row.GetValue<string>(1));
        Assert.IsNull(row.GetValue<double?>(2));
    }

    private static ContextInput[] ContextRows() => [new(1, 1, 2, 0, "hello", 3.14), new(2, 3, 4, 5, "other", 0)];
    public sealed record ContextInput(int Position, int One, int Two, int Three, string Text, double Fraction);
    public sealed class ContextLibrary : LibraryBase
    {
        [BindableMethod] public int ContextOne([InjectSpecificSource(typeof(ContextInput))] ContextInput row) => row.One;
        [BindableMethod] public int ContextTwo([InjectSpecificSource(typeof(ContextInput))] ContextInput row) => row.Two;
        [BindableMethod] public int ContextThree([InjectSpecificSource(typeof(ContextInput))] ContextInput row) => row.Three;
        [BindableMethod] public string ContextText([InjectSpecificSource(typeof(ContextInput))] ContextInput row) => row.Text;
        [BindableMethod] public double ContextFraction([InjectSpecificSource(typeof(ContextInput))] ContextInput row) => row.Fraction;
    }
}
