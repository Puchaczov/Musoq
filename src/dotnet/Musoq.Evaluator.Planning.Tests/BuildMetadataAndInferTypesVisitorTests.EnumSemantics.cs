using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

public partial class BuildMetadataAndInferTypesVisitorTests
{
    private const string EnumSource =
        "enum JobStatus : int { Queued = 10, Running = 20, Finished = 30 };" +
        "flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui };" +
        "enum OtherStatus : int { Running = 20 };" +
        "table Jobs { Status: JobStatus, Access: FileAccess, Other: OtherStatus };" +
        "couple #capture.any with table Jobs as Jobs;";

    [TestMethod]
    public void EnumHelpers_ShouldBindCompilerIntrinsicMarkersAndCompiledMask()
    {
        var visitor = Analyze(
            EnumSource +
            "select EnumValue(Status), EnumName(Status), IsDefined(Status), " +
            "HasAnyFlags(Access, 'Read'), HasAllFlags(Access, 'Read', 'Write'), " +
            "HasAnyFlags(Access), HasAllFlags(Access) from Jobs()",
            new CaptureMetadataContextSchemaProvider(_ => { }));
        var collector = new EnumMethodCollector();

        visitor.Root.Accept(new EnumMethodTraverser(collector));

        CollectionAssert.AreEquivalent(
            new[]
            {
                "EnumValueNullable",
                "EnumNameNullable",
                "IsDefinedNullable",
                "HasAnyFlagsNullable",
                "HasAllFlagsNullable",
                "HasAnyFlagsNullable",
                "HasAllFlagsNullable"
            },
            collector.Methods.Select(static method => method.Method!.Name).ToArray());
        Assert.IsTrue(collector.Methods.All(static method =>
            method.Method!.DeclaringType!.Name == "EnumIntrinsicMarkers"));
    }

    [TestMethod]
    public void EnumHelpers_WhenUnknownMember_ShouldRejectExactName()
    {
        var exception = Assert.Throws<VisitorException>(() => Analyze(
            EnumSource + "select HasAnyFlags(Access, 'read') from Jobs()",
            new CaptureMetadataContextSchemaProvider(_ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3108_UnknownEnumMember, exception.Code);
    }

    [TestMethod]
    public void EnumHelpers_WhenFlagsHelperUsesOrdinaryEnum_ShouldReject()
    {
        var exception = Assert.Throws<VisitorException>(() => Analyze(
            EnumSource + "select HasAnyFlags(Status, 'Running') from Jobs()",
            new CaptureMetadataContextSchemaProvider(_ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3111_InvalidEnumHelper, exception.Code);
    }

    [TestMethod]
    public void EnumComparison_IsDistinctFrom_ShouldSupportContextualMemberAndNull()
    {
        Analyze(
            EnumSource +
            "select Status from Jobs() where Status is distinct from 'Running' or Status is not distinct from null",
            new CaptureMetadataContextSchemaProvider(_ => { }));
    }

    [TestMethod]
    public void EnumCase_WhenBranchesUseSameIdentityAndContextualMember_ShouldPreserveIdentity()
    {
        Analyze(
            EnumSource +
            "select Status from Jobs() where case when true then Status else 'Queued' end = 'Running'",
            new CaptureMetadataContextSchemaProvider(_ => { }));
    }

    [TestMethod]
    public void EnumCase_WhenBranchesUseDifferentIdentities_ShouldReject()
    {
        var exception = Assert.Throws<VisitorException>(() => Analyze(
            EnumSource + "select case when true then Status else Other end from Jobs()",
            new CaptureMetadataContextSchemaProvider(_ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3109_EnumIdentityMismatch, exception.Code);
    }

    [TestMethod]
    public void EnumSetOperation_WhenIdentitiesMatch_ShouldBind()
    {
        Analyze(
            EnumSource + "select Status from Jobs() union select Status from Jobs()",
            new CaptureMetadataContextSchemaProvider(_ => { }));
    }

    [TestMethod]
    public void EnumSetOperation_WhenIdentitiesDiffer_ShouldReject()
    {
        var exception = Assert.Throws<VisitorException>(() => Analyze(
            EnumSource + "select Status from Jobs() union select Other from Jobs()",
            new CaptureMetadataContextSchemaProvider(_ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3109_EnumIdentityMismatch, exception.Code);
    }

    [TestMethod]
    [DataRow("select Status from Jobs() where Status > 'Running'")]
    [DataRow("select Status from Jobs() where Status between 'Queued' and 'Finished'")]
    [DataRow("select Status from Jobs() where Status like 'R%'")]
    [DataRow("select Status::int from Jobs()")]
    [DataRow("select Status from Jobs() order by Status")]
    [DataRow("select JobStatus.Running from Jobs()")]
    [DataRow("select RowNumber() over (order by Status) from Jobs()")]
    [DataRow("select Lag(Status) over (order by EnumValue(Status)) from Jobs()")]
    public void EnumUnsupportedOperatorMatrix_ShouldReportStableDiagnostic(string query)
    {
        var exception = Assert.Throws<VisitorException>(() => Analyze(
            EnumSource + query,
            new CaptureMetadataContextSchemaProvider(_ => { })));

        Assert.AreEqual(DiagnosticCode.MQ3110_UnsupportedEnumOperator, exception.Code);
    }

    [TestMethod]
    public void EnumWindowPartition_ShouldBindWhenOrderingUsesEnumValue()
    {
        Analyze(
            EnumSource +
            "select RowNumber() over (partition by Status order by EnumValue(Access)) from Jobs()",
            new CaptureMetadataContextSchemaProvider(_ => { }));
    }

    private sealed class EnumMethodCollector : NoOpExpressionVisitor
    {
        public List<AccessMethodNode> Methods { get; } = [];

        public override void Visit(AccessMethodNode node)
        {
            if (node.Method?.DeclaringType?.Name == "EnumIntrinsicMarkers")
                Methods.Add(node);
        }
    }

    private sealed class EnumMethodTraverser(EnumMethodCollector visitor)
        : RawTraverseVisitor<EnumMethodCollector>(visitor);
}
