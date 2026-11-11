using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class AggregateValuesTests
{
    [TestMethod]
    public void AggregateValuesStringKernel_PreservesNullsAsEmptyEntries()
    {
        var state = new AggregateValuesStringKernel.State();

        AggregateValuesStringKernel.Set(ref state, "hello");
        AggregateValuesStringKernel.Set(ref state, null);
        AggregateValuesStringKernel.Set(ref state, "world");

        Assert.AreEqual("hello,,world", AggregateValuesStringKernel.Get(in state));
    }

    [TestMethod]
    public void AggregateValuesStringKernel_EmptyStateReturnsEmptyString()
    {
        var state = new AggregateValuesStringKernel.State();

        Assert.AreEqual(string.Empty, AggregateValuesStringKernel.Get(in state));
    }

    [TestMethod]
    public void AggregateValuesStringKernel_MergePreservesInputOrder()
    {
        var target = new AggregateValuesStringKernel.State();
        var source = new AggregateValuesStringKernel.State();

        AggregateValuesStringKernel.Set(ref target, "a");
        AggregateValuesStringKernel.Set(ref source, "b");
        AggregateValuesStringKernel.Set(ref source, "c");
        AggregateValuesStringKernel.Merge(ref target, in source);

        Assert.AreEqual("a,b,c", AggregateValuesStringKernel.Get(in target));
    }

    [TestMethod]
    public void AggregateValuesStringDelimitedKernel_SkipsNullsAndUsesDelimiter()
    {
        var state = new AggregateValuesStringDelimitedKernel.State();

        AggregateValuesStringDelimitedKernel.Set(ref state, "hello", " | ");
        AggregateValuesStringDelimitedKernel.Set(ref state, null, " | ");
        AggregateValuesStringDelimitedKernel.Set(ref state, "world", " | ");

        Assert.AreEqual("hello | world", AggregateValuesStringDelimitedKernel.Get(in state));
    }

    [TestMethod]
    public void AggregateValuesStringDelimitedKernel_MergeKeepsLatestDelimiterAndOrder()
    {
        var target = new AggregateValuesStringDelimitedKernel.State();
        var source = new AggregateValuesStringDelimitedKernel.State();

        AggregateValuesStringDelimitedKernel.Set(ref target, "a", ", ");
        AggregateValuesStringDelimitedKernel.Set(ref source, "b", " | ");
        AggregateValuesStringDelimitedKernel.Set(ref source, "c", " | ");
        AggregateValuesStringDelimitedKernel.Merge(ref target, in source);

        Assert.AreEqual("a | b | c", AggregateValuesStringDelimitedKernel.Get(in target));
    }

    [TestMethod]
    public void AggregateValuesCharKernel_PreservesNullsAsEmptyEntries()
    {
        var state = new AggregateValuesCharKernel.State();

        AggregateValuesCharKernel.Set(ref state, 'A');
        AggregateValuesCharKernel.Set(ref state, null);
        AggregateValuesCharKernel.Set(ref state, 'C');

        Assert.AreEqual("A,,C", AggregateValuesCharKernel.Get(in state));
    }

    [TestMethod]
    public void AggregateValuesCharDelimitedKernel_SkipsNullsAndUsesDelimiter()
    {
        var state = new AggregateValuesCharDelimitedKernel.State();

        AggregateValuesCharDelimitedKernel.Set(ref state, 'A', " / ");
        AggregateValuesCharDelimitedKernel.Set(ref state, null, " / ");
        AggregateValuesCharDelimitedKernel.Set(ref state, 'C', " / ");

        Assert.AreEqual("A / C", AggregateValuesCharDelimitedKernel.Get(in state));
    }

    [TestMethod]
    public void AggregateValuesCharDelimitedKernel_MergeKeepsLatestDelimiterAndOrder()
    {
        var target = new AggregateValuesCharDelimitedKernel.State();
        var source = new AggregateValuesCharDelimitedKernel.State();

        AggregateValuesCharDelimitedKernel.Set(ref target, 'A', ",");
        AggregateValuesCharDelimitedKernel.Set(ref source, 'B', "|");
        AggregateValuesCharDelimitedKernel.Set(ref source, 'C', "|");
        AggregateValuesCharDelimitedKernel.Merge(ref target, in source);

        Assert.AreEqual("A|B|C", AggregateValuesCharDelimitedKernel.Get(in target));
    }
}
