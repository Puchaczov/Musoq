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

    private GenericSchemaProvider CreateAmbiguousAggregateSchemaProvider()
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

    private static GenericSchema<TLibrary> CreateSchema<TLibrary>(BasicEntity[] source)
        where TLibrary : LibraryBase, new()
    {
        return new GenericSchema<TLibrary>(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            {
                "entities",
                (new BasicEntityTable(), new MultiRowSource<BasicEntity>(source))
            }
        });
    }

    public sealed class AggregateLibraryA : LibraryBase
    {
        [AggregateFunction(typeof(AmbiguousAggregateKernelA), Name = nameof(AmbiguousAgg), Inline = true)]
        public int AmbiguousAgg(decimal? value)
        {
            return AggregateFunction.NotInvoked<int>();
        }

        public static class AmbiguousAggregateKernelA
        {
            public struct State
            {
                public int Value;
            }

            public static void Set(ref State state, decimal? value)
            {
                state.Value = 10;
            }

            public static int Get(in State state)
            {
                return state.Value;
            }

            public static void Merge(ref State target, in State source)
            {
                if (source.Value != 0)
                    target.Value = source.Value;
            }
        }
    }

    public sealed class AggregateLibraryB : LibraryBase
    {
        [AggregateFunction(typeof(AmbiguousAggregateKernelB), Name = nameof(AmbiguousAgg), Inline = true)]
        public int AmbiguousAgg(decimal? value)
        {
            return AggregateFunction.NotInvoked<int>();
        }

        public static class AmbiguousAggregateKernelB
        {
            public struct State
            {
                public int Value;
            }

            public static void Set(ref State state, decimal? value)
            {
                state.Value = 20;
            }

            public static int Get(in State state)
            {
                return state.Value;
            }

            public static void Merge(ref State target, in State source)
            {
                if (source.Value != 0)
                    target.Value = source.Value;
            }
        }
    }
}
