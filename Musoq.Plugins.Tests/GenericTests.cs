using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class GenericTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void WhenMergeArraysWithSingleArgument_ShouldReturnArray()
    {
        var result = Library.MergeArrays(Encoding.UTF8.GetBytes("test1"));

        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Length);
        Assert.AreEqual("test1", Encoding.UTF8.GetString(result));
    }
    
    [TestMethod]
    public void WhenMergeArraysWithTwoArguments_ShouldReturnArray()
    {
        var result = Library.MergeArrays(Encoding.UTF8.GetBytes("test1"), Encoding.UTF8.GetBytes("test2"));

        Assert.IsNotNull(result);
        Assert.AreEqual(10, result.Length);
        Assert.AreEqual("test1test2", Encoding.UTF8.GetString(result));
    }
    
    [TestMethod]
    public void WhenMergeArraysWithThreeArguments_ShouldReturnArray()
    {
        var result = Library.MergeArrays(Encoding.UTF8.GetBytes("test1"), Encoding.UTF8.GetBytes("test2"), Encoding.UTF8.GetBytes("test3"));

        Assert.IsNotNull(result);
        Assert.AreEqual(15, result.Length);
        Assert.AreEqual("test1test2test3", Encoding.UTF8.GetString(result));
    }
}