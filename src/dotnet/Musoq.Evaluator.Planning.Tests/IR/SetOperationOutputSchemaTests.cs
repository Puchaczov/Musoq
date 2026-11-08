using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class SetOperationOutputSchemaTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CombiningNullableAndNonNullablePlans_ShouldProduceNullableOutput(bool nullableOnRight)
    {
        var left = new OutputSchema([new ColumnSchema("K", nullableOnRight ? typeof(int) : typeof(int?), 0)]);
        var right = new OutputSchema([new ColumnSchema("Other", nullableOnRight ? typeof(int?) : typeof(int), 0)]);

        var result = OutputSchemaFactory.ForSetOperation(left, right);

        Assert.AreEqual(typeof(int?), result.Columns[0].Type);
        Assert.AreEqual(typeof(int?), result.Columns[0].SourceReadType);
        Assert.AreEqual("K", result.Columns[0].Name);
    }
}
