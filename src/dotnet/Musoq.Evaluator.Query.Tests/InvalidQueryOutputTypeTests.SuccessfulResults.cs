using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class InvalidQueryOutputTypeTests
{
    [TestMethod]
    public void WhenSelectPrimitiveTypes_ShouldSucceed()
    {
        var query = "select Name, City, Population, Money, Time from #A.Entities()";
        var time = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity { Name = "entity1", City = "city1", Population = 1000m, Money = 12.5m, Time = time }] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table,
            ("Name", typeof(string)), ("City", typeof(string)), ("Population", typeof(decimal)),
            ("Money", typeof(decimal)), ("Time", typeof(DateTime)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["entity1", "city1", 1000m, 12.5m, time]);
    }

    [TestMethod]
    public void WhenCteWithPrimitiveTypes_ShouldSucceed()
    {
        var query = @"
with cte as (
    select Name, City from #A.Entities()
)
select Name, City from cte";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("city1", "country1", 1000)] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["", "city1"]);
    }

    [TestMethod]
    public void WhenSelectNullableType_ShouldSucceed()
    {
        var query = "select NullableValue from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("test")] }
        };

        var vm = CreateAndRunVirtualMachineWithValidation(query, sources);
        var table = vm.Run(TokenSource.Token);

        TableMaterializationTestHelper.AssertColumns(table, ("NullableValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null });
    }
}
