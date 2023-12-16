using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class TimeSpanTests : LibraryBaseBaseTests
{
    [TestMethod]
    public void SumTimeSpanTest()
    {
        Library.SetSumTimeSpan(Group, "test", new TimeSpan(1, 0, 0));
        Library.SetSumTimeSpan(Group, "test", new TimeSpan(2, 0, 0));
        Library.SetSumTimeSpan(Group, "test", new TimeSpan(3, 0, 0));
        Library.SetSumTimeSpan(Group, "test", null);

        Assert.AreEqual(new TimeSpan(6, 0, 0), Library.SumTimeSpan(Group, "test"));
    }
    
    [TestMethod]
    public void ComputeMinTimeSpanTest()
    {
        Library.SetMinTimeSpan(Group, "test", new TimeSpan(1, 0, 0));
        Library.SetMinTimeSpan(Group, "test", new TimeSpan(2, 0, 0));
        Library.SetMinTimeSpan(Group, "test", new TimeSpan(3, 0, 0));
        Library.SetMinTimeSpan(Group, "test", null);
        
        Assert.AreEqual(new TimeSpan(1, 0, 0), Library.MinTimeSpan(Group, "test"));
    }
    
    [TestMethod]
    public void ComputeMaxTimeSpanTest()
    {
        Library.SetMaxTimeSpan(Group, "test", new TimeSpan(1, 0, 0));
        Library.SetMaxTimeSpan(Group, "test", new TimeSpan(2, 0, 0));
        Library.SetMaxTimeSpan(Group, "test", new TimeSpan(3, 0, 0));
        Library.SetMaxTimeSpan(Group, "test", null);
        
        Assert.AreEqual(new TimeSpan(3, 0, 0), Library.MaxTimeSpan(Group, "test"));
    }
}