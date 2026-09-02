using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for reproducing issue where cross apply alias is defined but not used anywhere.
///     This can cause a "key not found in dictionary" exception.
/// </summary>
[TestClass]
public partial class CrossApplyUnusedAliasTests : GenericEntityTestBase
{

    /// <summary>
    ///     Test case where cross apply alias 't' is defined but only columns from 'a' are selected.
    ///     The alias 't' is completely unused in the query.
    ///     Expected: Should either work gracefully or throw a clear error message.
    /// </summary>
    [TestMethod]
    public void CrossApply_WithUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.City from #schema.first() a cross apply #schema.second(a.Country) t";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country1", Population = 200 },
            new() { City = "City3", Country = "Country2", Population = 300 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" },
            new() { Country = "Country2", Money = 3000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["City1"], ["City1"], ["City2"], ["City2"], ["City3"]);
    }

    /// <summary>
    ///     Test case with two cross applies where the second alias is unused.
    /// </summary>
    [TestMethod]
    public void CrossApply_WithSecondUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            select a.City, b.Money 
            from #schema.first() a 
            cross apply #schema.second(a.Country) b 
            cross apply #schema.third() c";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var thirdSource = new List<CrossApplyClass3>
        {
            new() { Id = 1, Description = "Desc1" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)), ("b.Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["City1", 1000m]);
    }

    /// <summary>
    ///     Test case where cross apply uses property access but the alias is not used in select.
    /// </summary>
    [TestMethod]
    public void CrossApply_PropertyAccessWithUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.City from #schema.first() a cross apply a.Values as b";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Values = [1, 2, 3] },
            new() { City = "City2", Values = [4, 5] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["City1"], ["City1"], ["City1"], ["City2"], ["City2"]);
    }

    /// <summary>
    ///     Test case where cross apply alias is only used in WHERE but not in SELECT.
    /// </summary>
    [TestMethod]
    public void CrossApply_AliasUsedOnlyInWhere_ShouldWork()
    {
        const string query = @"
            select a.City 
            from #schema.first() a 
            cross apply #schema.second(a.Country) t 
            where t.Money > 1500";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "Country2", Population = 200 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" },
            new() { Country = "Country2", Money = 3000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["City1"], ["City2"]);
    }

    /// <summary>
    ///     Test case where the first alias 'a' is not used anywhere (but cross apply's 'b' is used).
    /// </summary>
    [TestMethod]
    public void CrossApply_FirstAliasUnused_ShouldNotThrowKeyNotFound()
    {
        const string query = "select b.Money from #schema.first() a cross apply #schema.second(a.Country) b";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("b.Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1000m]);
    }

    /// <summary>
    ///     Test case with only literal values selected (no alias used at all).
    /// </summary>
    [TestMethod]
    public void CrossApply_NoAliasUsedInSelect_ShouldNotThrowKeyNotFound()
    {
        const string query = "select 1 as Value from #schema.first() a cross apply #schema.second(a.Country) t";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [1]);
    }

    /// <summary>
    ///     Test case where cross apply with CTE and unused alias.
    /// </summary>
    [TestMethod]
    public void CrossApply_CteWithUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            with cte as (
                select a.City as City, a.Country as Country 
                from #schema.first() a
            )
            select c.City from cte c cross apply #schema.second(c.Country) t";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("c.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["City1"]);
    }

    /// <summary>
    ///     Test case where cross apply with method call on alias but alias not used in select.
    /// </summary>
    [TestMethod]
    public void CrossApply_MethodCallWithUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            select a.City
            from #schema.first() a
            cross apply a.Split(a.City, ',') t2";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1,City2", Country = "Country1", Population = 100 }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["City1,City2"], ["City1,City2"]);
    }

    /// <summary>
    ///     Test case CTE cross apply with method call and unused alias.
    /// </summary>
    [TestMethod]
    public void CrossApply_CteWithMethodCallAndUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            with testX as (
                select 'hello world' as Text
                from #schema.first() a
            )
            select t.Text
            from testX t
            cross apply t.Split(t.Text, ' ') unused";

        var firstSource = new EmptySourceEntity[1] { new() };
        var vm = CreateAndRunVirtualMachine(query, firstSource);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("t.Text", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["hello world"], ["hello world"]);
    }

    /// <summary>
    ///     Test star expansion with cross apply where second alias is not explicitly used.
    /// </summary>
    [TestMethod]
    public void CrossApply_StarExpansionWithUnusedSecondAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.* from #schema.first() a cross apply #schema.second(a.Country) t";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("a.Country", typeof(string)),
            ("a.Population", typeof(int)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["City1", "Country1", 100]);
    }

    /// <summary>
    ///     Test where cross apply is chained but middle alias is unused.
    /// </summary>
    [TestMethod]
    public void CrossApply_ChainedWithMiddleAliasUnused_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            select a.City, c.Money
            from #schema.first() a
            cross apply #schema.second(a.Country) b
            cross apply #schema.third() c";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();


        var thirdSource = new List<CrossApplyClass2>
        {
            new() { Country = "Any", Money = 5000, Month = "March" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            thirdSource);

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)), ("c.Money", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["City1", 5000m]);
    }

    /// <summary>
    ///     Test with multiple chained cross applies where intermediate aliases are unused.
    /// </summary>
    [TestMethod]
    public void CrossApply_MultipleChained_WithIntermediateUnusedAliases_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            select a.City
            from #schema.first() a
            cross apply #schema.second(a.Country) b
            cross apply #schema.second(a.Country) c";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["City1"]);
    }

    /// <summary>
    ///     Test outer apply with unused alias.
    /// </summary>
    [TestMethod]
    public void OuterApply_WithUnusedAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.City from #schema.first() a outer apply #schema.second(a.Country) t";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 },
            new() { City = "City2", Country = "NoMatch", Population = 200 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["City1"], ["City2"]);
    }

    /// <summary>
    ///     Test cross apply with count(*) aggregation where cross apply alias is not directly used.
    /// </summary>
    [TestMethod]
    public void CrossApply_WithCountAggregation_UnusedAliasInSelect_ShouldNotThrowKeyNotFound()
    {
        const string query = @"
            select a.City, a.Count(1) as Cnt
            from #schema.first() a
            cross apply #schema.second(a.Country) t
            group by a.City";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100 }
        }.ToArray();

        var secondSource = new List<CrossApplyClass2>
        {
            new() { Country = "Country1", Money = 1000, Month = "January" },
            new() { Country = "Country1", Money = 2000, Month = "February" }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource,
            secondSource,
            null,
            null,
            null,
            (parameters, source) =>
                source.Filter(f => (string)f.Country == RequireParameter<string>(parameters, 0)).ToArray());

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)), ("Cnt", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["City1", 2L]);
    }

    /// <summary>
    ///     Test case where cross apply with only accessing properties through nested path.
    /// </summary>
    [TestMethod]
    public void CrossApply_NestedPropertyAccess_UnusedIntermediateAlias_ShouldNotThrowKeyNotFound()
    {
        const string query = "select a.City from #schema.first() a cross apply a.Values as b";

        var firstSource = new List<CrossApplyClass1>
        {
            new() { City = "City1", Country = "Country1", Population = 100, Values = [1, 2, 3] }
        }.ToArray();

        var vm = CreateAndRunVirtualMachine(
            query,
            firstSource
        );

        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["City1"], ["City1"], ["City1"]);
    }

    /// <summary>
    ///     Test cross apply followed by a join where the cross apply alias is unused.
    /// </summary>
}
