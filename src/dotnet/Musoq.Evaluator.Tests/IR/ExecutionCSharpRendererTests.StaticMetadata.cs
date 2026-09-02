using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using ExecutionCSharpRenderer = Musoq.Targets.CSharpClr.ExecutionCSharpRenderer;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    [TestMethod]
    public void Convert_WhenInCheckHasSmallLiteralSet_ShouldUseConstantArrayMetadata()
    {
        var expression = new InCheck(
            new Literal("Dog", typeof(string)),
            [
                new Literal("Dog", typeof(string)),
                new Literal("Cat", typeof(string)),
                new Literal("Bird", typeof(string))
            ],
            typeof(bool));

        var converted = (ExecutionInCheck)ExecutionExpressionConverter.Convert(expression);

        Assert.IsNotNull(converted.ConstantSet);
        Assert.AreEqual(ExecutionConstantInSetKind.Array, converted.ConstantSet.Kind);
        Assert.AreEqual(typeof(string), converted.ConstantSet.ElementType.ResolveClrType());
        Assert.HasCount(3, converted.ConstantSet.Values);
    }

    [TestMethod]
    public void Convert_WhenInCheckHasTwentyValueStringLiteralSet_ShouldUseConstantSwitchMetadata()
    {
        var expression = new InCheck(
            new ColumnRef("p", "Name", typeof(string)),
            Enumerable.Range(0, 20)
                .Select(index => new Literal(index.ToString(), typeof(string)))
                .Cast<IrExpression>()
                .ToArray(),
            typeof(bool));

        var converted = (ExecutionInCheck)ExecutionExpressionConverter.Convert(expression);

        Assert.IsNotNull(converted.ConstantSet);
        Assert.AreEqual(ExecutionConstantInSetKind.Switch, converted.ConstantSet.Kind);
        Assert.AreEqual(typeof(string), converted.ConstantSet.ElementType.ResolveClrType());
        Assert.HasCount(20, converted.ConstantSet.Values);
    }

    [TestMethod]
    public void Convert_WhenInCheckHasVeryLargeLiteralSet_ShouldUseConstantFrozenSetMetadata()
    {
        var expression = new InCheck(
            new Literal("A", typeof(string)),
            Enumerable.Range(0, 64)
                .Select(index => new Literal(index.ToString(), typeof(string)))
                .Cast<IrExpression>()
                .ToArray(),
            typeof(bool));

        var converted = (ExecutionInCheck)ExecutionExpressionConverter.Convert(expression);

        Assert.IsNotNull(converted.ConstantSet);
        Assert.AreEqual(ExecutionConstantInSetKind.FrozenSet, converted.ConstantSet.Kind);
        Assert.AreEqual(typeof(string), converted.ConstantSet.ElementType.ResolveClrType());
        Assert.HasCount(64, converted.ConstantSet.Values);
    }

    [TestMethod]
    public void RenderClassMembers_WhenPlanContainsSmallConstantInCheck_ShouldEmitStaticArrayField()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreateConstantInCheckPlan("Q_InCheckSmall", 3));
        var code = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.Contains("private static readonly string[] __inSet_Q_InCheckSmall_0", code);
        Assert.Contains("new string[]", code);
        Assert.IsFalse(code.Contains("HashSet", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderClassMembers_WhenRendererIsReused_ShouldNotLeakConstantInSetFields()
    {
        var renderer = new ExecutionCSharpRenderer();
        _ = renderer.RenderClassMembers(CreateConstantInCheckPlan("Q_InCheckFirst", 3));

        var members = renderer.RenderClassMembers(CreatePlan());
        var code = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.IsFalse(code.Contains("__inSet_Q_InCheckFirst_0", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("new string[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsSmallConstantInCheck_ShouldReadStaticArrayField()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateConstantInCheckPlan("Q_InCheckSmall", 3), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("Array.IndexOf(__inSet_Q_InCheckSmall_0, p.Name) >= 0", code);
        Assert.IsFalse(code.Contains("new string[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderClassMembers_WhenPlanContainsTwentyValueConstantInCheck_ShouldAvoidLookupField()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreateConstantInCheckPlan("Q_InCheckLarge", 20));
        var code = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.IsFalse(code.Contains("__inSet_Q_InCheckLarge_0", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("HashSet", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsTwentyValueConstantInCheck_ShouldRenderSwitchExpression()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateConstantInCheckPlan("Q_InCheckLarge", 20), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("p.Name switch", code);
        Assert.Contains("\"A\" or \"B\"", code);
        Assert.IsFalse(code.Contains("__inSet_Q_InCheckLarge_0", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("new string[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderClassMembers_WhenPlanContainsVeryLargeConstantInCheck_ShouldEmitStaticFrozenSetField()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreateConstantInCheckPlan("Q_InCheckVeryLarge", 64));
        var code = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.Contains("FrozenSet<string> __inSet_Q_InCheckVeryLarge_0", code);
        Assert.Contains(".ToFrozenSet()", code);
        Assert.IsFalse(code.Contains("HashSet", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsVeryLargeConstantInCheck_ShouldReadStaticFrozenSetField()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateConstantInCheckPlan("Q_InCheckVeryLarge", 64), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("__inSet_Q_InCheckVeryLarge_0.Contains(p.Name)", code);
        Assert.IsFalse(code.Contains("new string[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderClassMembers_WhenPlanContainsStaticMetadata_ShouldEmitColumnFields()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreateConstantInCheckPlan("Q_Metadata", 3));
        var code = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.Contains("private static readonly IReadOnlyCollection<ISchemaColumn> __schemaColumns_Q_Metadata_p_0", code);
        Assert.Contains("Array.AsReadOnly(new ISchemaColumn[]", code);
        Assert.Contains("private static readonly Column[] __columns_Q_Metadata_result_1", code);
        Assert.Contains("new Column[]", code);
    }

    [TestMethod]
    public void RenderClassMembers_WhenStaticMetadataReferenceIsReusedWithDifferentShape_ShouldEmitDistinctFields()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreateSameReferenceDifferentMetadataPlan());
        var code = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.AreEqual(2, code.Split("private static readonly Column[]").Length - 1);
        Assert.Contains("private static readonly Column[] __columns_Q_MetadataCollision_shared_0", code);
        Assert.Contains("private static readonly Column[] __columns_Q_MetadataCollision_shared_1", code);
        Assert.Contains("new Column(\"Id\", typeof(int), 0)", code);
        Assert.Contains("new Column(\"Name\", typeof(string), 0)", code);
    }

    [TestMethod]
    public void RenderClassMembers_WhenMetadataDiffersOnlyByEnumIdentity_ShouldEmitPortableDistinctFields()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreateSameReferenceDifferentEnumMetadataPlan());
        var code = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.AreEqual(2, code.Split("private static readonly Column[]").Length - 1);
        Assert.Contains("__columns_Q_EnumMetadataCollision_shared_0", code);
        Assert.Contains("__columns_Q_EnumMetadataCollision_shared_1", code);
        Assert.Contains("new global::Musoq.Schema.EnumTypeDescriptor(\"JobStatus\"", code);
        Assert.Contains("new global::Musoq.Schema.EnumTypeDescriptor(\"Priority\"", code);
        Assert.Contains("global::Musoq.Schema.EnumTypeOrigin.QueryLocal", code);
        Assert.Contains("global::Musoq.Schema.EnumUnderlyingKind.Int16", code);
        Assert.Contains("new global::Musoq.Schema.EnumMemberDescriptor(\"Queued\"", code);
        Assert.Contains("global::Musoq.Schema.EnumScalarValue.FromRaw", code);
        Assert.IsFalse(code.Contains("System.Enum", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("Enum.Parse", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("Enum.ToObject", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderClassMembers_WhenSourceMetadataDiffersOnlyByModifiers_ShouldEmitDistinctFields()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreateSameReferenceDifferentSourceModifierMetadataPlan());
        var code = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.AreEqual(2, code.Split("private static readonly IReadOnlyCollection<ISchemaColumn>").Length - 1);
        Assert.Contains("__schemaColumns_Q_SourceModifierMetadataCollision_shared_0", code);
        Assert.Contains("__schemaColumns_Q_SourceModifierMetadataCollision_shared_1", code);
        Assert.Contains("global::Musoq.Schema.DataSources.SchemaColumn", code);
        Assert.Contains("\"utf-8\"", code);
        Assert.Contains("\"windows-1250\"", code);
    }

    [TestMethod]
    public void RenderMethod_WhenPlanContainsStaticMetadata_ShouldReadColumnFields()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateConstantInCheckPlan("Q_Metadata", 3), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("__schemaColumns_Q_Metadata_p_0", code);
        Assert.Contains("new Table(\"result\", __columns_Q_Metadata_result_1)", code);
        Assert.IsFalse(code.Contains("new Column[]", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("new ISchemaColumn[]", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenRendererIsReused_ShouldNotLeakStaticMetadataNames()
    {
        var renderer = new ExecutionCSharpRenderer();
        _ = renderer.RenderMethod(CreateConstantInCheckPlan("Q_MetadataFirst", 3), "ExecutePlan");

        var method = renderer.RenderMethod(CreatePlan(), "ExecutePlain");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.IsFalse(code.Contains("Q_MetadataFirst", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("__schemaColumns_Q_MetadataFirst", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("__columns_Q_MetadataFirst", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderClassMembers_WhenPostOperationHasStaticMetadata_ShouldEmitColumnFields()
    {
        var renderer = new ExecutionCSharpRenderer();
        var members = renderer.RenderClassMembers(CreatePostOperationMetadataPlan());
        var code = string.Join(Environment.NewLine, members.Select(member => member.NormalizeWhitespace().ToFullString()));

        Assert.AreEqual(1, code.Split("private static readonly Column[]").Length - 1);
        Assert.Contains("private static readonly Column[] __columns_Q_PostOps_result_0", code);
        Assert.IsFalse(code.Contains("__columns_Q_PostOps_resultSorted_1", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("__columns_Q_PostOps_resultSortedSkipped_2", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("__columns_Q_PostOps_resultSortedSkippedTaken_3", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenPostOperationHasStaticMetadata_ShouldAvoidColumnArrayCopies()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreatePostOperationMetadataPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var resultSorted = new Table(\"resultSorted\", __columns_Q_PostOps_result_0);", code);
        Assert.Contains("var resultSortedSkipped = new Table(\"resultSortedSkipped\", __columns_Q_PostOps_result_0);", code);
        Assert.Contains("var resultSortedSkippedTaken = new Table(\"resultSortedSkippedTaken\", __columns_Q_PostOps_result_0);", code);
        Assert.IsFalse(code.Contains(".Columns.ToArray()", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenTopNHasStaticMetadata_ShouldTakeOrderedRowsIntoOneTable()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateTopNMetadataPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains(
            "var resultTopNRows = EvaluationHelper.CastGeneratedRows<ResultRow0>(result.Rows).OrderBy((row) => row, ResultRow0OrderBy_0AComparer.Instance).Take(2);",
            code);
        Assert.Contains(".Take(2)", code);
        Assert.Contains("var resultTopN = new Table(\"resultTopN\", __columns_Q_TopN_result_0);", code);
        Assert.IsFalse(code.Contains("resultSorted", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains(".Columns.ToArray()", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenTopOffsetHasStaticMetadata_ShouldSelectBoundedRowsIntoOneTable()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateTopOffsetMetadataPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var resultTopOffsetSourceRows = new List<ResultRow0>(result.Rows.Count);", code);
        Assert.Contains("EvaluationHelper.SelectTopOffsetRecords(resultTopOffsetSourceRows, 1, 2, ResultRow0OrderBy_0AComparer.Instance)", code);
        Assert.Contains("var resultTopOffset = new Table(\"resultTopOffset\", __columns_Q_TopOffset_result_0);", code);
        Assert.Contains("resultTopOffset.EnsureCapacity(Math.Min(Math.Max(result.Count - 1, 0), 2));", code);
        Assert.Contains("foreach (var copiedRow in resultTopOffsetRows)", code);
        Assert.IsFalse(code.Contains("RowOrderKey", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("AppendTopOffsetRowsDirect", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("resultSorted", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains(".Columns.ToArray()", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RenderMethod_WhenSliceHasStaticMetadata_ShouldSkipAndTakeRowsIntoOneTable()
    {
        var renderer = new ExecutionCSharpRenderer();
        var method = renderer.RenderMethod(CreateSliceMetadataPlan(), "ExecutePlan");
        var code = method.NormalizeWhitespace().ToFullString();

        Assert.Contains("var resultSlicedRows = result.Rows.Skip(1).Take(2);", code);
        Assert.Contains("var resultSliced = new Table(\"resultSliced\", __columns_Q_Slice_result_0);", code);
        Assert.Contains("resultSliced.EnsureCapacity(Math.Min(Math.Max(result.Count - 1, 0), 2));", code);
        Assert.IsFalse(code.Contains("resultSkipped", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains(".Columns.ToArray()", StringComparison.Ordinal));
    }
}
