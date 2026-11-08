using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public class InterpretPipelineIntegrationTests : BinaryOrTextualEvaluatorTestBase
{
    [TestMethod]
    public void WhenBinaryInterpretCrossApplyQuery_ShouldProduceInterpretSourceInBothPlans()
    {
        const string query = @"
            binary Header {
                Magic: byte
            };
            select
                f.Name,
                h.Magic
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h";

        var buildItems = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            new BinarySchemaProvider(
                new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = [new BinaryEntity { Name = "header.bin", Content = [0x2A] }]
                }),
            LoggerResolver);

        Assert.IsNotNull(buildItems.RequireLogicalPlan());
        Assert.IsNotNull(buildItems.RequirePhysicalPlan());

        var logicalProject = PipelinePlanAssertions.FindLogicalApplyProject(buildItems.RequireLogicalPlan());
        var logicalApply = (ApplyNode)logicalProject.Input;
        Assert.AreEqual(ApplyKind.Cross, logicalApply.Kind);
        Assert.IsInstanceOfType<SchemaScanNode>(logicalApply.Left);
        Assert.IsInstanceOfType<InterpretSourceNode>(logicalApply.Right);
        var logicalInterpret = (InterpretSourceNode)logicalApply.Right;
        Assert.AreEqual(InterpretSourceKind.Interpret, logicalInterpret.Kind);
        Assert.AreEqual(ApplyKind.Cross, logicalInterpret.ApplyKind);
        Assert.AreEqual("Header", logicalInterpret.SchemaName);
        Assert.AreEqual("h", logicalInterpret.Alias);
        Assert.HasCount(1, logicalInterpret.Arguments);
        Assert.IsInstanceOfType<ColumnRef>(logicalInterpret.Arguments[0]);
        var logicalArgument = (ColumnRef)logicalInterpret.Arguments[0];
        Assert.AreEqual("f", logicalArgument.Alias);
        Assert.AreEqual("Content", logicalArgument.ColumnName);

        PipelinePlanAssertions.AssertFinalLogicalStatementUsesCteRef(buildItems.RequireLogicalPlan());

        var physicalProject = PipelinePlanAssertions.FindPhysicalApplyProject(buildItems.RequirePhysicalPlan());
        var physicalApply = (PhysicalNestedLoopApplyNode)physicalProject.Input;
        Assert.AreEqual(ApplyKind.Cross, physicalApply.Kind);
        Assert.IsInstanceOfType<PhysicalSchemaScanNode>(physicalApply.Left);
        Assert.IsInstanceOfType<PhysicalInterpretSourceNode>(physicalApply.Right);
        var physicalInterpret = (PhysicalInterpretSourceNode)physicalApply.Right;
        Assert.AreEqual(InterpretSourceKind.Interpret, physicalInterpret.Kind);
        Assert.AreEqual(ApplyKind.Cross, physicalInterpret.ApplyKind);
        Assert.AreEqual("Header", physicalInterpret.SchemaName);
        Assert.AreEqual("h", physicalInterpret.Alias);
        Assert.HasCount(1, physicalInterpret.Arguments);
        Assert.IsInstanceOfType<ColumnRef>(physicalInterpret.Arguments[0]);
        var physicalArgument = (ColumnRef)physicalInterpret.Arguments[0];
        Assert.AreEqual("f", physicalArgument.Alias);
        Assert.AreEqual("Content", physicalArgument.ColumnName);

        PipelinePlanAssertions.AssertFinalPhysicalStatementUsesCteRef(buildItems.RequirePhysicalPlan());
    }

    [TestMethod]
    public void WhenBinaryInterpretCrossApplyQuery_ShouldUseScalarRowStreamInExecutionPlan()
    {
        const string query = @"
            binary Header {
                Magic: byte
            };
            select h.Magic
            from #test.files() f
            cross apply Interpret<Header>(f.Content) h";

        var buildItems = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            new BinarySchemaProvider(
                new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = [new BinaryEntity { Name = "header.bin", Content = [0x2A] }]
                }),
            LoggerResolver);

        var executionPlanText = buildItems.RequireExecutionPlanText();

        Assert.Contains("ScalarForEach [h in hRows]", executionPlanText);
        Assert.IsFalse(executionPlanText.Contains("ChunkedForEach [h in hRows]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WhenTextTryParseOuterApplyQuery_ShouldPreserveTryParseSemanticsInBothPlans()
    {
        const string query = @"
            text LogEntry {
                Timestamp: between '[' ']',
                _: literal ' ',
                Level: until ':',
                _: literal ' ',
                Message: rest
            };
            select
                f.Name,
                log.Level
            from #test.lines() f
            outer apply TryParse<LogEntry>(f.Text) log";

        var buildItems = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            new TextSchemaProvider(
                new Dictionary<string, IEnumerable<TextEntity>>
                {
                    ["#test"] = [new TextEntity { Name = "app.log", Text = "[2026-03-09] INFO: booted" }]
                }),
            LoggerResolver);

        Assert.IsNotNull(buildItems.RequireLogicalPlan());
        Assert.IsNotNull(buildItems.RequirePhysicalPlan());

        var logicalProject = PipelinePlanAssertions.FindLogicalApplyProject(buildItems.RequireLogicalPlan());
        var logicalApply = (ApplyNode)logicalProject.Input;
        Assert.AreEqual(ApplyKind.Outer, logicalApply.Kind);
        Assert.IsInstanceOfType<InterpretSourceNode>(logicalApply.Right);
        var logicalInterpret = (InterpretSourceNode)logicalApply.Right;
        Assert.AreEqual(InterpretSourceKind.TryParse, logicalInterpret.Kind);
        Assert.AreEqual(ApplyKind.Outer, logicalInterpret.ApplyKind);
        Assert.AreEqual("LogEntry", logicalInterpret.SchemaName);
        Assert.AreEqual("log", logicalInterpret.Alias);
        Assert.HasCount(1, logicalInterpret.Arguments);
        Assert.IsInstanceOfType<ColumnRef>(logicalInterpret.Arguments[0]);
        var logicalArgument = (ColumnRef)logicalInterpret.Arguments[0];
        Assert.AreEqual("f", logicalArgument.Alias);
        Assert.AreEqual("Text", logicalArgument.ColumnName);

        PipelinePlanAssertions.AssertFinalLogicalStatementUsesCteRef(buildItems.RequireLogicalPlan());

        var physicalProject = PipelinePlanAssertions.FindPhysicalApplyProject(buildItems.RequirePhysicalPlan());
        var physicalApply = (PhysicalNestedLoopApplyNode)physicalProject.Input;
        Assert.AreEqual(ApplyKind.Outer, physicalApply.Kind);
        Assert.IsInstanceOfType<PhysicalInterpretSourceNode>(physicalApply.Right);
        var physicalInterpret = (PhysicalInterpretSourceNode)physicalApply.Right;
        Assert.AreEqual(InterpretSourceKind.TryParse, physicalInterpret.Kind);
        Assert.AreEqual(ApplyKind.Outer, physicalInterpret.ApplyKind);
        Assert.AreEqual("LogEntry", physicalInterpret.SchemaName);
        Assert.AreEqual("log", physicalInterpret.Alias);
        Assert.HasCount(1, physicalInterpret.Arguments);
        Assert.IsInstanceOfType<ColumnRef>(physicalInterpret.Arguments[0]);
        var physicalArgument = (ColumnRef)physicalInterpret.Arguments[0];
        Assert.AreEqual("f", physicalArgument.Alias);
        Assert.AreEqual("Text", physicalArgument.ColumnName);

        PipelinePlanAssertions.AssertFinalPhysicalStatementUsesCteRef(buildItems.RequirePhysicalPlan());
    }

    [TestMethod]
    public void WhenBinaryInterpretAsTextProjectionUsesNestedMember_ShouldPreserveFullColumnPathInFinalProjection()
    {
        const string query = @"
            text Data {
                Content: rest
            };
            binary Packet {
                Len: byte,
                Text: string[Len] utf8 as Data,
                Trailer: byte
            };
            select p.Len, p.Text.Content, p.Trailer from #test.files() f
            cross apply Interpret<Packet>(f.Content) p";

        var buildItems = InstanceCreator.CreateForAnalyze(
            query,
            Guid.NewGuid().ToString(),
            new BinarySchemaProvider(
                new Dictionary<string, IEnumerable<BinaryEntity>>
                {
                    ["#test"] = [new BinaryEntity { Name = "test.bin", Content = [0x00, 0xFF] }]
                }),
            LoggerResolver);

        var logicalProject = (ProjectNode)PipelinePlanAssertions.UnwrapMultiStatement(buildItems.RequireLogicalPlan());
        var logicalContentField = logicalProject.Fields[1];
        Assert.AreEqual("p.Text.Content", IrExpressionPrinter.Print(logicalContentField.Expression));

        var physicalProject = (PhysicalProjectNode)PipelinePlanAssertions.UnwrapPhysicalMultiStatement(buildItems.RequirePhysicalPlan());
        var physicalContentField = physicalProject.Fields[1];
        Assert.AreEqual("p.Text.Content", IrExpressionPrinter.Print(physicalContentField.Expression));
    }
}
