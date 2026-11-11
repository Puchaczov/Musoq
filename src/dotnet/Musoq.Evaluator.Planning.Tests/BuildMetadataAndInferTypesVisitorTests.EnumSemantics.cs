using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public partial class BuildMetadataAndInferTypesVisitorTests
{
    private const string EnumSource =
        "enum JobStatus : int { Queued = 10, Running = 20, Finished = 30 };"
        + "flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui };"
        + "enum OtherStatus : int { Running = 20 };"
        + "table Jobs { Status: JobStatus, Access: FileAccess, Other: OtherStatus };"
        + "couple #capture.any with table Jobs as Jobs;";

    [TestMethod]
    public void EnumHelpers_ShouldBindCompilerIntrinsicMarkersAndCompiledMask()
    {
        var visitor = Analyze(
            EnumSource
                + "select EnumValue(Status), EnumName(Status), IsDefined(Status), "
                + "HasAnyFlags(Access, 'Read'), HasAllFlags(Access, 'Read', 'Write'), "
                + "HasAnyFlags(Access), HasAllFlags(Access) from Jobs()",
            new CaptureMetadataContextSchemaProvider(_ => { })
        );
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
                "HasAllFlagsNullable",
            },
            collector.Methods.Select(static method => method.Method!.Name).ToArray()
        );
        Assert.IsTrue(
            collector.Methods.All(static method => method.Method!.DeclaringType!.Name == "EnumIntrinsicMarkers")
        );
    }

    [TestMethod]
    public void EnumHelpers_WhenUnknownMember_ShouldRejectExactName()
    {
        var exception = Assert.Throws<VisitorException>(() =>
            Analyze(
                EnumSource + "select HasAnyFlags(Access, 'read') from Jobs()",
                new CaptureMetadataContextSchemaProvider(_ => { })
            )
        );

        Assert.AreEqual(DiagnosticCode.MQ3108_UnknownEnumMember, exception.Code);
    }

    [TestMethod]
    public void EnumHelpers_WhenFlagsHelperUsesOrdinaryEnum_ShouldReject()
    {
        var exception = Assert.Throws<VisitorException>(() =>
            Analyze(
                EnumSource + "select HasAnyFlags(Status, 'Running') from Jobs()",
                new CaptureMetadataContextSchemaProvider(_ => { })
            )
        );

        Assert.AreEqual(DiagnosticCode.MQ3111_InvalidEnumHelper, exception.Code);
    }

    [TestMethod]
    public void EnumComparison_IsDistinctFrom_ShouldSupportContextualMemberAndNull()
    {
        var contexts = new List<SourceMetadataContext>();
        var visitor = Analyze(
            EnumSource
                + "select Status from Jobs() where Status is distinct from 'Running' or Status is not distinct from null",
            new CaptureMetadataContextSchemaProvider(contexts.Add)
        );
        var collector = CollectEnumSemantics(visitor);

        Assert.HasCount(2, collector.Comparisons);
        var member = collector.Comparisons.Single(node => !node.IsNegated);
        Assert.AreEqual(20, Assert.IsInstanceOfType<IntegerNode>(member.Right).ObjValue);
        Assert.IsInstanceOfType<NullNode>(collector.Comparisons.Single(node => node.IsNegated).Right);
        AssertJobStatusColumn(contexts.SelectMany(context => context.AllColumns), "Status");
    }

    [TestMethod]
    public void EnumCase_WhenBranchesUseSameIdentityAndContextualMember_ShouldPreserveIdentity()
    {
        var visitor = Analyze(
            EnumSource + "select Status from Jobs() where case when true then Status else 'Queued' end = 'Running'",
            new CaptureMetadataContextSchemaProvider(_ => { })
        );
        var expression = CollectEnumSemantics(visitor).Cases.Single();
        Assert.AreEqual(typeof(int), Nullable.GetUnderlyingType(expression.ReturnType) ?? expression.ReturnType);
        var otherwise = Assert.IsInstanceOfType<ElseNode>(expression.Else);
        Assert.AreEqual(10, Assert.IsInstanceOfType<IntegerNode>(otherwise.Expression).ObjValue);

        var exported = Analyze(
            EnumSource
                + "with selected as (select case when true then Status else 'Queued' end as State from Jobs()) "
                + "select State from selected where State = 'Running'",
            new CaptureMetadataContextSchemaProvider(_ => { })
        );
        AssertJobStatusColumn(exported.InferredColumnsByAlias.Values.SelectMany(columns => columns), "State");
    }

    [TestMethod]
    public void EnumCase_WhenBranchesUseDifferentIdentities_ShouldReject()
    {
        var exception = Assert.Throws<VisitorException>(() =>
            Analyze(
                EnumSource + "select case when true then Status else Other end from Jobs()",
                new CaptureMetadataContextSchemaProvider(_ => { })
            )
        );

        Assert.AreEqual(DiagnosticCode.MQ3109_EnumIdentityMismatch, exception.Code);
    }

    [TestMethod]
    public void EnumSetOperation_WhenIdentitiesMatch_ShouldBind()
    {
        Analyze(
            EnumSource + "select Status from Jobs() union select Status from Jobs()",
            new CaptureMetadataContextSchemaProvider(_ => { })
        );
        var exported = Analyze(
            EnumSource
                + "with selected as (select Status from Jobs() union select Status from Jobs()) select Status from selected",
            new CaptureMetadataContextSchemaProvider(_ => { })
        );
        AssertJobStatusColumn(exported.InferredColumnsByAlias.Values.SelectMany(columns => columns), "Status");
    }

    [TestMethod]
    public void EnumSetOperation_WhenIdentitiesDiffer_ShouldReject()
    {
        var exception = Assert.Throws<VisitorException>(() =>
            Analyze(
                EnumSource + "select Status from Jobs() union select Other from Jobs()",
                new CaptureMetadataContextSchemaProvider(_ => { })
            )
        );

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
        var exception = Assert.Throws<VisitorException>(() =>
            Analyze(EnumSource + query, new CaptureMetadataContextSchemaProvider(_ => { }))
        );

        Assert.AreEqual(DiagnosticCode.MQ3110_UnsupportedEnumOperator, exception.Code);
    }

    [TestMethod]
    public void EnumWindowPartition_ShouldBindWhenOrderingUsesEnumValue()
    {
        var contexts = new List<SourceMetadataContext>();
        var visitor = Analyze(
            EnumSource + "select RowNumber() over (partition by Status order by EnumValue(Access)) from Jobs()",
            new CaptureMetadataContextSchemaProvider(contexts.Add)
        );
        var window = CollectEnumSemantics(visitor).Windows.Single();
        Assert.IsNotNull(window.WindowSpecification);
        Assert.HasCount(1, window.WindowSpecification.PartitionFields);
        Assert.HasCount(1, window.WindowSpecification.OrderByFields);
        Assert.AreEqual(
            "Status",
            Assert.IsInstanceOfType<AccessColumnNode>(window.WindowSpecification.PartitionFields[0].Expression).Name
        );
        var order = Assert.IsInstanceOfType<AccessMethodNode>(window.WindowSpecification.OrderByFields[0].Expression);
        Assert.AreEqual("EnumValueNullable", order.Method!.Name);
        AssertJobStatusColumn(contexts.SelectMany(context => context.AllColumns), "Status");
    }

    private static void AssertJobStatusColumn(IEnumerable<ISchemaColumn> metadata, string name)
    {
        var columns = metadata.Where(column => column.ColumnName == name).ToArray();
        Assert.IsNotEmpty(columns, $"Expected an identity-bearing column named {name}.");
        foreach (var column in columns)
        {
            Assert.AreEqual(typeof(int), Nullable.GetUnderlyingType(column.ColumnType) ?? column.ColumnType);
            Assert.IsNotNull(column.EnumType);
            Assert.AreEqual("JobStatus", column.EnumType.DisplayName);
            Assert.AreEqual(EnumTypeOrigin.QueryLocal, column.EnumType.Origin);
            Assert.AreEqual(EnumUnderlyingKind.Int32, column.EnumType.UnderlyingKind);
            Assert.IsFalse(column.EnumType.IsFlags);
            CollectionAssert.AreEqual(
                new[] { "Queued", "Running", "Finished" },
                column.EnumType.Members.Select(member => member.Name).ToArray()
            );
            CollectionAssert.AreEqual(
                new[] { 10, 20, 30 },
                column.EnumType.Members.Select(member => member.Value.AsInt32()).ToArray()
            );
        }
    }

    private static EnumSemanticCollector CollectEnumSemantics(BuildMetadataAndInferTypesVisitor visitor)
    {
        var collector = new EnumSemanticCollector();
        visitor.Root.Accept(new EnumSemanticTraverser(collector));
        return collector;
    }

    private sealed class EnumSemanticCollector : NoOpExpressionVisitor
    {
        public List<IsDistinctFromNode> Comparisons { get; } = [];
        public List<CaseNode> Cases { get; } = [];
        public List<WindowFunctionNode> Windows { get; } = [];

        public override void Visit(IsDistinctFromNode node) => Comparisons.Add(node);

        public override void Visit(CaseNode node) => Cases.Add(node);

        public override void Visit(WindowFunctionNode node) => Windows.Add(node);
    }

    private sealed class EnumSemanticTraverser(EnumSemanticCollector visitor)
        : RawTraverseVisitor<EnumSemanticCollector>(visitor);

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
