using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Tests;

public partial class BinarySpecificationRegressionTests
{
    [TestMethod]
    [DataRow("string[Len] ascii", new byte[] { 1, 2, 65, 66, 99 }, "r.Content.Pick", "AB")]
    [DataRow("bits[3]", new byte[] { 1, 2, 7, 99 }, "r.Content.Pick", (byte)7)]
    [DataRow("short le[Len]", new byte[] { 1, 2, 1, 0, 2, 0, 99 }, "r.Content.Pick", new short[] { 1, 2 })]
    [DataRow("{ Value: byte }", new byte[] { 1, 2, 7, 99 }, "r.Content.Pick.Value", (byte)7)]
    [DataRow("{ Data: byte[Len] }", new byte[] { 1, 2, 7, 8, 99 }, "r.Content.Pick.Data", new byte[] { 7, 8 })]
    [DataRow("switch Kind { 1 => Inner: byte }", new byte[] { 1, 2, 7, 99 }, "r.Content.Pick.Inner", (byte)7)]
    [DataRow("substream[Len] as { Value: byte } lax", new byte[] { 1, 2, 7, 8, 99 }, "r.Content.Pick.Value", (byte)7)]
    [DataRow("byte repeat until Pick[-1] = 0", new byte[] { 1, 2, 7, 0, 99 }, "r.Content.Pick", new byte[] { 7, 0 })]
    [DataRow("bits[2][Len]", new byte[] { 1, 2, 9, 99 }, "r.Content.Pick", new byte[] { 1, 2 })]
    public void BinarySwitch_ShouldReadComposedBranchAndAlignFollowingField(string type, byte[] input, string projection, object payload)
    {
        using var table = RunBinaryQuery($"binary Rec {{ Kind: byte, Len: byte, Content: switch Kind {{ 1 => Pick: {type}, _ => Other: byte }}, Tail: byte }}; " +
            $"select r.Content.Case, {projection} as Payload, r.Tail from #test.files() f cross apply Interpret<Rec>(f.Content) r",
            new CompilationOptions(usePrimitiveTypeValidation: false), input);
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Pick", payload, (byte)99]);
    }

