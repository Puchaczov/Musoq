using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests
{
    [TestClass]
    public class CharacterAccessTests : BasicEntityTestBase
    {
        [TestMethod]
        public void DirectCharacterAccessTest()
        {
            var query = @"select Name from #A.Entities() where Name[0] = 'd'";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    [
                        new BasicEntity("david.jones@proseware.com"),
                        new BasicEntity("alice.smith@example.com")
                    ]
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("david.jones@proseware.com", table[0].Values[0]);
        }

        [TestMethod]
        public void AliasedCharacterAccessTest()
        {
            var query = @"select Name from #A.Entities() f where f.Name[0] = 'd'";
            var sources = new Dictionary<string, IEnumerable<BasicEntity>>
            {
                {
                    "#A",
                    [
                        new BasicEntity("david.jones@proseware.com"),
                        new BasicEntity("alice.smith@example.com")
                    ]
                }
            };

            var vm = CreateAndRunVirtualMachine(query, sources);
            var table = vm.Run();

            Assert.AreEqual(1, table.Count);
            Assert.AreEqual("david.jones@proseware.com", table[0].Values[0]);
        }
    }
}