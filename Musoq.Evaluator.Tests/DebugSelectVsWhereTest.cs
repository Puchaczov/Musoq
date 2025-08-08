using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class DebugSelectVsWhereTest : BasicEntityTestBase
    {
        [TestMethod]
        public void DebugSelectVsWhereCharacterAccess()
        {
            var logFile = "/tmp/debug_output.txt";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    [
                        new BasicEntity("david.jones@proseware.com"),
                        new BasicEntity("mary.smith@example.com")
                    ]
                }
            };

            File.WriteAllText(logFile, "=== Testing f.Name[0] in SELECT clause ===\n");
            try
            {
                var selectQuery = @"select f.Name[0] from #A.Entities() f";
                File.AppendAllText(logFile, $"Query: {selectQuery}\n");
                
                var vm1 = CreateAndRunVirtualMachine(selectQuery, sources);
                var table1 = vm1.Run();
                File.AppendAllText(logFile, $"Result: {table1.Count} rows\n");
                if (table1.Count > 0)
                {
                    File.AppendAllText(logFile, $"First result: {table1[0].Values[0]} (Type: {table1[0].Values[0]?.GetType().Name})\n");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, $"SELECT exception: {ex.Message}\n");
            }

            File.AppendAllText(logFile, "\n=== Testing f.Name[0] in WHERE clause ===\n");
            try
            {
                var whereQuery = @"select Name from #A.Entities() f where f.Name[0] = 'd'";
                File.AppendAllText(logFile, $"Query: {whereQuery}\n");
                
                var vm2 = CreateAndRunVirtualMachine(whereQuery, sources);
                var table2 = vm2.Run();
                File.AppendAllText(logFile, $"Result: {table2.Count} rows\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, $"WHERE exception: {ex.Message}\n");
            }

            File.AppendAllText(logFile, "\n=== Testing direct Name[0] in WHERE clause (should work) ===\n");
            try
            {
                var directQuery = @"select Name from #A.Entities() where Name[0] = 'd'";
                File.AppendAllText(logFile, $"Query: {directQuery}\n");
                
                var vm3 = CreateAndRunVirtualMachine(directQuery, sources);
                var table3 = vm3.Run();
                File.AppendAllText(logFile, $"Result: {table3.Count} rows\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, $"Direct WHERE exception: {ex.Message}\n");
            }
        }
    }
}