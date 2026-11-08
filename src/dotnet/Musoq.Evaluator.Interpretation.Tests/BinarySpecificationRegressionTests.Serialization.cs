using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void GroupedGenericRepetitions_ShouldSerializeOnlyNestedPayloadMembers()
    {
        using var table = RunBinary("binary Part { Value: byte }; binary Box<T> { Node: T }; " +
            "binary Rec { Items: (Box<Part> repeat until Items[-1].Node.Value = 0)[2], Tail: byte };",
            "ToJson(r.Items) as Payload, r.Tail", [7, 0, 8, 0, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            ["[[{\"Node\":{\"Value\":7}},{\"Node\":{\"Value\":0}}],[{\"Node\":{\"Value\":8}},{\"Node\":{\"Value\":0}}]]", (byte)99]);
    }
}
