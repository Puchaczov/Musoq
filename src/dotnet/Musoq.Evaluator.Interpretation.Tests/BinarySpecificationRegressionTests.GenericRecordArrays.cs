using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void GenericRecordArrayLength_ShouldUseTheDeclaredElementType()
    {
        using var table = RunBinary(
            "binary Item { Value: byte }; binary Rec { Items: Item[2] };",
            "Length(r.Items) as Count, IsNull(r.Items) as Missing",
            [10, 20]
        );

        TableMaterializationTestHelper.AssertColumns(table, ("Count", typeof(int?)), ("Missing", typeof(bool)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, false]);
    }

    [TestMethod]
    public void GenericRecordArrayLengthOnAbsentCollection_ShouldPreserveNull()
    {
        using var table = RunBinary(
            "binary Item { Value: byte }; binary Rec { Flag: byte, Items: Item[2] when Flag = 1 };",
            "Length(r.Items), IsNull(r.Items)",
            [0],
            [1, 10, 20]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, true], [2, false]);
    }

    [TestMethod]
    public void GenericRecordArrayLengthOnRepeatedCollection_ShouldCountEmptyAndNonEmptyInput()
    {
        using var table = RunBinary(
            "binary Item { Value: byte }; binary Rec { Items: Item repeat until eof };",
            "Length(r.Items)",
            [],
            [10, 20, 30]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [0], [3]);
    }

    [TestMethod]
    public void GenericRecordArrayLengthThroughSubstreamMember_ShouldRetainItsShape()
    {
        using var table = RunBinary(
            "binary Item { Value: byte }; binary Header { Types: Item repeat until eof }; binary Rec { Header: substream[2] as Header exact };",
            "Length(r.Header.Types)",
            [10, 20]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [2]);
    }

    [TestMethod]
    public void GenericRecordArrayLengthAfterRecordIndex_ShouldRetainItsShape()
    {
        using var table = RunBinary(
            "binary Item { Value: byte }; binary Group { Items: Item[2] }; binary Rec { Groups: Group[2] };",
            "Length(r.Groups[1].Items)",
            [10, 20, 30, 40]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [2]);
    }

    [TestMethod]
    public void GenericRecordArrayLengthOnJaggedCollection_ShouldRetainEachDimension()
    {
        using var table = RunBinary(
            "binary Item { Value: byte }; binary Rec { Items: Item[1][2] };",
            "Length(r.Items), Length(r.Items[0])",
            [10, 20]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, 1]);
    }

    [TestMethod]
    public void GenericRecordArrayLengthThroughCteProjections_ShouldRetainItsShape()
    {
        using var table = RunBinaryQuery(
            """
            binary Item { Value: byte }; binary Rec { Items: Item[2] };
            with Parsed as (
                select r.Items as Items from #test.files() f cross apply Interpret<Rec>(f.Content) r
            ), Projected as (select p.Items as Items from Parsed p)
            select Length(p.Items) as Count from Projected p
            """,
            new CompilationOptions(usePrimitiveTypeValidation: false),
            [10, 20]
        );

        TableMaterializationTestHelper.AssertColumns(table, ("Count", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2]);
    }

    [TestMethod]
    public void GenericRecordArrayBinding_ShouldPreservePrimitiveArrayAndStringOverloads()
    {
        using var table = RunBinary(
            "binary Rec { Bytes: byte[2], Text: string[2] ascii };",
            "Length(r.Bytes), Length(r.Text)",
            [10, 20, 65, 66]
        );

        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, 2]);
    }
}
