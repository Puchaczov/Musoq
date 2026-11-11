using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class CteReadOnceFusionDeterminismTests : BasicEntityTestBase
{
    [TestMethod]
    public void CteReadOnceFusion_WhenNonDeterministicProjectionIsReadTwice_ShouldMaterializeProducerValue()
    {
        const string query =
            @"
            with p as (select NextTestValue() as r from #A.Entities())
            select p.r, p.r as again from p";

        AssertSingleRowQuery(query, 1, 1);
    }

    [TestMethod]
    public void CteReadOnceFusion_WhenNonDeterministicAliasIsComparedWithItself_ShouldMaterializeBeforeFiltering()
    {
        const string query =
            @"
            with p as (select NextTestValue() as r from #A.Entities())
            select p.r from p where p.r = p.r";

        AssertSingleRowQuery(query, 1);
    }

    [TestMethod]
    public void CteReadOnceFusion_WhenNonDeterministicFilteredColumnIsPruned_ShouldMaterializeBeforeFiltering()
    {
        const string query =
            @"
            with p as (select NextTestValue() as r, Name from #A.Entities())
            select p.Name from p where p.r = p.r";

        AssertSingleRowQuery(query, "A");
    }

    private void AssertSingleRowQuery(string query, params object?[] expected)
    {
        var fixture = new EvaluationFixture("A");
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: fixture.Provider);
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(table, expected);
        Assert.AreEqual(1, fixture.Calls);
    }
}
