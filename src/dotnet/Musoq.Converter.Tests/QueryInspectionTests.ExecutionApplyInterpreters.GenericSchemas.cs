using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Schema;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderGenericBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateGenericBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [GenericContainer.Interpret(i.Content) -> cRows]", result.ExecutionPlanText);
        Assert.Contains("Items.Data:", result.ExecutionPlanText);
        AssertExecutionPlanContains("EnumerableSource [ic.c.Items.Data -> dRows]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderGenericBinaryInterpretSource_ShouldProjectGenericArrayElements()
    {
        var compiled = CompileForExecution(
            CreateGenericBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "generic",
                    Line = "INFO generic",
                    Numbers = [],
                    Content = CreateGenericPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("generic", table[0][0]);
        Assert.AreEqual((byte)0x0A, table[0][1]);
        Assert.AreEqual((byte)0x0C, table[2][1]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderNestedGenericBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(CreateNestedGenericBinaryInterpretQuery(), CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains(
            "InterpretSource [NestedGenericContainer.Interpret(i.Content) -> cRows]",
            result.ExecutionPlanText);
        Assert.Contains("Items.Data:", result.ExecutionPlanText);
        AssertExecutionPlanContains("EnumerableSource [ic.c.Items.Data -> pRows]", result.ExecutionPlanText);
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderNestedGenericBinaryInterpretSource_ShouldProjectNestedGenericElements()
    {
        var compiled = CompileForExecution(
            CreateNestedGenericBinaryInterpretQuery(),
            new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "nested-generic",
                    Line = "INFO nested generic",
                    Numbers = [],
                    Content = CreateNestedGenericPacketContent()
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("nested-generic", table[0][0]);
        Assert.AreEqual((byte)0x0A, table[0][1]);
        Assert.AreEqual((short)0x1234, table[0][2]);
        Assert.AreEqual((byte)0x0B, table[1][1]);
        Assert.AreEqual((short)0x5678, table[1][2]);
    }

    [TestMethod]
    public void CompileForInspection_WhenDefaultExecutionIrRoutingCanRenderInheritedBinaryInterpretSource_ShouldUseExecutionBackend()
    {
        var result = Inspect(@"
                binary BaseHeader {
                    Version: byte
                };
                binary ExtendedHeader extends BaseHeader {
                    Payload: byte[2]
                };
                select i.Name, h.Version from #apply.items() i cross apply Interpret<ExtendedHeader>(i.Content) h", CreateApplyCandidateSchemaProvider());

        AssertUsesExecutionBackend(result);
        AssertExecutionPlanContains("InterpretSource [ExtendedHeader.Interpret(i.Content) -> hRows]", result.ExecutionPlanText);
        Assert.Contains("h.Version: byte <- field h_Version", result.ExecutionPlanText);
        Assert.Contains("Payload: byte[] <- property Payload", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("Statement0Row0", StringComparison.Ordinal));
        AssertGeneratedCSharpDoesNotContain("EvaluationHelper.SmartForEach", result.GeneratedCSharpCode);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderInheritedBinaryInterpretSource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution(
            @"
                binary BaseHeader {
                    Version: byte
                };
                binary ExtendedHeader extends BaseHeader {
                    Payload: byte[2]
                };
                select i.Name, h.Version from #apply.items() i cross apply Interpret<ExtendedHeader>(i.Content) h", new ApplyCandidateSchemaProvider(
            [
                new ApplyCandidateEntity
                {
                    Name = "packet",
                    Line = "INFO packet",
                    Numbers = [],
                    Content = [7, 0xAA, 0xBB]
                }
            ]));

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("packet", table[0][0]);
        Assert.AreEqual((byte)7, table[0][1]);
    }

}
