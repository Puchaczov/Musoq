using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationCoreLanguageTests
{
    #region §2.5 Raw String Literals

    [TestMethod]
    [DataRow(@"r'C:\Some\Path\To\Directory'", @"C:\Some\Path\To\Directory")]
    [DataRow(@"R'C:\Some\Path\To\Directory'", @"C:\Some\Path\To\Directory")]
    [DataRow(@"r'\\server\share'", @"\\server\share")]
    [DataRow(@"r'C:\Temp\'", @"C:\Temp\")]
    [DataRow("r'a''b'", "a'b")]
    public void Spec_RawStringLiteral_DocumentedExamples_ShouldReturnExactValue(
        string operation,
        string expected)
    {
        TestMethodTemplate(operation, expected);
    }

    [TestMethod]
    public void Spec_RawStringLiteral_ShouldRemainDistinctFromOrdinaryEscapes()
    {
        TestMethodBatchTemplate(
            (@"'\n'", "\n"),
            (@"r'\n'", @"\n"));
    }

    #endregion
}
