using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationCoreLanguageTests
{
    [TestMethod]
    public void Spec_ScriptParameterPluralAlias_ShouldCompileAndExecute()
    {
        const string query =
            "params(minId: int = 2) select Name from #A.Entities() where Id >= $minId order by Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("Alice") { Id = 3 },
                    new BasicEntity("Bob") { Id = 1 },
                    new BasicEntity("Charlie") { Id = 2 }
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("Alice", table[0][0]);
        Assert.AreEqual("Charlie", table[1][0]);
    }
}
