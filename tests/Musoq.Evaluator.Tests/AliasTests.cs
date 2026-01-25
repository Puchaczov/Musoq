using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class AliasTests : MultiSchemaTestBase
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public void WhenUniqueColumnAcrossJoinedDataSetOccurred_ShouldNotNeedToUseAlias()
    {
        const string query = "select ZeroItem from #schema.first() first inner join #schema.second() second on 1 = 1";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("ZeroItem", table.Columns.ElementAt(0).ColumnName);
    }

    [TestMethod]
    public void WhenNonExistingAliasUsed_ShouldThrow()
    {
        const string query = "select b.ZeroItem from #schema.first() a";

        Assert.Throws<UnknownColumnOrAliasException>(() => CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]));
    }

    [TestMethod]
    public void WhenAmbiguousColumnAcrossJoinedDataSetOccurred_ShouldNeedToUseAlias()
    {
        const string query =
            "select first.FirstItem, second.FirstItem from #schema.first() first inner join #schema.second() second on 1 = 1";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("first.FirstItem", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("second.FirstItem", table.Columns.ElementAt(1).ColumnName);
    }

    [TestMethod]
    public void WhenCteInheritsAliasedName_ShouldBeAccessibleByRawColumnNameAccessSyntax()
    {
        const string query = @"
with p as (
    select 
        first.FirstItem, 
        second.FirstItem 
    from #schema.first() first 
    inner join #schema.second() second on 1 = 1
)
select [first.FirstItem], [second.FirstItem] from p";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("first.FirstItem", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("second.FirstItem", table.Columns.ElementAt(1).ColumnName);
    }

    [TestMethod]
    public void WhenCteInheritsAliasedName_ShouldBeAccessibleByAliasedColumnNameAccessSyntax()
    {
        const string query = @"
with p as (
    select 
        first.FirstItem, 
        second.FirstItem
    }
    from #schema.first() first
    inner join #schema.second() second on 1 = 1
)
select p.[first.FirstItem], p.[second.FirstItem] from p";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("p.first.FirstItem", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("p.second.FirstItem", table.Columns.ElementAt(1).ColumnName);
    }

    [TestMethod]
    public void WhenCteInheritsAliasedName_ShouldBeAccessibleByAliasedColumnNameAccessSyntaxWithAlias()
    {
        const string query = @"
with p as (
    select 
        first.FirstItem, 
        second.FirstItem
    from #schema.first() first
    inner join #schema.second() second on 1 = 1
), q as (
    select p.[first.FirstItem] as FirstItem, p.[second.FirstItem] as SecondItem from p
)
select q.FirstItem, q.SecondItem from q";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("q.FirstItem", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("q.SecondItem", table.Columns.ElementAt(1).ColumnName);
    }

    [TestMethod]
    public void WhenCteInheritsAliasedName_ShouldBeAccessibleByAliasedColumnNameAccessSyntaxWithAliasAndAlias()
    {
        const string query = @"
with p as (
    select 
        first.FirstItem, 
        second.FirstItem
    }
    from #schema.first() first
    inner join #schema.second() second on 1 = 1
), q as (
    select p.[first.FirstItem], p.[second.FirstItem] from p
)
select q.[p.first.FirstItem], q.[p.second.FirstItem] from q";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("q.p.first.FirstItem", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("q.p.second.FirstItem", table.Columns.ElementAt(1).ColumnName);
    }

    [TestMethod]
    public void WhenAliasUsedWithinCte_AndSameUsedWithinOuterQuery_AliasesShouldNotClash()
    {
        const string query = @"
with p as (
    select 
        1 
    from #schema.first() first
    cross apply first.Split('') b
)
select 
    1 
from p inner join #schema.first() first on 1 = 1
cross apply first.Split('') b";

        var vm = CreateAndRunVirtualMachine(query, [
            new FirstEntity()
        ], [
            new SecondEntity()
        ]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("1", table.Columns.ElementAt(0).ColumnName);
    }

    [TestMethod]
    public void WhenSameAliasUsedInFromAndJoin_ShouldThrow()
    {
        const string query =
            "select a.FirstItem from #schema.first() a inner join #schema.second() a on a.FirstItem = a.FirstItem";

        Assert.Throws<AliasAlreadyUsedException>(() => CreateAndRunVirtualMachine(query, [
            new FirstEntity(),
            new FirstEntity()
        ], [
            new SecondEntity(),
            new SecondEntity()
        ]));
    }

    [TestMethod]
    public void WhenSameAliasUsedInMultipleJoins_ShouldThrow()
    {
        const string query = @"
            select src.FirstItem 
            from #schema.first() src 
            inner join #schema.second() b on src.FirstItem = b.FirstItem
            inner join #schema.third() b on b.FirstItem = src.FirstItem";

        Assert.Throws<AliasAlreadyUsedException>(() => CreateAndRunVirtualMachine(query, [
            new FirstEntity(),
            new FirstEntity(),
            new FirstEntity()
        ], [
            new SecondEntity(),
            new SecondEntity(),
            new SecondEntity()
        ]));
    }

    [TestMethod]
    public void WhenSameAliasUsedInCTEAndMainQuery_ShouldThrow()
    {
        const string query = @"
            with src as (
                select FirstItem from #schema.first()
            )
            select src.FirstItem 
            from #schema.second() src
            inner join src on src.FirstItem = src.FirstItem";

        Assert.Throws<AliasAlreadyUsedException>(() => CreateAndRunVirtualMachine(query, [
            new FirstEntity(),
            new FirstEntity()
        ], [
            new SecondEntity(),
            new SecondEntity()
        ]));
    }

    [TestMethod]
    public void WhenSameAliasUsedInCrossApply_ShouldThrow()
    {
        const string query = @"
            select a.FirstItem 
            from #schema.first() a 
            cross apply #schema.second() a";

        Assert.Throws<AliasAlreadyUsedException>(() => CreateAndRunVirtualMachine(query, [
            new FirstEntity(),
            new FirstEntity()
        ], [
            new SecondEntity(),
            new SecondEntity()
        ]));
    }

    [TestMethod]
    public void WhenSameAliasUsedInOuterApply_ShouldThrow()
    {
        const string query = @"
            select a.FirstItem 
            from #schema.first() a 
            outer apply #schema.second() a";

        Assert.Throws<AliasAlreadyUsedException>(() => CreateAndRunVirtualMachine(query, [
            new FirstEntity(),
            new FirstEntity()
        ], [
            new SecondEntity(),
            new SecondEntity()
        ]));
    }
}
