using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class ScriptParameterExecutionTests
{
    [TestMethod]
    [DataRow("param", "plain")]
    [DataRow("param", "qualify")]
    [DataRow("param", "cte")]
    [DataRow("let", "plain")]
    [DataRow("let", "qualify")]
    [DataRow("let", "cte")]
    [DataRow("literal", "plain")]
    [DataRow("literal", "qualify")]
    [DataRow("literal", "cte")]
    public void WhenSourceInputIsCombinedWithWindowRanking_ShouldReturnEquivalentEvidence(
        string inputKind, string queryShape)
    {
        var declaration = inputKind switch
        {
            "param" => "param(key: string = 'KEY_1')",
            "let" => "let key: string = 'KEY_1'",
            _ => ""
        };
        var argument = inputKind == "literal" ? "'KEY_1'" : "$key";
        var ranking = $"select m.Key as Topic, n.Key as Evidence, RowNumber() over (partition by m.Key order by n.Key) as Position from #Parameterized.Items({argument}) m cross join #Parameterized.Items() n";
        var query = queryShape switch
        {
            "qualify" => $"{ranking} qualify Position <= 1 order by Position",
            "cte" => $"with ranked as ({ranking}) select Topic, Evidence, Position from ranked where Position <= 1 order by Position",
            _ => $"{ranking} order by Position"
        };
        var vm = CompileWithProvider($"{declaration} {query}", new ParameterizedEnvironmentSchemaProvider());
        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(queryShape == "plain" ? 2 : 1, table.Count);
        Assert.AreEqual("KEY_1", table[0].ReadValue(0));
        Assert.AreEqual("KEY_1", table[0].ReadValue(1));
        Assert.AreEqual(1L, table[0].ReadValue(2));
        if (queryShape == "plain")
        {
            Assert.AreEqual("KEY_1", table[1].ReadValue(0));
            Assert.AreEqual("KEY_2", table[1].ReadValue(1));
            Assert.AreEqual(2L, table[1].ReadValue(2));
        }

        if (inputKind == "param")
        {
            global::Musoq.Tests.Common.NativeParameterTestInputs.Set(vm.TypedParameters, vm.ParameterDefinitions, "key", "KEY_2");
            var rebound = vm.Run(TestContext.CancellationToken);
            Assert.AreEqual(table.Count, rebound.Count);
            Assert.AreEqual("KEY_2", rebound[0].ReadValue(0));
            Assert.AreEqual("KEY_1", rebound[0].ReadValue(1));
            Assert.AreEqual(1L, rebound[0].ReadValue(2));
        }
    }
}
