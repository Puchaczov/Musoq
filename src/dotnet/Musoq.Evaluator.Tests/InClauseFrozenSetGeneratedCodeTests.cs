using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class InClauseFrozenSetGeneratedCodeTests : BasicEntityTestBase
{
    [TestMethod]
    public void Query_WhenConstantInListExceedsPrimitiveSwitchThreshold_ShouldUseFrozenSetAndReturnMatches()
    {
        var inList = string.Join(", ", Enumerable.Range(1, 50).Select(static value => $"'{value}'"));
        var query = $"select Name from #A.Entities() where Name in ({inList})";
        var sources = CreateSingleSource(new BasicEntity("25"), new BasicEntity("100"));

        var inspection = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver,
            TestCompilationOptions);
        var table = CreateAndRunVirtualMachine(query, sources).Run();

        Assert.Contains("using System.Collections.Frozen;", inspection.GeneratedCSharpCode);
        Assert.Contains("FrozenSet", inspection.GeneratedCSharpCode);
        Assert.Contains("ToFrozenSet", inspection.GeneratedCSharpCode);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("25", table[0][0]);
    }
}