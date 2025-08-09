using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class QuickDebugTests : BasicEntityTestBase
{
    [TestMethod]
    public void PropertyStringAccess_OutOfBounds_Debug()
    {
        // Test Self.Name[100] (property access) vs Name[100] (column access)
        var query = @"select Self.Name[100] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("david")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        
        // Debug: Check what we actually got
        var actualResult = table[0].Values[0];
        Console.WriteLine($"Property access result: {actualResult} (type: {actualResult?.GetType()})");
        
        // Test that it returns the safe default
        Assert.AreEqual('\0', actualResult);
    }

    [TestMethod]
    public void ColumnStringAccess_OutOfBounds_Debug()
    {
        // Test Name[100] (column access)
        var query = @"select Name[100] from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {"#A", [new BasicEntity("david")]}
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run();

        Assert.AreEqual(1, table.Count);
        
        // Debug: Check what we actually got
        var actualResult = table[0].Values[0];
        Console.WriteLine($"Column access result: {actualResult} (type: {actualResult?.GetType()})");
    }
}