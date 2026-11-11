using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class CSharpierLineBudgetBaselineTests
{
    [TestMethod]
    public void FormattingAllowance_ShouldPreserveExistingGrowthHeadroom()
    {
        const int originalLines = 157;
        const int originalBudget = 160;
        const int formattingDelta = 59;

        var adjusted = CSharpierLineBudgetBaseline.Apply(originalLines + formattingDelta, formattingDelta);

        Assert.AreEqual(originalBudget - originalLines, originalBudget - adjusted);
    }

    [TestMethod]
    public void FormattingAllowance_ShouldStillRejectGrowthBeyondOriginalBudget()
    {
        const int originalBudget = 160;
        const int formattingDelta = 59;

        var adjusted = CSharpierLineBudgetBaseline.Apply(originalBudget + formattingDelta + 1, formattingDelta);

        Assert.IsTrue(adjusted > originalBudget);
    }

    [TestMethod]
    public void FormattingAllowance_ShouldPreserveHeadroomWhenFormattingRemovesLines()
    {
        Assert.AreEqual(160, CSharpierLineBudgetBaseline.Apply(151, -9));
    }

    [TestMethod]
    public void NewFile_ShouldReceiveNoFormattingAllowance()
    {
        Assert.AreEqual(0, CSharpierLineBudgetBaseline.Lines("new-file-without-a-calibration.cs"));
    }
}
