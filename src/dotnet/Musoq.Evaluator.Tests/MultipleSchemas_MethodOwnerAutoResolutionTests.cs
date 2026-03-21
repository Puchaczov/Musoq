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
public class MethodOwnerAutoResolutionTests : BasicEntityTestBase
{
    [TestMethod]
    public void WhenUnqualifiedMethodExistsInCommonAncestor_ShouldAutoResolve()
    {
        const string query = @"select a.City, ToDecimal(a.Population)
from #A.entities() a
inner join #B.entities() b on a.City = b.City";

        var vm = CreateAndRunVirtualMachine(
            query,
            schemaProvider: CreateTwoSchemaProvider<SharedOnlyLibraryA, SharedOnlyLibraryB>());

        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Warsaw", table[0].Values[0]);
        Assert.AreEqual(100m, table[0].Values[1]);
    }

    [TestMethod]
    public void WhenUnqualifiedMethodIsUniqueToOneSchema_ShouldAutoResolve()
    {
        const string query = @"select a.City, UniqueToA(a.Population)
from #A.entities() a
inner join #B.entities() b on a.City = b.City";

        var vm = CreateAndRunVirtualMachine(
            query,
            schemaProvider: CreateTwoSchemaProvider<UniqueMethodLibraryA, SharedOnlyLibraryB>());

        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Warsaw", table[0].Values[0]);
        Assert.AreEqual(200m, table[0].Values[1]);
    }

    [TestMethod]
    public void WhenUnqualifiedMethodHasDifferentImplementations_ShouldReportAmbiguity()
    {
        const string query = @"select a.City, AmbiguousMethod(a.Population)
from #A.entities() a
inner join #B.entities() b on a.City = b.City";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(
            query,
            schemaProvider: CreateTwoSchemaProvider<AmbiguousMethodLibraryA, AmbiguousMethodLibraryB>()));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3035_AmbiguousMethodOwner, DiagnosticPhase.Bind,
            "AmbiguousMethod(a.Population)");
        AssertHasGuidance(ex);
        AssertMessageContains(ex, "a");
        AssertMessageContains(ex, "b");
    }

    [TestMethod]
    public void WhenAmbiguousMethodIsQualified_ShouldUseRequestedOwner()
    {
        const string query = @"select a.City, a.AmbiguousMethod(a.Population)
from #A.entities() a
inner join #B.entities() b on a.City = b.City";

        var vm = CreateAndRunVirtualMachine(
            query,
            schemaProvider: CreateTwoSchemaProvider<AmbiguousMethodLibraryA, AmbiguousMethodLibraryB>());

        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Warsaw", table[0].Values[0]);
        Assert.AreEqual(10m, table[0].Values[1]);
    }

    [TestMethod]
    public void WhenAggregateAndNonAggregateMethodsBothUnqualified_ShouldAutoResolve()
    {
        const string query = @"select a.City, Sum(ToDecimal(b.Population)) as TotalPop
from #A.entities() a
inner join #B.entities() b on a.City = b.City
group by a.City";

        var vm = CreateAndRunVirtualMachine(
            query,
            schemaProvider: CreateTwoSchemaProvider<SharedOnlyLibraryA, SharedOnlyLibraryA>());

        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Warsaw", table[0].Values[0]);
        Assert.AreEqual(200m, table[0].Values[1]);
    }

    [TestMethod]
    public void WhenMultipleUnqualifiedSharedMethods_ShouldAutoResolveAll()
    {
        const string query = @"select a.City, ToString(ToDecimal(a.Population))
from #A.entities() a
inner join #B.entities() b on a.City = b.City";

        var vm = CreateAndRunVirtualMachine(
            query,
            schemaProvider: CreateTwoSchemaProvider<SharedOnlyLibraryA, SharedOnlyLibraryB>());

        var table = vm.Run(TokenSource.Token);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Warsaw", table[0].Values[0]);
        Assert.AreEqual("100", table[0].Values[1]);
    }

    private ISchemaProvider CreateTwoSchemaProvider<TLibA, TLibB>()
        where TLibA : LibraryBase, new()
        where TLibB : LibraryBase, new()
    {
        var sourceA = new[] { new BasicEntity("Warsaw", "Poland", 100) };
        var sourceB = new[] { new BasicEntity("Warsaw", "Poland", 200) };

        var schemas = new Dictionary<string, ISchema>
        {
            { "#A", CreateSchema<TLibA>(sourceA) },
            { "#B", CreateSchema<TLibB>(sourceB) }
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

    public sealed class SharedOnlyLibraryA : LibraryBase;

    public sealed class SharedOnlyLibraryB : LibraryBase;

    public sealed class UniqueMethodLibraryA : LibraryBase
    {
        [BindableMethod]
        public decimal UniqueToA(decimal value)
        {
            return value * 2;
        }
    }

    public sealed class AmbiguousMethodLibraryA : LibraryBase
    {
        [BindableMethod]
        public decimal AmbiguousMethod(decimal value)
        {
            return value / 10;
        }
    }

    public sealed class AmbiguousMethodLibraryB : LibraryBase
    {
        [BindableMethod]
        public decimal AmbiguousMethod(decimal value)
        {
            return value / 20;
        }
    }
}
