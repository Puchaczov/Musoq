using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class AsOfJoinTests
{
    [TestMethod]
    public void CompileForInspection_WhenValueTypeAsOfComparisonKey_ShouldNotBoxComparisonKey()
    {
        const string query =
            "select a.Name from #A.entities() a asof join #B.entities() b on a.Population >= b.Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90 }] }
        };

        var result = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver);

        Assert.Contains(
            "EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, decimal>",
            result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForInspection_WhenValueTypeAsOfTieBreakKey_ShouldUseTypedTieBreakKey()
    {
        const string query =
            "select a.Name from #A.entities() a asof join #B.entities() b on a.Population >= b.Population tie break by b.Money desc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "A1", Population = 100 }] },
            { "#B", [new BasicEntity { Name = "B1", Population = 90, Money = 1m }] }
        };

        var result = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver);

        Assert.Contains(
            "EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, decimal, decimal>",
            result.GeneratedCSharpCode);
        Assert.IsFalse(
            result.GeneratedCSharpCode.Contains("EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, decimal, object>", StringComparison.Ordinal),
            result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("EvaluationHelper.FindAsOfMatch<", StringComparison.Ordinal));
    }
}