    [TestMethod]
    [DataRow("bits[2][2]", new byte[] { 9, 99 }, "r.Items[0], r.Items[1]", (byte)1, (byte)2)]
    [DataRow("short le[2][2]", new byte[] { 1, 0, 2, 0, 3, 0, 4, 0, 99 }, "r.Items[0], r.Items[1]", new short[] { 1, 2 }, new short[] { 3, 4 })]
    [DataRow("substream[1] as { Value: byte } exact[2]", new byte[] { 7, 8, 99 }, "r.Items[0].Value, r.Items[1].Value", (byte)7, (byte)8)]
    [DataRow("(byte repeat until Items[-1] = 0)[2]", new byte[] { 7, 0, 8, 0, 99 }, "r.Items[0].Length, r.Items[1].Length", 2, 2)]
    public void BinaryArray_ShouldReadRecursiveElements(string type, byte[] input, string projection, object first, object second)
    {
        using var table = RunBinaryQuery($"binary Rec {{ Items: {type}, Tail: byte }}; " +
            $"select {projection}, r.Tail from #test.files() f cross apply Interpret<Rec>(f.Content) r",
            new CompilationOptions(usePrimitiveTypeValidation: false), input);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [first, second, (byte)99]);
    }

    [TestMethod]
    public void SwitchRepeatBits_ShouldMeasureProgressWithinTheCurrentByte()
    {
        using var table = RunBinary("binary Rec { Kind: byte, Content: switch Kind { 1 => Pick: bits[1] repeat until eof } };",
            "r.Content.Pick.Length", [1, 255]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [8]);
    }

    [TestMethod]
    public void UnselectedComposedBranch_ShouldNotEvaluateItsSize()
    {
        using var table = RunBinary("binary Rec { Kind: byte, Zero: byte, Content: switch Kind { 1 => Pick: byte, _ => Failing: string[1 % Zero] ascii }, Tail: byte };",
            "r.Content.Pick, r.Tail", [1, 0, 7, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, (byte)99]);
    }

    [TestMethod]
    public void BranchRepeatName_ShouldShadowAnEnclosingField()
    {
        using var table = RunBinary("binary Rec { Pick: byte, Kind: byte, Content: switch Kind { 1 => Pick: byte repeat until Pick[-1] = 0 }, Tail: byte };",
            "r.Content.Pick.Length, r.Tail", [99, 1, 7, 0, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, (byte)99]);
    }

    [TestMethod]
    public void RepeatOfSwitch_ShouldReadOnlySelectedPayloads()
    {
        using var table = RunBinary("binary Rec { Kind: byte, Items: switch Kind { 1 => Value: byte } repeat until Items[-1].Value = 0, Tail: byte };",
            "r.Items.Length, r.Items[0].Value, r.Tail", [1, 7, 0, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2, (byte)7, (byte)99]);
    }

    [TestMethod]
    [DataRow("case when Items[-1] = 0 then true else false end", 2)]
    [DataRow("(Missing ?? Items[-1]) = 0", 2)]
    [DataRow("Items[-1] = 0 or 1 % Zero = 0", 1)]
    public void RepeatConditions_ShouldUseSharedSchemaExpressionRules(string condition, int count)
    {
        using var table = RunBinary($"binary Rec {{ Flag: byte, Zero: byte, Missing: int le when Flag = 1, Items: byte repeat until {condition} }};",
            "r.Items.Length", count == 1 ? [0, 0, 0] : [0, 0, 7, 0]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [count]);
    }

    [TestMethod]
    public void NullLeftRepeatCondition_ShouldEvaluateRequiredRightOperand()
    {
        var exception = Assert.Throws<QueryExecutionException>(() => RunBinary(
            "binary Rec { Flag: byte, Zero: byte, Missing: int le when Flag = 1, Items: byte repeat until Missing > 0 and 1 % Zero = 0 };",
            "r.Items.Length", [0, 0, 7]));
        Assert.IsInstanceOfType<DivideByZeroException>(exception.InnerException);
    }

    [TestMethod]
    public void EmptyStructuredSubstream_ShouldUseNestedSchemaByteAlignment()
    {
        using var table = RunBinary("binary Empty { }; binary Rec { Leading: bits[3], Empty: substream[0] as Empty, After: bits[2], Tail: byte };",
            "r.Leading, r.After, r.Tail", [7, 2, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, (byte)2, (byte)99]);
    }

    [TestMethod]
    public void NestedCompositions_ShouldKeepDistinctTypesForRepeatedFieldNames()
    {
        using var table = RunBinary("binary Rec { Kind: byte, Content: switch Kind { 1 => Pick: { Kind: byte, Content: switch Kind { 2 => Value: byte } } }, Tail: byte };",
            "r.Content.Pick.Content.Value, r.Tail", [1, 2, 7, 99]);
        TableMaterializationTestHelper.AssertRowsInOrder(table, [(byte)7, (byte)99]);
    }

    [TestMethod]
    public void GroupedGenericRepetitions_ShouldRetainNestedRecordTypes()
    {
        using var table = RunBinaryQuery("binary Part { Value: byte }; binary Box<T> { Node: T }; " +
            "binary Rec { Items: (Box<Part> repeat until Items[-1].Node.Value = 0)[2], Tail: byte }; " +
            "select r.Items, r.Tail from #test.files() f cross apply Interpret<Rec>(f.Content) r",
            new CompilationOptions(usePrimitiveTypeValidation: false), [7, 0, 8, 0, 99]);
        var items = (Array)table[0].ReadValue(0)!;
        Assert.AreEqual("Box`1", items.GetType().GetElementType()!.GetElementType()!.Name);
        Assert.AreEqual(2, items.Length);
        for (var index = 0; index < items.Length; index++)
        {
            var group = (Array)items.GetValue(index)!;
            Assert.AreEqual(2, group.Length);
            var first = PartialPayloadAssertions.CompletedValues(group.GetValue(0)!)["Node"]!;
            var last = PartialPayloadAssertions.CompletedValues(group.GetValue(1)!)["Node"]!;
            Assert.AreEqual("Part", first.GetType().Name);
            Assert.AreEqual((byte)(7 + index), PartialPayloadAssertions.CompletedValues(first)["Value"]);
            Assert.AreEqual((byte)0, PartialPayloadAssertions.CompletedValues(last)["Value"]);
        }
        Assert.AreEqual((byte)99, table[0].ReadValue(1));
    }

    [TestMethod]
    public void UnselectedBitBranch_ShouldRetainNullableMetadata()
    {
        using var table = RunBinary("binary Rec { Kind: byte, Content: switch Kind { 1 => Flag: bits[3], _ => Other: byte }, Tail: byte };",
            "r.Content.Flag, r.Tail", [2, 7, 99]);
        TableMaterializationTestHelper.AssertColumns(table, ("r.Content.Flag", typeof(byte?)), ("Tail", typeof(byte)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [null, (byte)99]);
    }
}
