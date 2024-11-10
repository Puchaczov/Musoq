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

    [TestMethod]
    public void AddTimeSpansTest()
    {
        var timeSpan = Library.AddTimeSpans(TimeSpan.Zero, TimeSpan.FromHours(1));
        
        Assert.AreEqual(TimeSpan.FromHours(1), timeSpan);
    }
    
    [TestMethod]
    public void WhenFirstTimeSpanIsNull_Add_ShouldReturnRightOne()
    {
        var timeSpan = Library.AddTimeSpans(null, TimeSpan.FromMinutes(30));
        
        Assert.AreEqual(TimeSpan.FromMinutes(30), timeSpan);
    }
    
    [TestMethod]
    public void WhenSecondTimeSpanIsNull_Add_ShouldReturnLeftOne()
    {
        var timeSpan = Library.AddTimeSpans(TimeSpan.FromMinutes(30), null);
        
        Assert.AreEqual(TimeSpan.FromMinutes(30), timeSpan);
    }
    
    [TestMethod]
    public void SubtractTimeSpansTest()
    {
        var timeSpan = Library.SubtractTimeSpans(TimeSpan.FromHours(1), TimeSpan.FromMinutes(30));
        
        Assert.AreEqual(TimeSpan.FromMinutes(30), timeSpan);
    }
    
    [TestMethod]
    public void WhenFirstTimeSpanIsNull_Subtract_ShouldReturnRightOne()
    {
        var timeSpan = Library.SubtractTimeSpans(null, TimeSpan.FromMinutes(30));
        
        Assert.AreEqual(TimeSpan.FromMinutes(30), timeSpan);
    }
    
    [TestMethod]
    public void WhenSecondTimeSpanIsNull_Subtract_ShouldReturnLeftOne()
    {
        var timeSpan = Library.SubtractTimeSpans(TimeSpan.FromMinutes(30), null);
        
        Assert.AreEqual(TimeSpan.FromMinutes(30), timeSpan);
    }
}