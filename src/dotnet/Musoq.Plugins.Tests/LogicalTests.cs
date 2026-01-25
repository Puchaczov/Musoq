using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class LogicalTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void ChooseTest()
    {
        Assert.AreEqual("a", Library.Choose(0, "a", "b", "c", "d"));
        Assert.AreEqual("b", Library.Choose(1, "a", "b", "c", "d"));
        Assert.AreEqual("c", Library.Choose(2, "a", "b", "c", "d"));
        Assert.AreEqual("d", Library.Choose(3, "a", "b", "c", "d"));
        Assert.IsNull(Library.Choose(4, "a", "b", "c", "d"));
        Assert.IsNull(Library.Choose(3, "a", "b", "c", null));
    }

    [TestMethod]
    public void IfTest()
    {
        Assert.AreEqual("abc", Library.If(1 > 0, "abc", "cba"));
        Assert.AreEqual("cba", Library.If(1 < 0, "abc", "cba"));
    }

    [TestMethod]
    public void CoalesceTest()
    {
        Assert.AreEqual("abc", Library.Coalesce("abc"));
        Assert.AreEqual("abc", Library.Coalesce(null, "abc"));
        Assert.AreEqual("abc", Library.Coalesce(null, null, "abc"));
        Assert.AreEqual("abc", Library.Coalesce(null, null, null, "abc"));
        Assert.IsNull(Library.Coalesce(null, null, null, null));
    }

    [TestMethod]
    public void MatchTest()
    {
        Assert.IsTrue(Library.Match("\\d+", "9899"));
        Assert.IsFalse(Library.Match("\\d+", string.Empty));
    }
}
