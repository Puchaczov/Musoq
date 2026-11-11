using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator.Tests.Schema.Multi;
using Musoq.Evaluator.Tests.Schema.Multi.First;
using Musoq.Evaluator.Tests.Schema.Multi.Second;
using Musoq.Parser.Diagnostics;
using static Musoq.Evaluator.Tests.MusoqExceptionAssertions;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class AliasTests : MultiSchemaTestBase
{
    [TestMethod]
    public void WhenUniqueColumnAcrossJoinedDataSetOccurred_ShouldNotNeedToUseAlias()
    {
        const string query = "select ZeroItem from #schema.first() first inner join #schema.second() second on 1 = 1";

        var vm = CreateAndRunVirtualMachine(query, [new FirstEntity()], [new SecondEntity()]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("ZeroItem", table.Columns.ElementAt(0).ColumnName);
    }

    [TestMethod]
    public void WhenNonExistingAliasUsed_ShouldThrow()
    {
        const string query = "select b.ZeroItem from #schema.first() a";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, [new FirstEntity()], [new SecondEntity()])
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3015_UnknownAlias, DiagnosticPhase.Bind, "b");
    }

    [TestMethod]
    public void WhenAmbiguousColumnAcrossJoinedDataSetOccurred_ShouldNeedToUseAlias()
    {
        const string query =
            "select first.FirstItem, second.FirstItem from #schema.first() first inner join #schema.second() second on 1 = 1";

        var vm = CreateAndRunVirtualMachine(query, [new FirstEntity()], [new SecondEntity()]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("FirstItem", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("FirstItem", table.Columns.ElementAt(1).ColumnName);
    }

    [TestMethod]
    public void WhenCteUsesExplicitDottedAlias_ShouldBeAccessibleByRawColumnNameAccessSyntax()
    {
        const string query = (
            "\n"
            + "with p as (\n"
            + "    select \n"
            + "        first.FirstItem as [first.FirstItem],\n"
            + "        second.FirstItem as [second.FirstItem]\n"
            + "    from #schema.first() first \n"
            + "    inner join #schema.second() second on 1 = 1\n"
            + ")\n"
            + "select [first.FirstItem], [second.FirstItem] from p"
        );

        var vm = CreateAndRunVirtualMachine(query, [new FirstEntity()], [new SecondEntity()]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("first.FirstItem", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("second.FirstItem", table.Columns.ElementAt(1).ColumnName);
    }

    [TestMethod]
    public void WhenCteUsesExplicitDottedAlias_ShouldBeAccessibleByAliasedColumnNameAccessSyntax()
    {
        const string query = (
            "\n"
            + "with p as (\n"
            + "    select \n"
            + "        first.FirstItem as [first.FirstItem],\n"
            + "        second.FirstItem as [second.FirstItem]\n"
            + "    }\n"
            + "    from #schema.first() first\n"
            + "    inner join #schema.second() second on 1 = 1\n"
            + ")\n"
            + "select p.[first.FirstItem], p.[second.FirstItem] from p"
        );

        var vm = CreateAndRunVirtualMachine(query, [new FirstEntity()], [new SecondEntity()]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("first.FirstItem", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("second.FirstItem", table.Columns.ElementAt(1).ColumnName);
    }

    [TestMethod]
    public void WhenCteUsesExplicitDottedAlias_ShouldBeAccessibleByAliasedColumnNameAccessSyntaxWithAlias()
    {
        const string query = (
            "\n"
            + "with p as (\n"
            + "    select \n"
            + "        first.FirstItem as [first.FirstItem],\n"
            + "        second.FirstItem as [second.FirstItem]\n"
            + "    from #schema.first() first\n"
            + "    inner join #schema.second() second on 1 = 1\n"
            + "), q as (\n"
            + "    select p.[first.FirstItem] as FirstItem, p.[second.FirstItem] as SecondItem from p\n"
            + ")\n"
            + "select q.FirstItem, q.SecondItem from q"
        );

        var vm = CreateAndRunVirtualMachine(query, [new FirstEntity()], [new SecondEntity()]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("FirstItem", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("SecondItem", table.Columns.ElementAt(1).ColumnName);
    }

    [TestMethod]
    public void WhenCteUsesExplicitDottedAlias_ShouldBeAccessibleByAliasedColumnNameAccessSyntaxWithAliasAndAlias()
    {
        const string query = (
            "\n"
            + "with p as (\n"
            + "    select \n"
            + "        first.FirstItem as [first.FirstItem],\n"
            + "        second.FirstItem as [second.FirstItem]\n"
            + "    }\n"
            + "    from #schema.first() first\n"
            + "    inner join #schema.second() second on 1 = 1\n"
            + "), q as (\n"
            + "    select p.[first.FirstItem] as [p.first.FirstItem], p.[second.FirstItem] as [p.second.FirstItem] from p\n"
            + ")\n"
            + "select q.[p.first.FirstItem], q.[p.second.FirstItem] from q"
        );

        var vm = CreateAndRunVirtualMachine(query, [new FirstEntity()], [new SecondEntity()]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(2, table.Columns.Count());

        Assert.AreEqual("p.first.FirstItem", table.Columns.ElementAt(0).ColumnName);
        Assert.AreEqual("p.second.FirstItem", table.Columns.ElementAt(1).ColumnName);
    }

    [TestMethod]
    public void WhenAliasUsedWithinCte_AndSameUsedWithinOuterQuery_AliasesShouldNotClash()
    {
        const string query = (
            "\n"
            + "with p as (\n"
            + "    select \n"
            + "        1 \n"
            + "    from #schema.first() first\n"
            + "    cross apply first.Split('') b\n"
            + ")\n"
            + "select \n"
            + "    1 \n"
            + "from p inner join #schema.first() first on 1 = 1\n"
            + "cross apply first.Split('') b"
        );

        var vm = CreateAndRunVirtualMachine(query, [new FirstEntity()], [new SecondEntity()]);

        var table = vm.Run(TestContext.CancellationToken);

        Assert.AreEqual(1, table.Columns.Count());

        Assert.AreEqual("1", table.Columns.ElementAt(0).ColumnName);
    }

    [TestMethod]
    public void WhenSameAliasUsedInFromAndJoin_ShouldThrow()
    {
        const string query =
            "select a.FirstItem from #schema.first() a inner join #schema.second() a on a.FirstItem = a.FirstItem";

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                query,
                [new FirstEntity(), new FirstEntity()],
                [new SecondEntity(), new SecondEntity()]
            )
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3021_DuplicateAlias, DiagnosticPhase.Bind);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenSameAliasUsedInMultipleJoins_ShouldThrow()
    {
        const string query = (
            "\n"
            + "            select src.FirstItem \n"
            + "            from #schema.first() src \n"
            + "            inner join #schema.second() b on src.FirstItem = b.FirstItem\n"
            + "            inner join #schema.third() b on b.FirstItem = src.FirstItem"
        );

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                query,
                [new FirstEntity(), new FirstEntity(), new FirstEntity()],
                [new SecondEntity(), new SecondEntity(), new SecondEntity()]
            )
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3021_DuplicateAlias, DiagnosticPhase.Bind);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenSameAliasUsedInCTEAndMainQuery_ShouldThrow()
    {
        const string query = (
            "\n"
            + "            with src as (\n"
            + "                select FirstItem from #schema.first()\n"
            + "            )\n"
            + "            select src.FirstItem \n"
            + "            from #schema.second() src\n"
            + "            inner join src on src.FirstItem = src.FirstItem"
        );

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                query,
                [new FirstEntity(), new FirstEntity()],
                [new SecondEntity(), new SecondEntity()]
            )
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3021_DuplicateAlias, DiagnosticPhase.Bind, "src");
    }

    [TestMethod]
    public void WhenCteReferenceUsesDuplicateJoinAlias_ShouldReportDuplicateAlias()
    {
        const string query = (
            "\n"
            + "            with cte as (\n"
            + "                select FirstItem from #schema.first()\n"
            + "            )\n"
            + "            select a.FirstItem\n"
            + "            from #schema.first() a\n"
            + "            inner join cte a on 1 = 1"
        );

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(query, [new FirstEntity()], [new SecondEntity()])
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3021_DuplicateAlias, DiagnosticPhase.Bind, "a");
    }

    [TestMethod]
    public void WhenSameAliasUsedInCrossApply_ShouldThrow()
    {
        const string query = (
            "\n"
            + "            select a.FirstItem \n"
            + "            from #schema.first() a \n"
            + "            cross apply #schema.second() a"
        );

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                query,
                [new FirstEntity(), new FirstEntity()],
                [new SecondEntity(), new SecondEntity()]
            )
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3021_DuplicateAlias, DiagnosticPhase.Bind);
        AssertHasGuidance(ex);
    }

    [TestMethod]
    public void WhenSameAliasUsedInOuterApply_ShouldThrow()
    {
        const string query = (
            "\n"
            + "            select a.FirstItem \n"
            + "            from #schema.first() a \n"
            + "            outer apply #schema.second() a"
        );

        var ex = Assert.Throws<MusoqQueryException>(() =>
            CreateAndRunVirtualMachine(
                query,
                [new FirstEntity(), new FirstEntity()],
                [new SecondEntity(), new SecondEntity()]
            )
        );

        AssertErrorEnvelope(ex, DiagnosticCode.MQ3021_DuplicateAlias, DiagnosticPhase.Bind);
        AssertHasGuidance(ex);
    }
}
