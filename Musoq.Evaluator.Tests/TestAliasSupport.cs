using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class TestAliasSupport : BasicEntityTestBase
{
    [TestMethod]
    public void TestBasicAliasedTableAccess()
    {
        // Test if basic aliased table access works at all
        var query = @"select Name from #A.Entities() f where f.Name = 'david.jones@proseware.com'";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A",
                new[]
                {
                    new BasicEntity("12@hostname.com"),
                    new BasicEntity("ma@hostname.comcom"),  
                    new BasicEntity("david.jones@proseware.com"),
                    new BasicEntity("ma@hostname.com")
                }
            }
        };

        Console.WriteLine("Testing basic aliased table access:");
        Console.WriteLine($"Query: {query}");
        
        try
        {
            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();
            
            Console.WriteLine($"Success! Results: {table.Count} rows");
            for (int i = 0; i < table.Count; i++)
            {
                Console.WriteLine($"Row {i}: {table[i].Values[0]}");
            }
            
            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("david.jones@proseware.com", table[0].Values[0]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed with exception: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}