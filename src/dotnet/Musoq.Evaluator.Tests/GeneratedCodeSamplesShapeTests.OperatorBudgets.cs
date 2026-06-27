using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void OperatorFamilyBudgets_WhenCheckedIn_ShouldCoverEveryCatalogCategory()
    {
        var actualCategories = ReadSamples()
            .Select(static sample => sample.Category)
            .Distinct()
            .OrderBy(static category => category)
            .ToArray();
        var budgetCategories = OperatorFamilyBudgets.Keys
            .OrderBy(static category => category)
            .ToArray();

        CollectionAssert.AreEqual(budgetCategories, actualCategories);
    }

    [TestMethod]
    public void OperatorFamilies_WhenCheckedIn_ShouldStayWithinRuntimeV2ShapeBudgets()
    {
        AssertOperatorFamilyBudgets(ReadSamples());
    }
}
