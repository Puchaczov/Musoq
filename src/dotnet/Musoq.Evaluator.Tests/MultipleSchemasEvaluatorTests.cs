using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;
using Musoq.Parser.Diagnostics;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class MultipleSchemasEvaluatorTests : MultiSchemaTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenCompilerMustDecideWhichOneOfTheMethodsUse_ShouldChoseTheFirstOne()
    {
        const string query =
            "select first.MethodA() from #schema.first() first inner join #schema.second() second on 1 = 1";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(0, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenCompilerMustDecideWhichOneOfTheMethodsUse_ShouldChoseTheSecondOne()
    {
        const string query =
            "select second.MethodA() from #schema.first() first inner join #schema.second() second on 1 = 1";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(1, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenCompilerMustDecideWhichOneOfTheMethodsUse_ShouldChoseFirstOne()
    {
        const string query =
            "select first.MethodA() from #schema.second() second inner join #schema.first() first on 1 = 1";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(0, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenCompilerMustDecideWhichOneOfTheMethodsUse_TheMethodHasAdditionalArgument_ShouldChoseTheSecondOne()
    {
        const string query =
            "select second.MethodB('abc') from #schema.second() second inner join #schema.first() first on 1 = 1";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(1, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenCompilerMustDecideWhichOneOfTheMethodsUse_TheMethodHasAdditionalArgument_ShouldChoseFirstOne()
    {
        const string query =
            "select first.MethodB('abc') from #schema.first() first inner join #schema.second() second on 1 = 1";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(0, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenMultipleInjectsWithinMethod_ShouldNotThrow()
    {
        const string query = "select AggregateMethodA() from #schema.first()";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], []);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void WhenUnqualifiedAggregateHasSingleResolvableOwner_ShouldInferAlias()
    {
        const string query = @"select first.FirstItem, AggregateMethodA() as AggregateValue
from #schema.first() first
inner join #schema.second() second on 1 = 1
group by first.FirstItem";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity { FirstItem = "group-a" }
        ], [
            new SecondEntity { FirstItem = "group-a" }
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("group-a", table[0].Values[0]);
        Assert.AreEqual("0", table[0].Values[1]);
    }

    [TestMethod]
    public void WhenInjectingEntityUsesCommonInterfaceWithMethod_ShouldMatchMethodAndCall()
    {
        const string query = "select MethodC() from #schema.first()";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], []);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Count);

        Assert.AreEqual(5, table[0].Values[0]);
    }

    [TestMethod]
    public void WhenMissingAliasButBothObjectsAreTheSame_ShouldNotThrow()
    {
        var query = @"select FirstItem from #schema.first() first inner join #schema.first() second on 1 = 1";

        CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], []);
    }

    [TestMethod]
    public void WhenMissingAlias_ShouldThrow()
    {
        var query = @"select FirstItem from #schema.first() first inner join #schema.second() second on 1 = 1";

        var ex = Assert.Throws<MusoqQueryException>(() => CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]));

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3002_AmbiguousColumn, DiagnosticPhase.Bind, "FirstItem");
        AssertHasGuidance(ex);
    }
}

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
