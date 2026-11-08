using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class GroupByTests
{
    [TestMethod]
    public void CompileForInspection_WhenValueTypeMultiKeyGroupBy_ShouldNotEmitCompositeKeyBoxing()
    {
        const string query =
            "select Id, Population, Count(Id) from #A.Entities() group by Id, Population";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("City", "Country", 100) { Id = 1 },
                    new BasicEntity("City", "Country", 100) { Id = 1 }
                ]
            }
        };

        var result = InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new BasicSchemaProvider<BasicEntity>(sources),
            LoggerResolver);

        Assert.IsFalse(
            result.GeneratedCSharpCode.Contains("CompositeKey", StringComparison.Ordinal),
            "Value-type multi-column group-by key must not box parts through CompositeKey.");
        Assert.Contains("Dictionary<(int, decimal), ", result.GeneratedCSharpCode);
    }
}
