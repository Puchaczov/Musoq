using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class JsonTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void WhenSerializingPrimitiveType_ShouldPass()
    {
        Assert.AreEqual("1", Library.ToJson(1));
    }
    
    [TestMethod]
    public void WhenSerializingObject_ShouldPass()
    {
        Assert.AreEqual("{\"Name\":\"John\",\"Age\":30}", Library.ToJson(new { Name = "John", Age = 30 }));
    }
    
    [TestMethod]
    public void WhenSerializingArray_ShouldPass()
    {
        Assert.AreEqual("[1,2,3]", Library.ToJson(new[] { 1, 2, 3 }));
    }
    
    [TestMethod]
    public void WhenNull_ShouldPass()
    {
        Assert.AreEqual(null, Library.ToJson<int?>(null));
    }
}