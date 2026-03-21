using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AggregateOwnerAmbiguityTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenUnqualifiedAggregateMatchesDifferentSchemaLibraries_ShouldReportCandidateAliases()
    {
        const string query = @"select a.City, AmbiguousAgg(b.Population) as AggValue
from #A.entities() a
inner join #B.entities() b on a.City = b.City
group by a.City";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(
            query,
            schemaProvider: CreateAmbiguousAggregateSchemaProvider()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3034_AmbiguousAggregateOwner, DiagnosticPhase.Bind,
            "AmbiguousAgg(b.Population)");
        AssertHasGuidance(ex);
        AssertMessageContains(ex, "a");
        AssertMessageContains(ex, "b");
    }

    [TestMethod]
    public void WhenAmbiguousAggregateIsQualified_ShouldUseRequestedOwner()
    {
        const string query = @"select a.City, a.AmbiguousAgg(b.Population) as AggValue
from #A.entities() a
inner join #B.entities() b on a.City = b.City
group by a.City";

        var vm = CreateAndRunVirtualMachine(
            query,
            schemaProvider: CreateAmbiguousAggregateSchemaProvider());

        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Warsaw", table[0].Values[0]);
        Assert.AreEqual(10, table[0].Values[1]);
    }

    private ISchemaProvider CreateAmbiguousAggregateSchemaProvider()
    {
        var sourceA = new[] { new BasicEntity("Warsaw", "Poland", 100) };
        var sourceB = new[] { new BasicEntity("Warsaw", "Poland", 200) };

        var schemas = new Dictionary<string, ISchema>
        {
            { "#A", CreateSchema<AggregateLibraryA>(sourceA) },
            { "#B", CreateSchema<AggregateLibraryB>(sourceB) }
        };

        return new GenericSchemaProvider(schemas);
    }

    private static ISchema CreateSchema<TLibrary>(BasicEntity[] source)
        where TLibrary : LibraryBase, new()
    {
        var nameToIndexMap = new Dictionary<string, int>(BasicEntity.TestNameToIndexMap);
        var indexToObjectAccessMap = new Dictionary<int, Func<BasicEntity, object>>(BasicEntity.TestIndexToObjectAccessMap);

        return new GenericSchema<TLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, RowSource RowSource)>
        {
            {
                "entities",
                (new BasicEntityTable(),
                    new MultiRowSource<BasicEntity>(source, nameToIndexMap, indexToObjectAccessMap))
            }
        });
    }

    public sealed class AggregateLibraryA : LibraryBase
    {
        [AggregationGetMethod]
        public int AmbiguousAgg([InjectGroup] Group group, string name)
        {
            return group.GetValue<int>(name);
        }

        [AggregationSetMethod]
        public void SetAmbiguousAgg([InjectGroup] Group group, string name, decimal? value)
        {
            group.SetValue(name, 10);
        }
    }

    public sealed class AggregateLibraryB : LibraryBase
    {
        [AggregationGetMethod]
        public int AmbiguousAgg([InjectGroup] Group group, string name)
        {
            return group.GetValue<int>(name);
        }

        [AggregationSetMethod]
        public void SetAmbiguousAgg([InjectGroup] Group group, string name, decimal? value)
        {
            group.SetValue(name, 20);
        }
    }
}
