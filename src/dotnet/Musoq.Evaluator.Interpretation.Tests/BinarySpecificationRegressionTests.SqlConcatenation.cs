using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    [DataRow("'a' || 'β'", "aβ")]
    [DataRow("'a' || null", null)]
    [DataRow("null || 'a'", null)]
    [DataRow("'a' || 'b' || 'c' || 'd' || 'e' || 'f' || 'g' || 'h'", "abcdefgh")]
    [DataRow("CASE WHEN Flag = 1 THEN 'a' END || 'b'", "ab")]
    public void EmbeddedSqlConcatenation_ShouldPreserveStringsAndNull(string expression, string? expected)
    {
        using var table = RunBinary($"binary Rec {{ Flag: byte, Derived: {expression} }};", "r.Derived", [1]);

        TableMaterializationTestHelper.AssertColumns(table, ("Derived", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { expected });
    }

    [TestMethod]
    public void EmbeddedLongVariableChain_ShouldUseCachedCallbackAndNativeStringState()
    {
        using var table = RunBinary("binary Rec { Name: string[1] ascii, Derived: Name || Name || Name || Name || Name || Name || Name || Name };",
            "r.Derived", [65]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["AAAAAAAA"]);
    }

    [TestMethod]
    public void EmbeddedRepeatCondition_ShouldUseSqlConcatenationForItsLastElement()
    {
        using var table = RunBinary("binary Item { Name: string[1] ascii } binary Rec { Items: Item repeat until Items[-1].Name || '!' = 'x!' };",
            "r.Items[0].Name as A, r.Items[1].Name as B", [65, 120]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, ["A", "x"]);
    }
}
