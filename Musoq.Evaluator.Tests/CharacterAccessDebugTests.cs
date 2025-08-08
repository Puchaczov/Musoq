using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class CharacterAccessDebugTests : BasicEntityTestBase
    {
        [TestMethod]
        public void Debug_SelectAliasedCharacterAccess()
        {
            var query = @"select f.Name[0] from #A.Entities() f";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    [
                        new BasicEntity("david.jones@proseware.com")
                    ]
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            // This should return 'd' but likely returns the full string
            System.Console.WriteLine($"DEBUG: Select result: '{table[0].Values[0]}'");
            System.Console.WriteLine($"DEBUG: Expected: 'd', Actual length: {table[0].Values[0].ToString().Length}");
        }
        
        [TestMethod]
        public void Debug_DirectColumnAccess()
        {
            var query = @"select Name from #A.Entities()";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    [
                        new BasicEntity("david.jones@proseware.com")
                    ]
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            System.Console.WriteLine($"DEBUG: Direct Name result: '{table[0].Values[0]}'");
        }
    }
}