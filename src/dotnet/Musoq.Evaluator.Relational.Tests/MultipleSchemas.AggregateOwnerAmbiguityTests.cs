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
using static Musoq.Evaluator.Tests.AggregateOwnerAmbiguityTestsFixtures;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class AggregateOwnerAmbiguityTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenUnqualifiedAggregateMatchesDifferentSchemaLibraries_ShouldReportCandidateAliases()
    {
        const string query =
            @"select a.City, AmbiguousAgg(b.Population) as AggValue
from #A.entities() a
inner join #B.entities() b on a.City = b.City
group by a.City";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, schemaProvider: CreateAmbiguousAggregateSchemaProvider())
        );

        AssertErrorEnvelope(
            ex,
            DiagnosticCode.MQ3034_AmbiguousAggregateOwner,
            DiagnosticPhase.Bind,
            "AmbiguousAgg(b.Population)"
        );
        AssertHasGuidance(ex);
        AssertMessageContains(ex, "a");
        AssertMessageContains(ex, "b");
    }

    [TestMethod]
    public void WhenAmbiguousAggregateIsQualified_ShouldUseRequestedOwner()
    {
        const string query =
            @"select a.City, a.AmbiguousAgg(b.Population) as AggValue
from #A.entities() a
inner join #B.entities() b on a.City = b.City
group by a.City";

        var vm = CreateAndRunVirtualMachine(query, schemaProvider: CreateAmbiguousAggregateSchemaProvider());

        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Warsaw", table[0].ReadValues()[0]);
        Assert.AreEqual(10, table[0].ReadValues()[1]);
    }

    private GenericSchemaProvider CreateAmbiguousAggregateSchemaProvider()
    {
        var sourceA = new[] { new BasicEntity("Warsaw", "Poland", 100) };
        var sourceB = new[] { new BasicEntity("Warsaw", "Poland", 200) };

        var schemas = new Dictionary<string, ISchema>
        {
            { "#A", CreateSchema<AggregateLibraryA>(sourceA) },
            { "#B", CreateSchema<AggregateLibraryB>(sourceB) },
        };

        return new GenericSchemaProvider(schemas);
    }
}
