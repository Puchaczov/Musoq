using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class CteTests
{
    [TestMethod]
    public void WhenCteParallelizationEnabled_IndependentCtes_ShouldSucceed()
    {
        var query = @"
with p as (
    select City, Country from #A.entities()
), c as (
    select City, Country from #B.entities()
)
select p.City, p.Country, c.City as OtherCity from p inner join c on 1 = 1";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("PARIS", "FRANCE", 600)
                ]
            }
        };

        var compilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false, useCteParallelization: true);
        var vm = CreateAndRunVirtualMachine(query, sources, compilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.City", typeof(string)),
            ("p.Country", typeof(string)),
            ("OtherCity", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["WARSAW", "POLAND", "PARIS"],
            ["BERLIN", "GERMANY", "PARIS"]);
    }

    [TestMethod]
    public void WhenCteParallelizationEnabled_DependentCtes_ShouldSucceed()
    {
        var query = @"
with p as (
    select City, Country from #A.entities()
), d as (
    select City from p where Country = 'POLAND'
)
select * from d";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("CZESTOCHOWA", "POLAND", 400),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            }
        };

        var compilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false, useCteParallelization: true);
        var vm = CreateAndRunVirtualMachine(query, sources, compilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["WARSAW"],
            ["CZESTOCHOWA"]);
    }

    [TestMethod]
    public void WhenCteParallelizationEnabled_MultiLevelDependencies_ShouldSucceed()
    {
        var query = @"
with p as (
    select City, Country from #A.entities()
), c as (
    select City, Country from #B.entities()
), d as (
    select City from p where Country = 'POLAND'
), f as (
    select d.City as City1, c.City as City2 from d inner join c on 1 = 1
)
select * from f";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("WARSAW", "POLAND", 500),
                    new BasicEntity("BERLIN", "GERMANY", 250)
                ]
            },
            {
                "#B", [
                    new BasicEntity("PARIS", "FRANCE", 600)
                ]
            }
        };

        var compilationOptions = new CompilationOptions(usePrimitiveTypeValidation: false, useCteParallelization: true);
        var vm = CreateAndRunVirtualMachine(query, sources, compilationOptions);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City1", typeof(string)),
            ("City2", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["WARSAW", "PARIS"]);
    }

    [TestMethod]
    public void WhenCteWithOrderByAliasOfFunctionCall_ShouldSortByTransformedValue()
    {
        var query = @"
            with cte as (
                select Name as OldColumn, ToInt32(Name) as NumValue from #A.Entities()
            )
            select OldColumn, NumValue from cte order by NumValue asc";

        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            {
                "#A", [
                    new BasicEntity("20"),
                    new BasicEntity("3"),
                    new BasicEntity("1"),
                    new BasicEntity("10"),
                    new BasicEntity("2")
                ]
            }
        };

        var vm = CreateAndRunVirtualMachine(query, sources);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("OldColumn", typeof(string)),
            ("NumValue", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["1", 1],
            ["2", 2],
            ["3", 3],
            ["10", 10],
            ["20", 20]);
    }
}
