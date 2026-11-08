using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class RewriteWhereExpressionToPassItToDataSourceTests
{
    [TestMethod]
    [DataRow("b.City in ('NYC', 'LA')")]
    [DataRow("b.City contains ('NYC', 'LA')")]
    [DataRow("b.City is null")]
    [DataRow("b.City is not null")]
    [DataRow("b.Population between 1 and 3")]
    public void ForeignAtom_ShouldNeverEnterSourceLocalPredicate(string predicate)
    {
        var result = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            $"select a.Name from #A.entities() a join #B.entities() b on a.City = b.City where {predicate}");
        Assert.AreEqual("1 = 1", result.UsedWhereNodes.Single(entry => entry.Key.Alias == "a").Value.Expression.ToString());
    }

    [TestMethod]
    [DataRow("a.Population between 1 and 3")]
    [DataRow("a.Population between 1 and 3 and a.City = 'NYC'")]
    [DataRow("a.City = 'NYC' or a.Population between 1 and 3")]
    [DataRow("not (a.Population between 1 and 3)")]
    public void SourceLocalRange_ShouldRemainACompleteBooleanPredicate(string predicate)
    {
        var result = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            $"select a.Name from #A.entities() a where {predicate}");
        Assert.AreEqual(predicate, result.UsedWhereNodes.Single().Value.Expression.ToString());
    }

    [TestMethod]
    [DataRow("not (a.Population < b.Population)")]
    [DataRow("not (b.Population > a.Population + 2)")]
    [DataRow("not (b.Population < 2)")]
    [DataRow("not (a.Population > 0 and b.Population < 2)")]
    [DataRow("not (a.Population > 0 or b.Population < 2)")]
    [DataRow("not (not (a.Population < b.Population))")]
    [DataRow("not (not (not (a.Population < b.Population)))")]
    [DataRow("not (DoNothing(a.City) = 'NYC')")]
    [DataRow("not (a.City rlike b.City)")]
    [DataRow("not (b.City in ('NYC', 'LA'))")]
    [DataRow("not (b.City contains ('NYC', 'LA'))")]
    [DataRow("not (b.Population between 1 and 3)")]
    [DataRow("not (b.City is null)")]
    public void WhenNegatedOperandCannotTransferExactly_ShouldOfferNeutralPredicate(string predicate)
    {
        var result = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            $"select a.Name from #A.entities() a join #B.entities() b on a.City = b.City where {predicate}");

        Assert.AreEqual("1 = 1", result.UsedWhereNodes.Single(entry => entry.Key.Alias == "a").Value.Expression.ToString());
    }

    [TestMethod]
    [DataRow("not (a.Population < 2)")]
    [DataRow("not (a.City like 'NY%')")]
    [DataRow("not (a.City rlike 'NY.*')")]
    [DataRow("not (a.City in ('NYC', 'LA'))")]
    [DataRow("not (a.City is null)")]
    [DataRow("not (1 = 1)")]
    [DataRow("not (1 = 0)")]
    public void WhenNegatedOperandTransfersExactly_ShouldPreserveNegation(string predicate)
    {
        var result = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            $"select a.Name from #A.entities() a where {predicate}");

        Assert.AreEqual(predicate, result.UsedWhereNodes.Single().Value.Expression.ToString());
    }

    [TestMethod]
    public void WhenUnsafeNegationHasIndependentConjunct_ShouldPreserveSafeConjunct()
    {
        var result = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select a.Name from #A.entities() a join #B.entities() b on a.City = b.City where a.Population > 0 and not (a.Population < b.Population)");

        Assert.AreEqual("a.Population > 0 and 1 = 1", result.UsedWhereNodes.Single(entry => entry.Key.Alias == "a").Value.Expression.ToString());
    }
}
