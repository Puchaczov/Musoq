using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    [DataRow("cross", "Item", "i.V")]
    [DataRow("outer", "Item", "i.V")]
    [DataRow("cross", "byte", "i.Value")]
    [DataRow("outer", "byte", "i.Value")]
    public void DeepApplyCollectionPath_ShouldResolveEverySegment(string apply, string element, string projection)
    {
        var table = RunBinaryQuery($"binary Item {{ V: byte }}; binary Inner {{ N: byte, Items: {element}[N] }}; binary Middle {{ B: Inner }}; binary Rec {{ A: Middle }}; select {projection} as Value from #test.files() f cross apply Interpret<Rec>(f.Content) r {apply} apply r.A.B.Items i",
            [2, 3, 4]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)3], [(byte)4]);
    }

    [TestMethod]
    [DataRow("cross", 0)]
    [DataRow("outer", 1)]
    public void DeepApplyAbsentRecord_ShouldHonorApplyKind(string apply, int count)
    {
        var table = RunBinaryQuery($"binary Item {{ V: byte }}; binary Inner {{ Items: Item[1] }}; binary Middle {{ B: Inner }}; binary Rec {{ Flag: byte, A: Middle when Flag = 1 }}; select i.V from #test.files() f cross apply Interpret<Rec>(f.Content) r {apply} apply r.A.B.Items i", [0]);

        Assert.AreEqual(count, table.Count);
    }
}
