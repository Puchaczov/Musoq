using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void ParentSizedChildSchema_ShouldReadContainingCount()
    {
        var table = RunBinaryQuery("binary Item { V: byte }; binary Body { Items: Item[parent.Count] }; binary Rec { Count: byte, Body: Body }; select i.V from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Body.Items i", [2, 10, 20]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10], [(byte)20]);
    }

    [TestMethod]
    public void AncestorSizedChildSchema_ShouldReadGrandparentCount()
    {
        var table = RunBinaryQuery("binary Leaf { Values: byte[parent.parent.Count] }; binary Body { Child: Leaf }; binary Rec { Count: byte, Body: Body }; select i.Value from #test.files() f cross apply Interpret<Rec>(f.Content) r cross apply r.Body.Child.Values i", [2, 10, 20]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)10], [(byte)20]);
    }

    [TestMethod]
    public void ParentDependentGuardAndCase_ShouldUseCompletedComputedFields()
    {
        var table = RunBinary("binary Child { V: byte when NOT (parent.Enabled = false), Derived: CASE WHEN parent.Enabled THEN V ELSE 9 END }; binary Rec { Flag: byte, Enabled: Flag = 1, Child: Child, Tail: byte };",
            "r.Child.Derived, r.Tail", [1, 7, 99], [0, 99]);

        TableMaterializationTestHelper.AssertRowsInOrder(table, [7, (byte)99], [9, (byte)99]);
    }

    [TestMethod]
    [DataRow("binary Child { Values: byte[parent.Count] }; binary Rec { Child: Child, Count: byte };")]
    [DataRow("binary Child { Values: byte[parent.Missing] }; binary Rec { Count: byte, Child: Child };")]
    [DataRow("binary Child { Values: byte[parent.parent.Count] }; binary Rec { Count: byte, Child: Child };")]
    [DataRow("binary Rec { Values: byte[parent.Count] };")]
    public void InvalidParentContext_ShouldReportSchemaDiagnostic(string schema)
    {
        var exception = Assert.Throws<MusoqQueryException>(() => RunBinary(schema, "r.Values[0]", [1]));

        StringAssert.Contains(exception.Message, "MQ2030");
    }
}
