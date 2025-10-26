﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class StDevTests : LibraryBaseBaseTests
{

    [TestMethod]
    public void StdDevTest()
    {
        Library.SetStDev(Group, "test", 60000m, 0);
        Library.SetStDev(Group, "test", 80000m, 0);

        var result = Library.StDev(Group, "test");
        var difference = Math.Abs(result - 14142.13m);
        Assert.IsLessThan(difference, 0.01m);
    }

    [TestMethod]
    public void StDevTest_2()
    {
        Library.SetStDev(Group, "test", 5m, 0);
        Library.SetStDev(Group, "test", 6m, 0);
        Library.SetStDev(Group, "test", 8m, 0);
        Library.SetStDev(Group, "test", 9m, 0);

        var result = Library.StDev(Group, "test");
        var difference = Math.Abs(result - 1.8257m);
        Assert.IsLessThan(difference, 0.001m);
    }

    [TestMethod]
    public void StDevTest_3()
    {
        Library.SetStDev(Group, "test", 4m, 0);
        Library.SetStDev(Group, "test", 9m, 0);
        Library.SetStDev(Group, "test", 11m, 0);
        Library.SetStDev(Group, "test", 12m, 0);
        Library.SetStDev(Group, "test", 17m, 0);
        Library.SetStDev(Group, "test", 5m, 0);
        Library.SetStDev(Group, "test", 8m, 0);
        Library.SetStDev(Group, "test", 12m, 0);
        Library.SetStDev(Group, "test", 14m, 0);

        var result = Library.StDev(Group, "test");
        var difference = Math.Abs(result - 3.94m);
        Assert.IsLessThan(difference, 0.001m);
    }
}