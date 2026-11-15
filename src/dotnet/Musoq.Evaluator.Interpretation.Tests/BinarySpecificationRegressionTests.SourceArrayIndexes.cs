using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void SourceArrayIndexOnAbsentInterpretedCollection_ShouldPreserveNullableMemberMetadata()
    {
        using var table = RunBinary(
            "binary Item { Value: byte }; binary Rec { Flag: byte, Position: int le, Items: Item[1] when Flag = 1 };",
            "r.Items[r.Position].Value as Value",
            [0, 0, 0, 0, 0],
            [1, 0, 0, 0, 0, 10]
        );
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(byte?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, new object?[] { null }, [(byte)10]);
    }

    [TestMethod]
    public void SourceArrayIndexOnInterpretedRecords_ShouldRetainNestedMemberTypes()
    {
        using var table = RunBinary(
            "binary Item { Value: byte }; binary Rec { Position: int le, Items: Item[2] };",
            "r.Items[r.Position].Value as Value, Length(r.Items[r.Position - 1].Value::string) as Width",
            [1, 0, 0, 0, 10, 20]
        );
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(byte?)), ("Width", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)20, 2]);
    }

    [TestMethod]
    public void SourceArrayIndexOnJaggedRecords_ShouldRetainEachGeneratedElementType()
    {
        using var table = RunBinary(
            "binary Item { Value: byte }; binary Rec { Position: int le, Items: Item[1][2] };",
            "r.Items[r.Position][r.Position - 1].Value",
            [1, 0, 0, 0, 10, 20]
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)20]);
    }

    [TestMethod]
    public void SourceArrayIndexThroughCte_ShouldRetainTheRecordSchema()
    {
        using var table = RunBinaryQuery(
            """
            binary Item { Value: byte }; binary Rec { Position: int le, Items: Item[2] };
            with c as (
                select r.Items, r.Position from #test.files() f cross apply Interpret<Rec>(f.Content) r
            ), Selected as (select c.Items[c.Position] as Item from c c)
            select s.Item.Value from Selected s
            """,
            new CompilationOptions(usePrimitiveTypeValidation: false),
            [1, 0, 0, 0, 10, 20]
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)20]);
    }

    [TestMethod]
    public void CombinedInterpretedCteQuery_ShouldComposeAllFourFixes()
    {
        using var table = RunBinaryQuery(
            """
            binary Item { Value: byte };
            binary Rec {
                Flag: byte, Optional: byte when Flag = 1,
                Present: (Optional IS NOT NULL), Count: byte, Items: Item[Count]
            };
            with Parsed as (
                select r.Flag, r.Present, Length(r.Items) as Size,
                       r.Items[r.Flag::int ?? 0].Value as Value,
                       case when r.Flag = 0 then 3 else Length(r.Items) end as Amount
                from #test.files() f cross apply Interpret<Rec>(f.Content) r
            ), Counts as (select p.Flag, Count(1) as Rows from Parsed p group by p.Flag)
            select p.Flag, p.Present, p.Size, p.Value, p.Amount, c.Rows
            from Parsed p inner join Counts c on p.Flag = c.Flag order by p.Flag
            """,
            [0, 2, 10, 20],
            [1, 7, 2, 30, 40]
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [(byte)0, false, 2, (byte)10, 3, 1L],
            [(byte)1, true, 2, (byte)40, 2, 1L]
        );
    }
}
