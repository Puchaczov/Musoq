using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    public void IndexedSwitchBranches_ShouldPreserveNullableMemberValuesAndDiscriminator()
    {
        var table = RunBinary(
            "binary PayloadRecord { Value: ushort be }; binary Item { Kind: byte, Payload: switch Kind { 1 => Branch: PayloadRecord, _ => Raw: byte[2] } }; binary Rec { Items: Item[1], Trailer: byte };",
            "r.Items[0].Payload.Case as SelectedCase, r.Items[0].Payload.Raw.Length as RawLength, r.Items[0].Payload.Branch.Value as BranchValue, r.Trailer",
            [0, 0x11, 0x22, 0xEE], [1, 0x11, 0x22, 0xEE]);

        TableMaterializationTestHelper.AssertColumns(table,
            ("SelectedCase", typeof(string)), ("RawLength", typeof(int?)),
            ("BranchValue", typeof(ushort?)), ("Trailer", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            ["Raw", 2, null, (byte)0xEE], ["Branch", null, (ushort)0x1122, (byte)0xEE]);
    }

    [TestMethod]
    public void ConditionalSwitch_ShouldPreserveAbsentUnionAndAbsentBranchMembers()
    {
        var table = RunBinary(
            "binary PayloadRecord { Value: ushort be }; binary Rec { Flag: byte, Kind: byte, Payload: switch Kind { 1 => Branch: PayloadRecord, _ => Raw: byte[2] } when Flag = 1, Trailer: byte };",
            "r.Payload.Case as SelectedCase, r.Payload.Raw.Length as RawLength, r.Payload.Branch.Value as BranchValue, r.Trailer",
            [0, 1, 0xEE], [1, 1, 0x11, 0x22, 0xEE]);

        TableMaterializationTestHelper.AssertRowsInOrder(table,
            [null, null, null, (byte)0xEE], ["Branch", null, (ushort)0x1122, (byte)0xEE]);
    }

    [TestMethod]
    public void EnumeratedSwitchWrapperBranches_ShouldPreserveNullableMemberValues()
    {
        const string sql = """
            binary NarrowPayload { Value: byte };
            binary WidePayload { Value: ushort be };
            binary TaggedItem {
                Kind: byte,
                Payload: switch Kind {
                    1 => Narrow: NarrowPayload,
                    2 => Wide: WidePayload
                }
            };
            binary Frame {
                Count: byte,
                Items: TaggedItem[Count],
                Trailer: byte check Trailer = 0xEE
            };
            select item.Ordinal as Ordinal, item.Kind as Kind,
                   item.Payload.Narrow.Value as NarrowValue,
                   item.Payload.Wide.Value as WideValue,
                   r.Trailer as Trailer
            from #test.files() f
            cross apply Interpret<Frame>(f.Content) r
            cross apply r.Items item with ordinality
            order by item.Ordinal
            """;

        var table = RunBinaryQuery(sql, [0x02, 0x01, 0x11, 0x02, 0x12, 0x34, 0xEE]);

        TableMaterializationTestHelper.AssertColumns(table,
            ("Ordinal", typeof(int)), ("Kind", typeof(byte)),
            ("NarrowValue", typeof(byte?)), ("WideValue", typeof(ushort?)),
            ("Trailer", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table,
            [0, (byte)1, (byte)0x11, null, (byte)0xEE],
            [1, (byte)2, null, (ushort)0x1234, (byte)0xEE]);
    }
}
