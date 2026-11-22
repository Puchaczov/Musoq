using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Examples.DataSources.StructuredInputs;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
[TestCategory("AfterimageRepro")]
public sealed class AfterimageSourceAndConversionQueryTests
{
    [TestMethod]
    [DataRow("1ui", "01000000")]
    [DataRow("1::uint", "01000000")]
    [DataRow("1L", "0100000000000000")]
    [DataRow("1::long", "0100000000000000")]
    [DataRow("null::uint", null)]
    [DataRow("null", null)]
    public void ToHex_ShouldAcceptNumericLiteralsAndCasts(string value, string? expected)
    {
        var sql = $"select ToHex({value}) from values {{ (Seed: 0) }} v";

        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(ToHex_ShouldAcceptNumericLiteralsAndCasts),
            new GenericSchemaProvider(new Dictionary<string, ISchema>()),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();

        Assert.AreEqual(expected, result.Single().GetValue<string>(0));
    }

    [TestMethod]
    [DataRow("alternate")]
    [DataRow("c")]
    public void CteSources_ShouldHaveDistinctContextIdsEvenWithTheSameAlias(string secondAlias)
    {
        var sql = $$"""
            with first_source as (select c.SourceContextId as ContextId from #inputs.contextprobe() c),
                 second_source as (
                     select {{secondAlias}}.SourceContextId as ContextId from #inputs.contextprobe() {{secondAlias}}
                 )
            select f.ContextId, s.ContextId
            from first_source f cross join second_source s
            """;

        using var compiled = InstanceCreator.CompileForExecution(
            sql,
            nameof(CteSources_ShouldHaveDistinctContextIdsEvenWithTheSameAlias),
            new StructuredInputsSchemaProvider(),
            new TestsLoggerResolver()
        );
        using var result = compiled.Run();
        var row = result.Single();

        Assert.AreNotEqual(row.GetValue<string>(0), row.GetValue<string>(1));
    }
}
