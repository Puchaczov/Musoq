using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class LikeLongPatternRegressionTests : BasicEntityTestBase
{
    [TestMethod]
    public void Like_LongUnicodeExactPattern_ShouldFallBackFromOversizedNonBacktrackingAutomaton()
    {
        var value = new string('\u0142', 4096);

        Assert.IsTrue(new Operators().Like(value, value));
        Assert.IsFalse(new Operators().Like(value + "x", value));
    }

    [TestMethod]
    public void Like_LongUnicodeWildcardPattern_ShouldFallBackFromOversizedNonBacktrackingAutomaton()
    {
        var prefix = new string('\u0142', 4096);

        Assert.IsTrue(new Operators().Like(prefix + "-tail", prefix + "%"));
        Assert.IsFalse(new Operators().Like("other", prefix + "%"));
    }

    [TestMethod]
    public void CompiledDynamicLike_LongUnicodePattern_ShouldReturnMatchingRow()
    {
        var value = new string('\u0142', 4096);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Name = value, City = value },
                new BasicEntity { Name = value + "-miss", City = value }
            ]
        };
        using var query = CreateAndRunVirtualMachine(
            "select e.Name from #A.Entities() e where e.Name like e.City order by e.Name",
            sources);

        var result = query.Run(TestContext.CancellationToken);

        Assert.HasCount(1, result);
        Assert.AreEqual(value, result[0][0]);
    }
}
