using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderCrossApplyInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(@"
                text LogLine {
                    Level: until ' ',
                    Message: rest
                };
                select i.Name, l.Level, l.Message from #apply.items() i cross apply Parse<LogLine>(i.Line) l", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [LogLine.Parse(i.Line) -> lRows]", result.ExecutionPlanText);
        AssertExecutionPlanContains("ForEach [l in lRows]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderCrossApplyInterpretSource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(@"
                text LogLine {
                    Level: until ' ',
                    Message: rest
                };
                select i.Name, l.Level, l.Message from #apply.items() i cross apply Parse<LogLine>(i.Line) l", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual("INFO", table[0][1]);
        Assert.AreEqual("ready", table[0][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderTryParseInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(@"
                text InfoLine {
                    _: literal 'INFO: ',
                    Message: rest
                };
                select i.Name, l.Message from #apply.items() i outer apply TryParse<InfoLine>(i.Line) l", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [InfoLine.TryParse(i.Line) -> lRows]", result.ExecutionPlanText);
        Assert.Contains("If [NOT lHasMatch]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderTryParseOuterApply_ShouldPreserveInvalidRowsAsNull()
    {
        var compiled = CompileForExecution(@"
                text InfoLine {
                    _: literal 'INFO: ',
                    Message: rest
                };
                select i.Name, l.Message from #apply.items() i outer apply TryParse<InfoLine>(i.Line) l", new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "valid",
                    Line = "INFO: ready",
                    Numbers = []
                },
                new ApplyCandidateEntity
                {
                    Name = "invalid",
                    Line = "WARN: nope",
                    Numbers = []
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("valid", table[0][0]);
        Assert.AreEqual("ready", table[0][1]);
        Assert.AreEqual("invalid", table[1][0]);
        Assert.IsNull(table[1][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderConditionalBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateConditionalBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [OptionalPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("p.Value: int? <- field p_Value", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("Statement0Row0", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderConditionalBinaryInterpretSource_ShouldPreserveNullValue()
    {
        const int expectedPacketValue = 42;

        var compiled = CompileForExecution(
            CreateConditionalBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "present",
                    Line = "INFO present",
                    Numbers = [],
                    Content = CreateOptionalPacketContent(expectedPacketValue)
                },
                new ApplyCandidateEntity
                {
                    Name = "missing",
                    Line = "INFO missing",
                    Numbers = [],
                    Content = CreateOptionalPacketContent(null)
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("present", table[0][0]);
        Assert.AreEqual((byte)1, table[0][1]);
        Assert.AreEqual(expectedPacketValue, table[0][2]);
        Assert.AreEqual("missing", table[1][0]);
        Assert.AreEqual((byte)0, table[1][1]);
        Assert.IsNull(table[1][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderConstrainedOffsetBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateConstrainedOffsetBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [IndexedPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("Magic: int <- property Magic", result.ExecutionPlanText);
        Assert.Contains("p.Data: int <- field p_Data", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("Statement0Row0", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderConstrainedOffsetBinaryInterpretSource_ShouldSeekAndValidate()
    {
        const int expectedData = 1234;

        var compiled = CompileForExecution(
            CreateConstrainedOffsetBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "indexed",
                    Line = "INFO indexed",
                    Numbers = [],
                    Content = CreateIndexedPacketContent(expectedData)
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("indexed", table[0][0]);
        Assert.AreEqual(IndexedPacketHeaderSize, table[0][1]);
        Assert.AreEqual(IndexedPacketDataOffset, table[0][2]);
        Assert.AreEqual(expectedData, table[0][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderComputedBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateComputedBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [Rectangle.Interpret(i.Content) -> rRows]", result.ExecutionPlanText);
        Assert.Contains("Area:", result.ExecutionPlanText);
        Assert.IsTrue(result.GeneratedCSharpCode.Contains("r.Area", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderComputedBinaryInterpretSource_ShouldProjectComputedValue()
    {
        var compiled = CompileForExecution(
            CreateComputedBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "rectangle",
                    Line = "INFO rectangle",
                    Numbers = [],
                    Content = CreateRectangleContent(10, 5)
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("rectangle", table[0][0]);
        Assert.AreEqual(10, table[0][1]);
        Assert.AreEqual(5, table[0][2]);
        Assert.AreEqual(50, table[0][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderNestedBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateNestedBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [Vertex.Interpret(i.Content) -> vRows]", result.ExecutionPlanText);
        Assert.Contains("Position.X: float <- nested property Position.X", result.ExecutionPlanText);
        Assert.Contains("X: v.Position.X", result.ExecutionPlanText);
        Assert.Contains("v.Position.X", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderNestedBinaryInterpretSource_ShouldProjectNestedValues()
    {
        var compiled = CompileForExecution(
            CreateNestedBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "vertex",
                    Line = "INFO vertex",
                    Numbers = [],
                    Content = CreateVertexContent(7, 1.5f, 2.5f)
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("vertex", table[0][0]);
        Assert.AreEqual(7, table[0][1]);
        Assert.AreEqual(1.5f, table[0][2]);
        Assert.AreEqual(2.5f, table[0][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderInlineBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateInlineBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [InlinePacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("Header.Magic:", result.ExecutionPlanText);
        Assert.Contains("Magic: p.Header.Magic", result.ExecutionPlanText);
        Assert.Contains("p.Header.Magic", result.GeneratedCSharpCode);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderInlineBinaryInterpretSource_ShouldProjectInlineValues()
    {
        var compiled = CompileForExecution(
            CreateInlineBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "inline",
                    Line = "INFO inline",
                    Numbers = [],
                    Content = CreateInlinePacketContent(0x12345678, 258, 0xFF)
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("inline", table[0][0]);
        Assert.AreEqual(0x12345678, table[0][1]);
        Assert.AreEqual((short)258, table[0][2]);
        Assert.AreEqual((byte)0xFF, table[0][3]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderStringBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateStringBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [TextPacket.Interpret(i.Content) -> pRows]", result.ExecutionPlanText);
        Assert.Contains("p.Text: string <- field p_Text", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("Statement0Row0", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderStringBinaryInterpretSource_ShouldProjectStringValue()
    {
        var compiled = CompileForExecution(
            CreateStringBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "text",
                    Line = "INFO text",
                    Numbers = [],
                    Content = CreateTextPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("text", table[0][0]);
        Assert.AreEqual((byte)5, table[0][1]);
        Assert.AreEqual("Ada", table[0][2]);
    }

}
