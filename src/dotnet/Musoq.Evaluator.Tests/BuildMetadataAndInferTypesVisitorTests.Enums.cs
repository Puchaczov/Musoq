using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.EnvironmentVariable;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public partial class BuildMetadataAndInferTypesVisitorTests
{
    [TestMethod]
    public void EnumDeclaration_ShouldCreatePortableQueryLocalDescriptor()
    {
        var visitor = Analyze(
            "flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui };" +
            "select 1 from #EnvironmentVariables.All()");

        Assert.IsTrue(visitor.QueryLocalEnumTypes.TryGetValue("fileaccess", out var descriptor));
        Assert.AreEqual(EnumTypeOrigin.QueryLocal, descriptor.Origin);
        Assert.AreEqual(EnumUnderlyingKind.UInt32, descriptor.UnderlyingKind);
        Assert.IsTrue(descriptor.IsFlags);
        Assert.HasCount(4, descriptor.Members);
        Assert.IsTrue(descriptor.TryGetValue("ReadWrite", out var readWrite));
        Assert.AreEqual(3u, readWrite.AsUInt32());
    }

    [TestMethod]
    public void TableColumnType_WhenQueryLocalEnum_ShouldExposeCarrierAndDescriptor()
    {
        var capturedContexts = new List<SourceMetadataContext>();
        Analyze(
            "enum JobStatus : int { Queued = 10, Running = 20, Finished = 30 };" +
            "table Jobs { Status: jobstatus };" +
            "couple #capture.any with table Jobs as Jobs;" +
            "select Status from Jobs()",
            new CaptureMetadataContextSchemaProvider(capturedContexts.Add));

        var column = capturedContexts
            .Single(context => context.AllColumns.Any(candidate => candidate.ColumnName == "Status"))
            .AllColumns
            .Single(candidate => candidate.ColumnName == "Status");

        Assert.AreEqual(typeof(int?), column.ColumnType);
        Assert.AreEqual(typeof(int?), column.SourceReadType);
        Assert.IsNotNull(column.EnumType);
        Assert.AreEqual("JobStatus", column.EnumType.DisplayName);
        Assert.AreEqual(EnumTypeOrigin.QueryLocal, column.EnumType.Origin);
    }

    [TestMethod]
    public void TableColumnType_WhenExactReachableNativeEnum_ShouldNormalizeToCarrier()
    {
        var capturedContexts = new List<SourceMetadataContext>();
        Analyze(
            "table Jobs { Status: Musoq.Evaluator.Tests.TableContractNativeStatus };" +
            "couple #capture.any with table Jobs as Jobs;" +
            "select Status from Jobs()",
            new CaptureMetadataContextSchemaProvider(capturedContexts.Add));

        var column = capturedContexts
            .Single(context => context.AllColumns.Any(candidate => candidate.ColumnName == "Status"))
            .AllColumns
            .Single(candidate => candidate.ColumnName == "Status");

        Assert.AreEqual(typeof(short?), column.ColumnType);
        Assert.AreEqual(typeof(TableContractNativeStatus?), column.SourceReadType);
        Assert.IsNotNull(column.EnumType);
        Assert.AreEqual(EnumTypeOrigin.NativeClr, column.EnumType.Origin);
    }

    [TestMethod]
    public void EnumDeclaration_WhenTypeNameDiffersOnlyByCase_ShouldRejectDuplicate()
    {
        var exception = Assert.Throws<Musoq.Parser.Exceptions.SyntaxException>(() => Analyze(
            "enum State : byte { Ready = 1ub };" +
            "enum state : byte { Done = 2ub };" +
            "select 1 from #EnvironmentVariables.All()"));

        Assert.AreEqual(DiagnosticCode.MQ2042_InvalidEnumDeclaration, exception.Code);
    }

    [TestMethod]
    public void EnumComparison_WhenUsingExactMemberName_ShouldBindPrimitiveConstant()
    {
        var visitor = Analyze(
            "enum JobStatus : int { Queued = 10, Running = 20, Finished = 30 };" +
            "table Jobs { Status: JobStatus };" +
            "couple #capture.any with table Jobs as Jobs;" +
            "select Status from Jobs() where Status = 'Running'",
            new CaptureMetadataContextSchemaProvider(_ => { }));
        var collector = new EnumComparisonCollector();

        visitor.Root.Accept(new EnumComparisonTraverser(collector));

        Assert.IsNotNull(collector.Equality);
        Assert.IsInstanceOfType<IntegerNode>(collector.Equality.Right);
        Assert.AreEqual(20, ((IntegerNode)collector.Equality.Right).ObjValue);
    }

    [TestMethod]
    public void EnumComparison_WhenMemberCasingDiffers_ShouldRejectUnknownMember()
    {
        var exception = Assert.Throws<VisitorException>(() => Analyze(
            "enum JobStatus : int { Running = 20 };" +
            "table Jobs { Status: JobStatus };" +
            "couple #capture.any with table Jobs as Jobs;" +
            "select Status from Jobs() where Status = 'running'",
            new CaptureMetadataContextSchemaProvider(_ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3108_UnknownEnumMember, exception.Code);
    }

    [TestMethod]
    public void EnumComparison_WhenComparedWithNumericLiteral_ShouldRejectImplicitConversion()
    {
        var exception = Assert.Throws<VisitorException>(() => Analyze(
            "enum JobStatus : int { Running = 20 };" +
            "table Jobs { Status: JobStatus };" +
            "couple #capture.any with table Jobs as Jobs;" +
            "select Status from Jobs() where Status = 20",
            new CaptureMetadataContextSchemaProvider(_ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3110_UnsupportedEnumOperator, exception.Code);
    }

    [TestMethod]
    public void EnumComparison_WhenIdentitiesDiffer_ShouldRejectEvenWithSameCarrier()
    {
        var exception = Assert.Throws<VisitorException>(() => Analyze(
            "enum JobStatus : int { Running = 20 };" +
            "enum OtherStatus : int { Running = 20 };" +
            "table Jobs { Status: JobStatus, Other: OtherStatus };" +
            "couple #capture.any with table Jobs as Jobs;" +
            "select Status from Jobs() where Status = Other",
            new CaptureMetadataContextSchemaProvider(_ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3109_EnumIdentityMismatch, exception.Code);
    }

    [TestMethod]
    public void EnumDeclaration_WhenTableReferencesLaterDeclaration_ShouldReportVisibilityError()
    {
        const string query =
            "table Jobs { Status: JobStatus };" +
            "enum JobStatus : int { Running = 20 };" +
            "select 1 from #EnvironmentVariables.All()";

        var result = new QueryAnalyzer(new EnvironmentVariablesSchemaProvider()).Analyze(query);

        Assert.IsTrue(result.Diagnostics.Any(diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ3107_UnknownEnumType));
    }

    [TestMethod]
    public void EnumDeclaration_WhenPlacedAfterExecutableStatement_ShouldReportStatementOrderError()
    {
        const string query =
            "select 1 from #EnvironmentVariables.All();" +
            "enum JobStatus : int { Running = 20 };";

        var result = new QueryAnalyzer(new EnvironmentVariablesSchemaProvider()).Analyze(query);

        Assert.IsTrue(result.Diagnostics.Any(diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ3102_InvalidStatementOrder));
    }

    private sealed class EnumComparisonCollector : NoOpExpressionVisitor
    {
        public EqualityNode? Equality { get; private set; }

        public override void Visit(EqualityNode node)
        {
            Equality = node;
        }
    }

    private sealed class EnumComparisonTraverser(EnumComparisonCollector visitor)
        : RawTraverseVisitor<EnumComparisonCollector>(visitor);
}

public enum TableContractNativeStatus : short
{
    Queued = 10,
    Running = 20
}
