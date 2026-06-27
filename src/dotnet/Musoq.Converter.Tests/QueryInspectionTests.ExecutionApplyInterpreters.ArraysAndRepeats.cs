using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderPrimitiveArrayBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreatePrimitiveArrayBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [ArrayPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("Values: short[]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderPrimitiveArrayBinaryInterpretSource_ShouldProjectArrayElements()
    {
        var compiled = CompileForExecution(
            CreatePrimitiveArrayBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "arrays",
                    Line = "INFO arrays",
                    Numbers = [],
                    Content = CreatePrimitiveArrayPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("arrays", table[0][0]);
        Assert.AreEqual((byte)3, table[0][1]);
        Assert.AreEqual((short)1, table[0][2]);
        Assert.AreEqual((short)3, table[2][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderStringArrayBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateStringArrayBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [StringArrayPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("Names: string[]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderStringArrayBinaryInterpretSource_ShouldProjectArrayElements()
    {
        var compiled = CompileForExecution(
            CreateStringArrayBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "string-arrays",
                    Line = "INFO string arrays",
                    Numbers = [],
                    Content = CreateStringArrayPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("string-arrays", table[0][0]);
        Assert.AreEqual((byte)3, table[0][1]);
        Assert.AreEqual("Ada", table[0][2]);
        Assert.AreEqual("Cal", table[2][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderInlineSchemaArrayBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateInlineSchemaArrayBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [InlineArrayPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("Items:", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderInlineSchemaArrayBinaryInterpretSource_ShouldProjectArrayElements()
    {
        var compiled = CompileForExecution(
            CreateInlineSchemaArrayBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "inline-arrays",
                    Line = "INFO inline arrays",
                    Numbers = [],
                    Content = CreateInlineSchemaArrayPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("inline-arrays", table[0][0]);
        Assert.AreEqual((byte)2, table[0][1]);
        Assert.AreEqual((byte)0xA1, table[0][2]);
        Assert.AreEqual((short)258, table[0][3]);
        Assert.AreEqual((byte)0xB2, table[1][2]);
        Assert.AreEqual((short)772, table[1][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderPrimitiveRepeatUntilBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreatePrimitiveRepeatUntilBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [PrimitiveRepeatPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("Values: byte[]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderPrimitiveRepeatUntilBinaryInterpretSource_ShouldProjectRepeatedPrimitiveElements()
    {
        var compiled = CompileForExecution(
            CreatePrimitiveRepeatUntilBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "repeat-primitives",
                    Line = "INFO repeat primitives",
                    Numbers = [],
                    Content = CreatePrimitiveRepeatUntilPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(4, table.Count);
        Assert.AreEqual("repeat-primitives", table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual((byte)0, table[3][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderBitsRepeatUntilBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateBitsRepeatUntilBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [BitsRepeatPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("Flags: byte[]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderBitsRepeatUntilBinaryInterpretSource_ShouldProjectRepeatedBitValues()
    {
        var compiled = CompileForExecution(
            CreateBitsRepeatUntilBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "repeat-bits",
                    Line = "INFO repeat bits",
                    Numbers = [],
                    Content = CreateBitsRepeatUntilPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("repeat-bits", table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual((byte)0, table[1][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderStringRepeatUntilBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateStringRepeatUntilBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [StringRepeatPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("Names: string[]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderStringRepeatUntilBinaryInterpretSource_ShouldProjectRepeatedStringElements()
    {
        var compiled = CompileForExecution(
            CreateStringRepeatUntilBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "repeat-strings",
                    Line = "INFO repeat strings",
                    Numbers = [],
                    Content = CreateStringRepeatUntilPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("repeat-strings", table[0][0]);
        Assert.AreEqual("Ada", table[0][1]);
        Assert.AreEqual("END", table[2][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderInlineSchemaRepeatUntilBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateInlineSchemaRepeatUntilBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [InlineRepeatPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("Items:", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderInlineSchemaRepeatUntilBinaryInterpretSource_ShouldProjectRepeatedInlineElements()
    {
        var compiled = CompileForExecution(
            CreateInlineSchemaRepeatUntilBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "repeat-inline",
                    Line = "INFO repeat inline",
                    Numbers = [],
                    Content = CreateInlineSchemaRepeatUntilPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("repeat-inline", table[0][0]);
        Assert.AreEqual((byte)0xA1, table[0][1]);
        Assert.AreEqual((short)258, table[0][2]);
        Assert.AreEqual((byte)0xB2, table[1][1]);
        Assert.AreEqual((short)772, table[1][2]);
        Assert.AreEqual((byte)0x00, table[2][1]);
        Assert.AreEqual((short)1029, table[2][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderSchemaReferenceArrayBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateSchemaReferenceArrayBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [SchemaArrayPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("Items: object[]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderSchemaReferenceArrayBinaryInterpretSource_ShouldProjectArrayElements()
    {
        var compiled = CompileForExecution(
            CreateSchemaReferenceArrayBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "schema-arrays",
                    Line = "INFO schema arrays",
                    Numbers = [],
                    Content = CreateSchemaReferenceArrayPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("schema-arrays", table[0][0]);
        Assert.AreEqual((byte)3, table[0][1]);
        Assert.AreEqual((byte)0xAA, table[0][2]);
        Assert.AreEqual((byte)0xCC, table[2][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderSchemaReferenceRepeatUntilBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateSchemaReferenceRepeatUntilBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [SchemaRepeatPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("Items:", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderSchemaReferenceRepeatUntilBinaryInterpretSource_ShouldProjectRepeatedSchemaElements()
    {
        var compiled = CompileForExecution(
            CreateSchemaReferenceRepeatUntilBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "repeat-schemas",
                    Line = "INFO repeat schemas",
                    Numbers = [],
                    Content = CreateSchemaReferenceRepeatUntilPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("repeat-schemas", table[0][0]);
        Assert.AreEqual((byte)0xAA, table[0][1]);
        Assert.AreEqual((byte)0x00, table[2][1]);
    }

}
