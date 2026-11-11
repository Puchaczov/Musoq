using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests;

public sealed partial class RuntimeV2MaintainabilityBudgetTests
{
    private const int RuntimeV2FileLineBudget = 900;
    private static readonly Dictionary<string, string> OversizedRuntimeV2FileJustifications = new(
        StringComparer.Ordinal
    );
    private static readonly IReadOnlyDictionary<string, int> ProductionHotspotFileBudgets = new Dictionary<string, int>(
        StringComparer.Ordinal
    )
    {
        // Source spans are retained while cloning the semantic tree so diagnostics
        // continue to point at the authoring site after normalization.
        ["src/dotnet/Musoq.Evaluator/Visitors/CloneQueryVisitor.cs"] = 157,
        ["src/dotnet/Musoq.Evaluator/Visitors/CloneQueryVisitor.LiteralsAndAccess.cs"] = 183,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.AggregateHelpers.cs"] = 217,
        ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitorUtilities.cs"] = 192,
        ["src/dotnet/Musoq.Evaluator/Visitors/RawTraverseVisitor.cs"] = 159,
        ["src/dotnet/Musoq.Evaluator/Visitors/RawTraverseVisitor.InterpretationSchemas.cs"] = 129,
        ["src/dotnet/Musoq.Evaluator/Visitors/RawTraverseVisitor.QuerySources.cs"] = 154,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.Aggregates.cs"] = 45,
        ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.Methods.cs"] = 47,
        ["src/dotnet/Musoq.Converter/InstanceCreator.cs"] = 330,
        ["src/dotnet/Musoq.Evaluator/Visitors/InterpreterCodeGenerator.BinarySchema.cs"] = 222,
        ["src/dotnet/Musoq.Parser/Diagnostics/ErrorMetadataCatalog.Semantic.cs"] = 21,
        ["src/dotnet/Musoq.Parser/Diagnostics/DiagnosticCode.cs"] = 216,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.cs"] = 152,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionExpressionConverter.cs"] = 105,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionNode.cs"] = 3,
        ["src/dotnet/Musoq.Evaluator/IR/Logical/LogicalPlanBuilder.Aggregates.cs"] = 102,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.DistinctAggregates.cs"] = 11,
        // T1 diagnostics add explicit operand classification and signed
        // numeric handling to this parser partial.
        ["src/dotnet/Musoq.Parser/Parser.Expressions.cs"] = 147,
        ["src/dotnet/Musoq.Evaluator/Visitors/RewriteQueryVisitor.Query.cs"] = 88,
        ["src/dotnet/Musoq.Evaluator/Visitors/RewriteQueryVisitor.Query.Context.cs"] = 61,
        ["src/dotnet/Musoq.Evaluator/Visitors/RewriteQueryVisitor.Query.GroupedSplit.cs"] = 142,
        ["src/dotnet/Musoq.Evaluator/Visitors/RewriteQueryVisitor.Query.JoinSplit.cs"] = 365,
        ["src/dotnet/Musoq.Evaluator/Visitors/RewriteQueryVisitor.Query.ResultSplit.cs"] = 77,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/PredicatePlacementPlanner.cs"] = 131,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/PredicatePlacementPlanner.AliasDiscovery.cs"] = 193,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/PredicatePlacementPlanner.FilterClassification.cs"] = 163,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/PredicatePlacementPlanner.Formatting.cs"] = 64,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/PredicatePlacementPlanner.JoinClassification.cs"] = 116,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Generic.cs"] = 161,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Generic.Coalesce.cs"] = 135,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Generic.Conditionals.cs"] = 104,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Generic.ElementSelection.cs"] = 182,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Generic.Regex.cs"] = 32,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Math.cs"] = 138,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Math.Clamp.cs"] = 77,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Math.Powers.cs"] = 68,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Math.RandomAndRank.cs"] = 61,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Math.RangePredicates.cs"] = 111,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Math.TrigLog.cs"] = 155,
        ["src/dotnet/Musoq.Parser/NoOpExpressionVisitor.cs"] = 217,
        ["src/dotnet/Musoq.Parser/NoOpExpressionVisitor.ExpressionExtras.cs"] = 59,
        ["src/dotnet/Musoq.Parser/NoOpExpressionVisitor.InterpretationAndWindows.cs"] = 127,
        ["src/dotnet/Musoq.Parser/NoOpExpressionVisitor.QueryAndSources.cs"] = 141,
        ["src/dotnet/Musoq.Parser/NoOpExpressionVisitor.SetOperatorsAndCtes.cs"] = 81,
        ["src/dotnet/Musoq.Evaluator/Visitors/RewriteQueryTraverseVisitor.cs"] = 217,
        ["src/dotnet/Musoq.Evaluator/Visitors/RewriteQueryTraverseVisitor.InterpretationSchemas.cs"] = 230,
        ["src/dotnet/Musoq.Evaluator/Visitors/RewriteQueryTraverseVisitor.QuerySetAndCte.cs"] = 91,
        ["src/dotnet/Musoq.Evaluator/Visitors/RewriteQueryTraverseVisitor.Windows.cs"] = 33,
        ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesTraverseVisitor.cs"] = 119,
        ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesTraverseVisitor.InterpretationSchemas.cs"] =
            107,
        ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesTraverseVisitor.QuerySetAndCte.cs"] = 111,
        ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesTraverseVisitor.Sources.cs"] = 244,
        ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesTraverseVisitor.Windows.cs"] = 36,
        ["src/dotnet/Musoq.Parser/Parser.QueryClauses.cs"] = 189,
        ["src/dotnet/Musoq.Parser/Parser.QueryClauses.OrderingAndWhere.cs"] = 71,
        ["src/dotnet/Musoq.Parser/Parser.QueryClauses.SelectFields.cs"] = 134,
        ["src/dotnet/Musoq.Parser/Parser.QueryClauses.Windows.cs"] = 216,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/BoundaryRowShapePlanner.cs"] = 264,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/BoundaryRowShapePlanner.Helpers.cs"] = 274,
        ["src/dotnet/Musoq.Evaluator/Helpers/WindowIntOrderBuilder.cs"] = 321,
        ["src/dotnet/Musoq.Evaluator/Helpers/WindowIntOrderBuilder.Unpartitioned.cs"] = 211,
        ["src/dotnet/Musoq.Evaluator/Helpers/EvaluationHelper.TableRows.cs"] = 199,
        ["src/dotnet/Musoq.Evaluator/Helpers/EvaluationHelper.TableRows.TopOffset.cs"] = 244,
        ["src/dotnet/Musoq.Evaluator/Helpers/EvaluationHelper.TableRows.TopOffsetTypes.cs"] = 68,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Compression.cs"] = 40,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.StrictConversions.cs"] = 25,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Strings.RegexAndEncoding.cs"] = 9,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.NetworkUtils.cs"] = 21,
        ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.ExpressionOperators.cs"] = 311,
        ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.ResultShape.cs"] = 120,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/PhysicalStrategyRules.cs"] = 56,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/SourceInteractionPlanner.cs"] = 119,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/SourceBoundaryPlanner.cs"] = 51,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/PredicateMovementPlanner.cs"] = 20,
        ["src/dotnet/Musoq.Evaluator/IR/Planning/RequiredColumnUsagePlanner.cs"] = 40,
        ["src/dotnet/Musoq.Schema/Interpreters/BytesInterpreterBase.cs"] = 134,
        ["src/dotnet/Musoq.Schema/Interpreters/TextInterpreterBase.cs"] = 109,
        ["src/dotnet/Musoq.Evaluator/Visitors/InterpreterCodeGenerator.TextSchema.cs"] = 119,
        ["src/dotnet/Musoq.Evaluator/Helpers/AsOfJoinIndex.cs"] = 160,
        ["src/dotnet/Musoq.Evaluator/Helpers/AsOfJoinIndex.Builders.cs"] = 380,
        ["src/dotnet/Musoq.Evaluator/Helpers/AsOfJoinIndex.Buckets.cs"] = 260,
        ["src/dotnet/Musoq.Evaluator/Helpers/AsOfTieBreakComparerFactory.cs"] = 51,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Validation.Blocks.cs"] = 73,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.EntryPoints.cs"] = 135,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.WindowRegistrations.cs"] = 228,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.PipelineDecomposition.cs"] = 114,
        ["src/dotnet/Musoq.Converter/Build/TransformTree.cs"] = 133,
        ["src/dotnet/Musoq.Plugins/CountAllAggregateKernel.cs"] = 40,
        ["src/dotnet/Musoq.Plugins/CountNullableAggregateKernel.cs"] = 43,
        ["src/dotnet/Musoq.Plugins/CountReferenceAggregateKernel.cs"] = 43,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.AsOfJoins.cs"] = 237,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.BlocksAndHoisting.cs"] = 135,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.HashJoinHelpers.cs"] = 132,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.OuterApplyNullSubstitution.cs"] = 59,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.PostOperations.cs"] = 47,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.SingleKeyAggregates.cs"] = 103,
        ["src/dotnet/Musoq.Evaluator/IR/Physical/PhysicalPlanBuilder.cs"] = 105,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/CodeGeneration/MethodDeclarationHelper.cs"] = 179,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.OrderRecords.cs"] = 125,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Sources.cs"] = 37,
        [
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.AggregateGenerationSignatures.cs"
        ] = 108,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.AggregateInlining.cs"] = 374,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Flow.Parallel.cs"] = 380,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.JoinIndexes.cs"] = 345,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionExpressionConverter.FieldResolution.cs"] = 352,
        ["src/dotnet/Musoq.Parser/Parser.From.cs"] = 314,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.WindowPartitionHelpers.cs"] =
            102,
        ["src/dotnet/Musoq.Evaluator/IR/Expressions/AggregateRefRewriter.cs"] = 92,
        ["src/dotnet/Musoq.Evaluator/IR/Physical/PhysicalPlanPrinter.cs"] = 240,
        ["src/dotnet/Musoq.Evaluator/Visitors/SubqueryToCteRewriteVisitor.cs"] = 81,
        // Keyword/dialect enrichment is now gated to generic syntax codes
        // so specific diagnostics retain their authored metadata.
        ["src/dotnet/Musoq.Parser/Diagnostics/SyntaxDiagnosticEnhancer.cs"] = 157,
        ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.DataUtils.cs"] = 137,
        ["src/dotnet/Musoq.Parser/SchemaParser.EmbeddedExpressions.cs"] = 19,
        ["src/dotnet/Musoq.Parser/Lexing/KeywordLookup.cs"] = 248,
        ["src/dotnet/Musoq.Parser/Lexing/Lexer.IdentifiersAndKeywords.cs"] = 93,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.SingleUseHashBuildFusion.cs"] = 78,
        [
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.SingleUseHashBuildFusion.AliasDiscovery.cs"
        ] = 51,
        [
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.SingleUseHashBuildFusion.ExpressionRewrite.cs"
        ] = 249,
        [
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.SingleUseHashBuildFusion.HashBuildNodes.cs"
        ] = 171,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.SingleUseHashBuildFusion.Payload.cs"] =
            123,
        [
            "src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.SingleUseHashBuildFusion.PayloadPruning.cs"
        ] = 261,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.SingleUseHashBuildFusion.Sources.cs"] =
            115,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/SingleUseMaterializationPlanner.cs"] = 230,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/SingleUseMaterializationPlanner.Consumers.cs"] = 121,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.KeySetHelperRendering.cs"] =
            221,
        [
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.KeySetHelperRendering.Functions.cs"
        ] = 3,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.HashJoinKeyRendering.cs"] =
            123,
        [
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.HashJoinKeyRendering.KeyLocals.cs"
        ] = 177,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionPlanPrinter.Nodes.cs"] = 284,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionPlanPrinter.Nodes.Formatting.cs"] = 71,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PlanPrinterStreamNames.cs"] = 29,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ChunkedLoopSyntaxFactory.cs"] = 266,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionShapeResolver.BinarySchemas.cs"] = 198,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/BinarySchemaNestedColumns.cs"] = 88,
        ["src/dotnet/Musoq.Evaluator/Visitors/ConstantOperatorEvaluator.cs"] = 221,
        ["src/dotnet/Musoq.Evaluator/Visitors/ScriptVariableInitializerEvaluator.cs"] = 92,
        ["src/dotnet/Musoq.Evaluator/Visitors/ScriptVariableInitializerEvaluator.ArithmeticAndBitwise.cs"] = 90,
        ["src/dotnet/Musoq.Evaluator/Visitors/ScriptVariableInitializerEvaluator.Diagnostics.cs"] = 28,
        ["src/dotnet/Musoq.Evaluator/Visitors/ScriptVariableInitializerEvaluator.NumericHelpers.cs"] = 71,
        ["src/dotnet/Musoq.Evaluator/Visitors/ScriptVariableInitializerEvaluator.Predicates.cs"] = 110,
        ["src/dotnet/Musoq.Evaluator/Visitors/ScriptVariableInitializerEvaluator.References.cs"] = 50,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Validation.Nodes.cs"] = 44,
        [
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Validation.NodeCapabilities.cs"
        ] = 107,
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Syntax.Hashing.cs"] = 126,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.RawAggregatePipelines.cs"] = 152,
        ["src/dotnet/Musoq.Evaluator/Tables/Table.cs"] = 141,
        ["src/dotnet/Musoq.Evaluator/Tables/Table.PendingRows.cs"] = 213,
        ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.WindowRegistrationKeys.cs"] = 76,
    };
    private static readonly SourceFamilyBudget[] ProductionFamilyBudgets =
    [
        new("src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution", "declares:ExecutionCSharpRenderer", 461),
        new("src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution", "ExecutionCSharpRenderer.*Aggregate*.cs", 374),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionIrAnalysis*.cs", 99),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionExpressionFingerprint*.cs", 297),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionExpressionConverter*.cs", 352),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionNode*.cs", 190),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "PhysicalLoweringImplementation*.cs", 310),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "SingleUse*.cs", 261),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "CteSourceBacked*.cs", 394),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "CteSidecar*.cs", 153),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionIrRewriter*.cs", 650),
        new(
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution",
            "ExecutionCSharpRenderer.OperatorProfiling*.cs",
            280
        ),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "*Key*Rendering.cs", 221),
        new(
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution",
            "ExecutionCSharpRenderer.HashJoinKeyRendering*.cs",
            177
        ),
        new(
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution",
            "ExecutionCSharpRenderer.KeySetHelperRendering*.cs",
            221
        ),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionPlanPrinter*.cs", 284),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionShapeResolver*.cs", 198),
        new("src/dotnet/Musoq.Evaluator/IR/Execution/Lowering", "*.cs", 130),
        new("src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/Rendering", "*.cs", 75),
        new("src/dotnet/Musoq.Evaluator/IR/Logical", "LogicalPlanBuilder*.cs", 341),
        new("src/dotnet/Musoq.Evaluator/IR/Optimization", "*.cs", 492),
        new("src/dotnet/Musoq.Evaluator/IR/Optimization", "ExpressionCse*.cs", 405),
        new("src/dotnet/Musoq.Evaluator/IR/Optimization", "MethodTarget*.cs", 492),
        new("src/dotnet/Musoq.Evaluator/IR/Optimization", "CteSidecar*.cs", 117),
        new("src/dotnet/Musoq.Evaluator/IR/Optimization", "ProjectionPruning*.cs", 368),
        new("src/dotnet/Musoq.Evaluator/IR/Planning", "PredicatePlacementPlanner*.cs", 193),
        new("src/dotnet/Musoq.Evaluator/IR/Planning", "*.cs", 295),
        new("src/dotnet/Musoq.Evaluator/IR/Planning/SourcePlanning", "*.cs", 342),
        new("src/dotnet/Musoq.Evaluator/IR/Physical/SourcePlanning", "*.cs", 329),
        new("src/dotnet/Musoq.Evaluator/IR/SourcePlanning", "*.cs", 111),
        new("src/dotnet/Musoq.Evaluator/Visitors", "BuildMetadataAndInferTypesVisitor*.cs", 309),
        new("src/dotnet/Musoq.Evaluator/Visitors", "SemanticVisitorState*.cs", 128),
        new("src/dotnet/Musoq.Evaluator/Visitors", "CloneQueryVisitor*.cs", 250),
        new("src/dotnet/Musoq.Evaluator/Visitors", "InterpreterCodeGenerator*.cs", 292),
        new("src/dotnet/Musoq.Evaluator/Visitors", "RawTraverseVisitor*.cs", 165),
        new("src/dotnet/Musoq.Evaluator/Visitors", "ScriptVariableInitializerEvaluator*.cs", 110),
        new("src/dotnet/Musoq.Evaluator/Visitors", "Semantic*.cs", 127),
        new("src/dotnet/Musoq.Evaluator/Visitors", "RewriteQueryVisitor*.cs", 365),
        new("src/dotnet/Musoq.Evaluator/Visitors", "SubqueryToCteRewriteVisitor*.cs", 266),
        new("src/dotnet/Musoq.Evaluator/Visitors/Helpers/Subqueries", "*.cs", 207),
        new("src/dotnet/Musoq.Converter/Build", "*.cs", 301),
        new("src/dotnet/Musoq.Plugins/Lib", "LibraryBase*.cs", 298),
        new("src/dotnet/Musoq.Plugins", "AggregateKernels*.cs", 130),
        new("src/dotnet/Musoq.Parser", "Parser*.cs", 316),
        new("src/dotnet/Musoq.Parser", "SchemaParser*.cs", 183),
        new("src/dotnet/Musoq.Parser/Traversal", "*.cs", 136),
        new("src/dotnet/Musoq.Parser/Lexing", "*.cs", 306),
        new("src/dotnet/Musoq.Evaluator/Helpers", "WindowFunctionHelpers*.cs", 286),
        new("src/dotnet/Musoq.Evaluator/Helpers", "WindowIntOrderBuilder*.cs", 321),
        new("src/dotnet/Musoq.Evaluator/Helpers", "EvaluationHelper*.cs", 559),
        new("src/dotnet/Musoq.Evaluator/Helpers", "AsOfJoinIndex*.cs", 380),
        new("src/dotnet/Musoq.Evaluator/Tables", "*.cs", 213),
        new("src/dotnet/Musoq.Evaluator/IR/Physical", "*.cs", 240),
        // Stability metadata is carried by the expression converter and column references.
        new("src/dotnet/Musoq.Evaluator/IR/Expressions", "*.cs", 310),
        new("src/dotnet/Musoq.Schema/Interpreters", "*InterpreterBase.cs", 134),
        new("src/dotnet/Musoq.Parser/Diagnostics", "ErrorMetadataCatalog*.cs", 360),
    ];

    private static readonly SourceFamilyTotalBudget[] ProductionFamilyTotalBudgets =
    [
        new("src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution", "declares:ExecutionCSharpRenderer", 28513),
        new("src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution", "ExecutionCSharpRenderer.*Aggregate*.cs", 4640),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionIrAnalysis*.cs", 246),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionExpressionFingerprint*.cs", 409),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "PhysicalLoweringImplementation*.cs", 19778),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "SingleUse*.cs", 1501),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "CteSourceBacked*.cs", 800),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "CteSidecar*.cs", 297),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionIrRewriter*.cs", 1299),
        new(
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution",
            "ExecutionCSharpRenderer.OperatorProfiling*.cs",
            406
        ),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "*Key*Rendering.cs", 388),
        new(
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution",
            "ExecutionCSharpRenderer.HashJoinKeyRendering*.cs",
            345
        ),
        new(
            "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution",
            "ExecutionCSharpRenderer.KeySetHelperRendering*.cs",
            224
        ),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionPlanPrinter*.cs", 1029),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionShapeResolver*.cs", 894),
        new("src/dotnet/Musoq.Evaluator/IR/Execution/Lowering", "*.cs", 601),
        new("src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/Rendering", "*.cs", 226),
        new("src/dotnet/Musoq.Plugins", "AggregateKernels*.cs", 747),
        new("src/dotnet/Musoq.Plugins/Lib", "LibraryBase*.cs", 14876),
        // Semantic validation now carries actionable TABLE/COUPLE, window, VALUES,
        // and source-binding diagnostics with their original spans.
        new("src/dotnet/Musoq.Evaluator/Visitors", "BuildMetadataAndInferTypesVisitor*.cs", 9888),
        new("src/dotnet/Musoq.Evaluator/Visitors", "SemanticVisitorState*.cs", 128),
        new("src/dotnet/Musoq.Evaluator/Visitors", "Semantic*.cs", 502),
        new("src/dotnet/Musoq.Evaluator/Visitors", "CloneQueryVisitor*.cs", 1085),
        new("src/dotnet/Musoq.Evaluator/Visitors", "ScriptVariableInitializerEvaluator*.cs", 441),
        new("src/dotnet/Musoq.Evaluator/Visitors", "RawTraverseVisitor*.cs", 755),
        new("src/dotnet/Musoq.Evaluator/IR/Optimization", "*.cs", 8163),
        new("src/dotnet/Musoq.Evaluator/IR/Optimization", "ExpressionCse*.cs", 882),
        new("src/dotnet/Musoq.Evaluator/IR/Optimization", "MethodTarget*.cs", 837),
        new("src/dotnet/Musoq.Evaluator/IR/Optimization", "CteSidecar*.cs", 117),
        new("src/dotnet/Musoq.Evaluator/IR/Optimization", "ProjectionPruning*.cs", 754),
        // Compile-time cancellation now checks CTE, source-transfer, and physical-planning traversals.
        new("src/dotnet/Musoq.Evaluator/IR/Planning", "*.cs", 8822),
        new("src/dotnet/Musoq.Evaluator/IR/Planning/SourcePlanning", "*.cs", 453),
        new("src/dotnet/Musoq.Evaluator/IR/Physical/SourcePlanning", "*.cs", 427),
        // The typed source-predicate capability contract intentionally adds a
        // separately owned negotiation family to this source-planning boundary.
        new("src/dotnet/Musoq.Evaluator/IR/SourcePlanning", "*.cs", 501),
        new("src/dotnet/Musoq.Parser", "Parser*.cs", 5465),
        new("src/dotnet/Musoq.Parser/Lexing", "*.cs", 2871),
        new("src/dotnet/Musoq.Evaluator/Helpers", "WindowFunctionHelpers*.cs", 2485),
        new("src/dotnet/Musoq.Evaluator/Helpers", "WindowIntOrderBuilder*.cs", 532),
        new("src/dotnet/Musoq.Evaluator/Helpers", "EvaluationHelper*.cs", 3104),
        new("src/dotnet/Musoq.Evaluator/Helpers", "AsOfJoinIndex*.cs", 850),
        new("src/dotnet/Musoq.Evaluator/Tables", "*.cs", 1221),
        new("src/dotnet/Musoq.Evaluator/Visitors", "InterpreterCodeGenerator*.cs", 2658),
        new("src/dotnet/Musoq.Evaluator/Visitors", "RewriteQueryVisitor*.cs", 2275),
        new("src/dotnet/Musoq.Evaluator/Visitors", "SubqueryToCteRewriteVisitor*.cs", 2480),
        new("src/dotnet/Musoq.Evaluator/IR/Logical", "LogicalPlanBuilder*.cs", 1878),
        new("src/dotnet/Musoq.Parser", "SchemaParser*.cs", 2068),
        // Stability metadata and resolver plumbing intentionally expand this family.
        new("src/dotnet/Musoq.Evaluator/IR/Expressions", "*.cs", 1981),
        new("src/dotnet/Musoq.Parser/Diagnostics", "ErrorMetadataCatalog*.cs", 1764),
        // Target-request and execution-IR cancellation boundaries are repository-owned build stages.
        new("src/dotnet/Musoq.Converter/Build", "*.cs", 1832),
        new("src/dotnet/Musoq.Evaluator/IR/Physical", "*.cs", 895),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionNode*.cs", 849),
        new("src/dotnet/Musoq.Evaluator/IR/Execution", "ExecutionExpressionConverter*.cs", 867),
        new("src/dotnet/Musoq.Evaluator/IR/Planning", "PredicatePlacementPlanner*.cs", 904),
        new("src/dotnet/Musoq.Parser/Traversal", "*.cs", 188),
    ];

    private static readonly Dictionary<string, int> ProductionNotImplementedExceptionBudgets = new(
        StringComparer.Ordinal
    )
    {
        // Generated placeholder bodies are covered by renderer tests and are not reachable query execution paths.
        ["src/dotnet/Musoq.Targets.CSharpClr/Rendering/CodeGeneration/CompiledQueryClassRenderer.cs"] = 1,
    };

    private const int HighCountNotSupportedExceptionThreshold = 2;

    private static readonly IReadOnlyDictionary<string, int> ProductionHighCountNotSupportedExceptionBudgets =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["src/dotnet/Musoq.Evaluator/Visitors/ScriptParameterMetadataBinder.cs"] = 5,
            ["src/dotnet/Musoq.Evaluator/IR/UnsupportedShape.cs"] = 5,
            [
                "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Expressions.CachedMethodCalls.cs"
            ] = 2,
            [
                "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Expressions.MethodInvocationArguments.cs"
            ] = 2,
            ["src/dotnet/Musoq.Evaluator/Visitors/ScriptVariableMetadataBinder.cs"] = 3,
            ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.AsOfJoins.cs"] = 2,
            ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.QueryValidation.cs"] = 3,
            ["src/dotnet/Musoq.Parser/Nodes/BinaryIntegerNode.cs"] = 3,
            ["src/dotnet/Musoq.Parser/Nodes/HexIntegerNode.cs"] = 3,
            ["src/dotnet/Musoq.Parser/Nodes/OctalIntegerNode.cs"] = 3,
            ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.ColumnAccess.DotAndCalls.cs"] = 2,
            ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.SourceBinding.Schema.cs"] = 2,
            ["src/dotnet/Musoq.Converter/ExecutionTargets/ExecutionTargetCatalog.cs"] = 3,
            ["src/dotnet/Musoq.Targets.CSharpClr/CSharpClrExecutionBackend.cs"] = 2,
            ["src/dotnet/Musoq.Targets.CSharpClr/CSharpClrReferenceAssemblyCollector.cs"] = 2,
            ["src/dotnet/Musoq.Targets.Execution.Analysis/Targeting/ExecutionTargetCompatibilityAnalyzer.cs"] = 2,
            ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Date.Extraction.cs"] = 2,
        };

    private static readonly IReadOnlyDictionary<string, string> ProductionHighCountNotSupportedExceptionRationales =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["src/dotnet/Musoq.Evaluator/Visitors/ScriptParameterMetadataBinder.cs"] =
                "Metadata binder rejects invalid script parameter declarations, defaults and undeclared references.",
            ["src/dotnet/Musoq.Evaluator/IR/UnsupportedShape.cs"] =
                "Centralized unsupported-shape exception factory for Execution IR lowering and C# rendering.",
            [
                "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Expressions.CachedMethodCalls.cs"
            ] = "Renderer guardrails for unsupported cached invocation shapes.",
            [
                "src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.Expressions.MethodInvocationArguments.cs"
            ] = "Renderer guardrails for unsupported method argument expressions.",
            ["src/dotnet/Musoq.Evaluator/Visitors/ScriptVariableMetadataBinder.cs"] =
                "Metadata binder rejects unsupported script variable declaration and initializer forms.",
            ["src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalLoweringImplementation.AsOfJoins.cs"] =
                "Execution planning rejects unsupported ASOF join shapes.",
            ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.QueryValidation.cs"] =
                "Semantic validation rejects unsupported query result shapes.",
            ["src/dotnet/Musoq.Parser/Nodes/BinaryIntegerNode.cs"] =
                "Literal conversion rejects unsupported binary integer target types.",
            ["src/dotnet/Musoq.Parser/Nodes/HexIntegerNode.cs"] =
                "Literal conversion rejects unsupported hexadecimal integer target types.",
            ["src/dotnet/Musoq.Parser/Nodes/OctalIntegerNode.cs"] =
                "Literal conversion rejects unsupported octal integer target types.",
            ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.ColumnAccess.DotAndCalls.cs"] =
                "Semantic column access rejects unsupported dot/call combinations.",
            ["src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.SourceBinding.Schema.cs"] =
                "Source binding translates provider NotSupported lookups to actionable unknown-source diagnostics when collecting compiler errors.",
            ["src/dotnet/Musoq.Converter/ExecutionTargets/ExecutionTargetCatalog.cs"] =
                "Internal target catalog rejects unregistered phases and render requests that violate target capabilities.",
            ["src/dotnet/Musoq.Targets.CSharpClr/CSharpClrExecutionBackend.cs"] =
                "C# CLR backend rejects unsupported target dispatch and unsupported execution IR render outcomes.",
            ["src/dotnet/Musoq.Targets.CSharpClr/CSharpClrReferenceAssemblyCollector.cs"] =
                "Reference discovery classifies unresolved execution-plan CLR descriptors before Roslyn compilation.",
            ["src/dotnet/Musoq.Targets.Execution.Analysis/Targeting/ExecutionTargetCompatibilityAnalyzer.cs"] =
                "Compatibility analysis safely skips CLR property-path inspection when an execution descriptor cannot resolve its CLR type.",
            ["src/dotnet/Musoq.Plugins/Lib/LibraryBase.Date.Extraction.cs"] =
                "Date extraction helpers reject unsupported date-like inputs.",
        };

    private static readonly IReadOnlyDictionary<string, int> LargeTestFixtureBudgets = new Dictionary<string, int>(
        StringComparer.Ordinal
    )
    {
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/RuntimeV2MaintainabilityBudgetTests.cs"] = 8,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/RuntimeV2MaintainabilityBudgetTests.Budgets.cs"] = 631,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/RuntimeV2MaintainabilityBudgetTests.Helpers.cs"] = 150,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/RuntimeV2MaintainabilityBudgetTests.ProductionChecks.cs"] = 206,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/RuntimeV2MaintainabilityBudgetTests.Placeholders.cs"] = 34,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/RuntimeV2MaintainabilityBudgetTests.PublicSignatures.cs"] = 288,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/RuntimeV2MaintainabilityBudgetTests.Subqueries.cs"] = 44,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/RuntimeV2MaintainabilityBudgetTests.UnsupportedPaths.cs"] = 129,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/CommonSubexpressionEliminationEdgeCaseTests.cs"] = 339,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/CommonSubexpressionEliminationEdgeCaseTests.CachingNodes.cs"] = 281,
        [
            "src/dotnet/Musoq.Evaluator.Planning.Tests/CommonSubexpressionEliminationEdgeCaseTests.CachingNodes.MixedContexts.cs"
        ] = 399,
        [
            "src/dotnet/Musoq.Evaluator.Planning.Tests/CommonSubexpressionEliminationEdgeCaseTests.CachingNodes.NullAndAllColumns.cs"
        ] = 200,
        [
            "src/dotnet/Musoq.Evaluator.Planning.Tests/CommonSubexpressionEliminationEdgeCaseTests.CaseNullAndClauses.cs"
        ] = 343,
        [
            "src/dotnet/Musoq.Evaluator.Planning.Tests/CommonSubexpressionEliminationEdgeCaseTests.CombinedAndBoundaries.cs"
        ] = 167,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/BuildMetadataAndInferTypesVisitorUtilitiesTests.cs"] = 135,
        [
            "src/dotnet/Musoq.Evaluator.Planning.Tests/BuildMetadataAndInferTypesVisitorUtilitiesTests.IndexabilityAndEnumerables.cs"
        ] = 216,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/BuildMetadataAndInferTypesVisitorUtilitiesTests.StarExpansion.cs"] =
            179,
        [
            "src/dotnet/Musoq.Evaluator.Planning.Tests/BuildMetadataAndInferTypesVisitorUtilitiesTests.ValidQueryExpressionType.cs"
        ] = 364,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/IR/LogicalNodeTests.cs"] = 221,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/IR/LogicalNodeTests.Composite.cs"] = 79,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/IR/LogicalNodeTests.Printer.cs"] = 350,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/IR/LogicalNodeTests.UnaryAndBinary.cs"] = 242,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/ValuesFromTests.cs"] = 251,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/ValuesFromTests.DiagnosticsAndHelpers.cs"] = 161,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/ValuesFromTests.JoinsWindowsAndSets.cs"] = 265,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/ValuesFromTests.NumericMatrices.cs"] = 199,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/GroupByTests.cs"] = 551,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/GroupByTests.DistinctAggregates.cs"] = 286,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/SpecExploration.CoreLanguageTests.cs"] = 153,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/SpecExploration.CoreLanguageTests.AdvancedClauses.cs"] = 671,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/SpecExploration.CoreLanguageTests.JoinsAndAggregates.cs"] = 211,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/SpecExploration.CoreLanguageTests.LiteralsAndOperators.cs"] = 199,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/SpecExploration.CoreLanguageTests.SelectWhere.cs"] = 393,
        ["src/dotnet/Musoq.Evaluator.Runtime.Tests/RowAndKeyEqualityTests.cs"] = 337,
        ["src/dotnet/Musoq.Evaluator.Interpretation.Tests/Spec/StressTestsRound2.InterpretationSchemasTests.cs"] = 284,
        [
            "src/dotnet/Musoq.Evaluator.Interpretation.Tests/Spec/StressTestsRound2.InterpretationSchemasTests.ArraysAndCrossApply.cs"
        ] = 283,
        [
            "src/dotnet/Musoq.Evaluator.Interpretation.Tests/Spec/StressTestsRound2.InterpretationSchemasTests.StringEncoding.cs"
        ] = 188,
        [
            "src/dotnet/Musoq.Evaluator.Interpretation.Tests/Spec/StressTestsRound2.InterpretationSchemasTests.TextAndIntegration.cs"
        ] = 535,
        [
            "src/dotnet/Musoq.Evaluator.Interpretation.Tests/Spec/StressTestsRound2.InterpretationSchemasTests.TypeBoundaries.cs"
        ] = 200,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/CrossApplyUnusedAliasTests.cs"] = 540,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/CrossApplyUnusedAliasTests.CteApplyJoins.cs"] = 112,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/CrossApplyUnusedAliasTests.InterleavedApplyJoinChains.cs"] = 305,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/CrossApplyUnusedAliasTests.JoinChains.cs"] = 237,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/CrossApplyUnusedAliasTests.LocalModels.cs"] = 49,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/CrossApplyUnusedAliasTests.SameSourcePropertyApplies.cs"] = 323,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/AsOfJoinTests.cs"] = 17,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/AsOfJoinTests.Diagnostics.cs"] = 474,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/AsOfJoinTests.Directional.cs"] = 393,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/AsOfJoinTests.EdgeCases.cs"] = 215,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/AsOfJoinTests.PostOperations.cs"] = 348,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/ErrorQuality.BinaryTextSchemaTests.SyntaxBoundaries.cs"] = 426,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/IR/ExpressionIRTests.cs"] = 507,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/DistinctComprehensiveTests.cs"] = 244,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/CompilationPipelineErrorTests.cs"] = 343,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/TypeRelatedMistakesTests.cs"] = 229,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/StringMethodsExtendedTests.cs"] = 551,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/SetsOperatorsTests.cs"] = 92,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/AutomaticNumericTypeInference.PathValueTests.cs"] = 334,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/AutomaticNumericTypeInference.PathValueTests.CasePromotion.cs"] = 336,
        [
            "src/dotnet/Musoq.Evaluator.Query.Tests/AutomaticNumericTypeInference.PathValueTests.ComparisonsAndOrdering.cs"
        ] = 334,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/AutomaticNumericTypeInference.PathValueTests.InvalidConversions.cs"] =
            222,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/EndToEndQueryExecutionTests.cs"] = 350,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/EndToEndQueryExecutionTests.ExpressionsOrderingAndConversion.cs"] =
            335,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/EndToEndQueryExecutionTests.JoinsAndSets.cs"] = 368,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/EndToEndQueryExecutionTests.PagingAndMultitable.cs"] = 152,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/ReorderedSyntaxCteTests.cs"] = 355,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/ReorderedSyntaxCteTests.EdgeCases.cs"] = 180,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/ReorderedSyntaxCteTests.MixedAndNested.cs"] = 325,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/ReorderedSyntaxCteTests.SetOperatorsAndJoins.cs"] = 339,
        ["src/dotnet/Musoq.Evaluator.CodeGeneration.Tests/InterpreterCodeGenTests.cs"] = 145,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/Exploratory.ComplexPatternsTests.cs"] = 414,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/Exploratory.ComplexPatternsTests.ExpressionsAndFilters.cs"] = 340,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/Exploratory.ComplexPatternsTests.GroupingAndOrdering.cs"] = 347,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/Exploratory.ComplexPatternsTests.ScalarFunctions.cs"] = 106,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/DateTimeExtendedTests.cs"] = 349,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/DateTimeExtendedTests.DateMath.cs"] = 347,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/DateTimeExtendedTests.DiffAndCalendar.cs"] = 339,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/DateTimeExtendedTests.RangesAndParsing.cs"] = 111,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/BitsOperationsBranchCoverageTests.cs"] = 194,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/HashOptionalSchemaComprehensiveTests.cs"] = 339,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/HashOptionalSchemaComprehensiveTests.DescAndCouple.cs"] = 156,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/HashOptionalSchemaComprehensiveTests.GroupingPagingAndJoins.cs"] = 328,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/HashOptionalSchemaComprehensiveTests.SetsCtesAndCases.cs"] = 337,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/StarModifierTests.cs"] = 347,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/StarModifierTests.Integration.cs"] = 337,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/StarModifierTests.ParsingIntegration.cs"] = 160,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/StarModifierTests.SyntaxErrors.cs"] = 349,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/UserMistakesTests.cs"] = 347,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/UserMistakesTests.AliasesFunctionsAndEdges.cs"] = 349,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/UserMistakesTests.SemanticMistakes.cs"] = 346,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/UserMistakesTests.ValidControls.cs"] = 118,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/InvalidQueryOutputTypeTests.cs"] = 350,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/InvalidQueryOutputTypeTests.ExpressionsAndSetOperators.cs"] = 349,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/InvalidQueryOutputTypeTests.MessagesAndEdges.cs"] = 108,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/InvalidQueryOutputTypeTests.PrimitiveClauses.cs"] = 351,
        [
            "src/dotnet/Musoq.Evaluator.Planning.Tests/Visitors/Helpers/CteDependencyGraph/CteDependencyGraphBuilderTests.cs"
        ] = 217,
        [
            "src/dotnet/Musoq.Evaluator.Planning.Tests/Visitors/Helpers/CteDependencyGraph/CteDependencyGraphBuilderTests.ComplexAndEdge.cs"
        ] = 201,
        [
            "src/dotnet/Musoq.Evaluator.Planning.Tests/Visitors/Helpers/CteDependencyGraph/CteDependencyGraphBuilderTests.ExecutionAndApply.cs"
        ] = 252,
        [
            "src/dotnet/Musoq.Evaluator.Planning.Tests/Visitors/Helpers/CteDependencyGraph/CteDependencyGraphBuilderTests.Workflow.cs"
        ] = 362,
        ["src/dotnet/Musoq.Evaluator.Runtime.Tests/ParallelismOutputRowsTests.cs"] = 367,
        ["src/dotnet/Musoq.Evaluator.Runtime.Tests/ParallelismOutputRowsTests.Aggregations.cs"] = 214,
        ["src/dotnet/Musoq.Evaluator.Runtime.Tests/ParallelismOutputRowsTests.ComplexAndStress.cs"] = 235,
        ["src/dotnet/Musoq.Evaluator.Runtime.Tests/ParallelismOutputRowsTests.JoinsAndLargeData.cs"] = 240,
        ["src/dotnet/Musoq.Evaluator.Runtime.Tests/SafeDataAccessTests.cs"] = 68,
        ["src/dotnet/Musoq.Evaluator.Runtime.Tests/SafeDataAccessTests.SafeArrayAccess.cs"] = 429,
        ["src/dotnet/Musoq.Evaluator.Runtime.Tests/SafeDataAccessTests.TablesAndRows.cs"] = 515,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/Exploratory.FunctionsAndFixesTests.cs"] = 139,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/Exploratory.FunctionsAndFixesTests.CrossApplyAndJoins.cs"] = 287,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/Exploratory.FunctionsAndFixesTests.FixVerifications.cs"] = 322,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/Exploratory.FunctionsAndFixesTests.ScalarFunctions.cs"] = 207,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/DescColumnNestedPropertyTests.cs"] = 320,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/DescColumnNestedPropertyTests.Comprehensive.cs"] = 219,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/DescColumnNestedPropertyTests.ErrorsAndEvaluation.cs"] = 212,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/DescColumnNestedPropertyTests.NavigationWorkflow.cs"] = 192,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/Strings.ExtractAndEncodingTests.cs"] = 628,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/Strings.ExtractAndEncodingTests.ExtractBetween.cs"] = 208,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/Strings.ExtractAndEncodingTests.HtmlEncoding.cs"] = 70,
        ["src/dotnet/Musoq.Plugins.Conversions.Tests/NumericOnlyTypeConverterExtendedTests.cs"] = 223,
        ["src/dotnet/Musoq.Plugins.Conversions.Tests/NumericOnlyTypeConverterExtendedTests.Decimal.cs"] = 151,
        ["src/dotnet/Musoq.Plugins.Conversions.Tests/NumericOnlyTypeConverterExtendedTests.Double.cs"] = 175,
        ["src/dotnet/Musoq.Plugins.Conversions.Tests/NumericOnlyTypeConverterExtendedTests.EdgeCases.cs"] = 144,
        ["src/dotnet/Musoq.Plugins.Conversions.Tests/NumericOnlyTypeConverterExtendedTests.Int64.cs"] = 172,
        ["src/dotnet/Musoq.Plugins.Conversions.Tests/NumericOnlyTypeConverterExtendedTests.OtherTypes.cs"] = 53,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/DataUtilsTests.cs"] = 188,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/MathExtendedTests.cs"] = 180,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/LibraryBaseUtilsTests.cs"] = 290,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/StringCultureConsistencyTests.cs"] = 251,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/NetworkUtilsExtendedTests.cs"] = 173,
        ["src/dotnet/Musoq.Plugins.Conversions.Tests/FromBytesMethodsTests.cs"] = 230,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/IR/LogicalPlanBuilderTests.cs"] = 204,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/DistinctOrderByBugTests.cs"] = 225,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/QualifyTests.cs"] = 198,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/CompilationPipelineErrorTests.Lexer.cs"] = 246,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/CompilationPipelineErrorTests.MessagesAndCodes.cs"] = 161,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/CompilationPipelineErrorTests.Semantic.cs"] = 369,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/CompilationPipelineErrorTests.Syntax.cs"] = 284,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/DistinctComprehensiveTests.CteBasics.cs"] = 267,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/DistinctComprehensiveTests.CteChains.cs"] = 234,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/DistinctComprehensiveTests.Ctes.cs"] = 439,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/DistinctComprehensiveTests.CteSetOperators.cs"] = 128,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/DistinctComprehensiveTests.JoinsAndAggregates.cs"] = 52,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/DistinctComprehensiveTests.SetOperators.cs"] = 50,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/GroupByTests.AccessorsAndJoins.cs"] = 583,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/GroupByTests.CountsAndUngroupedAggregates.cs"] = 263,
        ["src/dotnet/Musoq.Evaluator.CodeGeneration.Tests/InterpreterCodeGenTests.Binary.cs"] = 499,
        ["src/dotnet/Musoq.Evaluator.CodeGeneration.Tests/InterpreterCodeGenTests.Text.cs"] = 618,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/IR/ExpressionIRTests.Converter.cs"] = 499,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/IR/ExpressionIRTests.Extractor.cs"] = 138,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/IR/ExpressionIRTests.Printer.cs"] = 252,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/SetsOperatorsTests.ExceptIntersect.cs"] = 342,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/SetsOperatorsTests.MixedAndGroupedSources.cs"] = 48,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/SetsOperatorsTests.MixedUnionScenarios.cs"] = 234,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/SetsOperatorsTests.Union.cs"] = 592,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/TypeRelatedMistakesTests.ArithmeticComparison.cs"] = 276,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/TypeRelatedMistakesTests.FunctionsAndCasts.cs"] = 252,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/TypeRelatedMistakesTests.QueryConstructs.cs"] = 458,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/TypeRelatedMistakesTests.ValidQueries.cs"] = 150,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/StringMethodsExtendedTests.Classification.cs"] = 211,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/StringMethodsExtendedTests.Counts.cs"] = 199,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/StringMethodsExtendedTests.Regex.cs"] = 162,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/StringMethodsExtendedTests.Shape.cs"] = 271,
        ["src/dotnet/Musoq.Converter.Inspection.Tests/QueryInspectionTests.Helpers.cs"] = 73,
        ["src/dotnet/Musoq.Converter.Inspection.Tests/QueryInspectionTests.Helpers.Assertions.cs"] = 163,
        ["src/dotnet/Musoq.Converter.Inspection.Tests/QueryInspectionTests.Helpers.BinaryArraysAndRepeats.cs"] = 198,
        ["src/dotnet/Musoq.Converter.Inspection.Tests/QueryInspectionTests.Helpers.BinaryBasic.cs"] = 151,
        ["src/dotnet/Musoq.Converter.Inspection.Tests/QueryInspectionTests.Helpers.BinaryGeneric.cs"] = 68,
        ["src/dotnet/Musoq.Converter.Inspection.Tests/QueryInspectionTests.Helpers.Normalization.cs"] = 54,
        ["src/dotnet/Musoq.Converter.Inspection.Tests/QueryInspectionTests.Helpers.QueryFactories.cs"] = 116,
        ["src/dotnet/Musoq.Converter.Inspection.Tests/QueryInspectionTests.Helpers.ScanFinders.cs"] = 78,
        ["src/dotnet/Musoq.Converter.Inspection.Tests/QueryInspectionTests.Helpers.SchemaProviders.cs"] = 148,
        ["src/dotnet/Musoq.Converter.Inspection.Tests/QueryInspectionTests.ExecutionAggregates.cs"] = 602,
        ["src/dotnet/Musoq.Converter.Inspection.Tests/QueryInspectionTests.ExecutionBasics.cs"] = 543,
        ["src/dotnet/Musoq.Evaluator.Runtime.Tests/TablesTests.cs"] = 708,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/Multilingual.CrossLanguageTests.cs"] = 699,
        ["src/dotnet/Musoq.Evaluator.Diagnostics.Tests/QueryAnalyzerDiagnosticTests.cs"] = 695,
        ["src/dotnet/Musoq.Evaluator.Query.Tests/BranchCoverage.DataStructuresTests.cs"] = 691,
        ["src/dotnet/Musoq.Evaluator.Planning.Tests/Visitors/RewriteQueryVisitorTests.cs"] = 680,
        ["src/dotnet/Musoq.Evaluator.Relational.Tests/CrossApplySelfPropertyTests.cs"] = 678,
        ["src/dotnet/Musoq.Plugins.Conversions.Tests/ConversionMethodsTests.cs"] = 394,
        ["src/dotnet/Musoq.Plugins.Conversions.Tests/NumericOnlyTypeConversionTests.cs"] = 173,
        ["src/dotnet/Musoq.Plugins.Conversions.Tests/ConvertingTests.cs"] = 376,
        ["src/dotnet/Musoq.Plugins.Functions.Tests/GenericTests.cs"] = 284,
        ["src/dotnet/Musoq.Parser.Tests/LexerSchemaTests.cs"] = 261,
        ["src/dotnet/Musoq.Parser.Tests/LexerSchemaTests.ComplexTokenization.cs"] = 262,
        ["src/dotnet/Musoq.Parser.Tests/LexerSchemaTests.EncodingAndModifiers.cs"] = 148,
        ["src/dotnet/Musoq.Parser.Tests/LexerSchemaTests.TextSchemaKeywords.cs"] = 210,
        ["src/dotnet/Musoq.Parser.Tests/TokenFactoryTests.cs"] = 304,
        ["src/dotnet/Musoq.Parser.Tests/TokenFactoryTests.ComplexTokens.cs"] = 219,
        ["src/dotnet/Musoq.Parser.Tests/TokenFactoryTests.SimpleTokensTail.cs"] = 276,
        ["src/dotnet/Musoq.Parser.Tests/TokenFactoryTests.SpecialAndEdgeCases.cs"] = 64,
        ["src/dotnet/Musoq.Parser.Tests/AdditionalNodesTests.cs"] = 225,
        ["src/dotnet/Musoq.Parser.Tests/NoOpExpressionVisitorTests.cs"] = 303,
        ["src/dotnet/Musoq.Parser.Tests/NoOpExpressionVisitorTests.BitwiseAndIdentifiers.cs"] = 89,
        ["src/dotnet/Musoq.Parser.Tests/NoOpExpressionVisitorTests.AllOverloads.cs"] = 105,
        ["src/dotnet/Musoq.Parser.Tests/NoOpExpressionVisitorTests.LiteralsAndAccess.cs"] = 281,
        ["src/dotnet/Musoq.Parser.Tests/NoOpExpressionVisitorTests.QueryAndInterpretation.cs"] = 123,
        ["src/dotnet/Musoq.Parser.Tests/TextSpanLocationTests.cs"] = 627,
        ["src/dotnet/Musoq.Parser.Tests/LexerTests.cs"] = 608,
        ["src/dotnet/Musoq.Schema.Tests/BytesInterpreterBaseTests.cs"] = 198,
        ["src/dotnet/Musoq.Schema.Tests/BytesInterpreterBaseTests.AlignmentValidationAndInterface.cs"] = 205,
        ["src/dotnet/Musoq.Schema.Tests/BytesInterpreterBaseTests.BytesStringsAndBits.cs"] = 180,
        ["src/dotnet/Musoq.Schema.Tests/BytesInterpreterBaseTests.PrimitiveReads.cs"] = 278,
        ["src/dotnet/Musoq.Schema.Tests/TextInterpreterBaseTests.cs"] = 121,
        ["src/dotnet/Musoq.Schema.Tests/TextInterpreterBaseTests.CoreReads.cs"] = 287,
        ["src/dotnet/Musoq.Schema.Tests/TextInterpreterBaseTests.TokenPatternAndWhitespace.cs"] = 317,
        ["src/dotnet/Musoq.Schema.Tests/TextInterpreterBaseTests.ValidationAndInterface.cs"] = 93,
        ["src/dotnet/Musoq.Schema.Tests/SchemaExtendedTests.cs"] = 255,
    };

    private static readonly SourceFamilyBudget[] SplitTestFixtureFamilyBudgets =
    [
        new("src/dotnet/Musoq.Plugins.Functions.Tests", "NetworkUtilsExtendedTests*.cs", 248),
        new("src/dotnet/Musoq.Plugins.Functions.Tests", "DataUtilsTests*.cs", 308),
        new("src/dotnet/Musoq.Plugins.Functions.Tests", "MathExtendedTests*.cs", 232),
        new("src/dotnet/Musoq.Plugins.Functions.Tests", "LibraryBaseUtilsTests*.cs", 290),
        new("src/dotnet/Musoq.Plugins.Functions.Tests", "StringCultureConsistencyTests*.cs", 306),
        new("src/dotnet/Musoq.Plugins.Conversions.Tests", "FromBytesMethodsTests*.cs", 315),
        new("src/dotnet/Musoq.Parser.Tests", "AdditionalNodesTests*.cs", 245),
        new("src/dotnet/Musoq.Parser.Tests", "NoOpExpressionVisitorTests*.cs", 303),
        new("src/dotnet/Musoq.Schema.Tests", "SchemaExtendedTests*.cs", 255),
        new("src/dotnet/Musoq.Evaluator.Planning.Tests/IR", "LogicalPlanBuilderTests*.cs", 297),
        new("src/dotnet/Musoq.Evaluator.Relational.Tests", "DistinctOrderByBugTests*.cs", 246),
        new("src/dotnet/Musoq.Evaluator.Relational.Tests", "QualifyTests*.cs", 200),
        new("src/dotnet/Musoq.Evaluator.Planning.Tests/IR", "PhysicalToExecutionPlanBuilderTests*.cs", 263),
        new("src/dotnet/Musoq.Evaluator.Query.Tests", "BranchCoverage.ExceptionCoverageTests*.cs", 366),
        new("src/dotnet/Musoq.Evaluator.Query.Tests", "BranchCoverage.DiagnosticsTests*.cs", 687),
        new("src/dotnet/Musoq.Evaluator.Interpretation.Tests", "BinaryOrTextual.AdvancedFormatsTests*.cs", 391),
        new("src/dotnet/Musoq.Evaluator.Interpretation.Tests", "BinaryOrTextual.CoreBinaryTests*.cs", 271),
        new("src/dotnet/Musoq.Evaluator.Interpretation.Tests", "BinaryOrTextual.SchemaFeaturesTests*.cs", 511),
        new("src/dotnet/Musoq.Evaluator.Interpretation.Tests", "BinaryOrTextual.TypesAndExpressionsTests*.cs", 400),
        new("src/dotnet/Musoq.Evaluator.Planning.Tests", "ConstantFoldingTests*.cs", 492),
        new("src/dotnet/Musoq.Evaluator.Relational.Tests", "CteTests*.cs", 317),
        new("src/dotnet/Musoq.Evaluator.Diagnostics.Tests", "ErrorQuality.BinaryTextSchemaTests*.cs", 426),
        new("src/dotnet/Musoq.Evaluator.Diagnostics.Tests", "ErrorQuality.DialectConfusionTests*.cs", 481),
        new("src/dotnet/Musoq.Evaluator.Diagnostics.Tests", "ErrorQuality.StructuralSyntaxTests*.cs", 531),
        new("src/dotnet/Musoq.Evaluator.Relational.Tests", "Exploratory.CrossApplyBasicsTests*.cs", 297),
        new("src/dotnet/Musoq.Evaluator.Relational.Tests", "Exploratory.QueriesAndJoinsTests*.cs", 353),
        new("src/dotnet/Musoq.Evaluator.CodeGeneration.Tests", "GeneratedCodeSamplesCatalog*.cs", 350),
        new("src/dotnet/Musoq.Evaluator.Tests.Common", "EvaluatorSampleFixtures*.cs", 350),
        new("src/dotnet/Musoq.Evaluator.CodeGeneration.Tests", "GeneratedCodeSamplesShapeTests*.cs", 360),
        new("src/dotnet/Musoq.Evaluator.Diagnostics.Tests", "MalformedQueryErrorTests*.cs", 326),
        new("src/dotnet/Musoq.Evaluator.Relational.Tests", "MultiJoinHashJoinTests*.cs", 417),
        new("src/dotnet/Musoq.Evaluator.Runtime.Tests", "RowAndKeyEqualityTests.Tables*.cs", 357),
        new("src/dotnet/Musoq.Evaluator.Relational.Tests", "SubqueryTests*.cs", 320),
        new("src/dotnet/Musoq.Evaluator.Query.Tests", "AutomaticDateTimeComparisonTests*.cs", 360),
        new("src/dotnet/Musoq.Evaluator.Query.Tests", "AutomaticNumericTypeInferenceTests*.cs", 360),
        new("src/dotnet/Musoq.Evaluator.Diagnostics.Tests", "ErrorQuality.ExprEdgeCaseTests*.cs", 360),
        new("src/dotnet/Musoq.Evaluator.Query.Tests", "StringComparisonConsistencyTests*.cs", 360),
        new("src/dotnet/Musoq.Evaluator.Diagnostics.Tests", "TypoAndUxDiagnosticProbeTests*.cs", 360),
        new("src/dotnet/Musoq.Evaluator.Interpretation.Tests/Spec", "BugProbe.InterpretationSchemasTests*.cs", 458),
        new("src/dotnet/Musoq.Plugins.Functions.Tests", "DateTests*.cs", 238),
        new("src/dotnet/Musoq.Plugins.Conversions.Tests", "LibraryBaseStrictConversionsDecimalTests*.cs", 213),
        new("src/dotnet/Musoq.Plugins.Functions.Tests", "NetworkUtilsTests*.cs", 433),
        new("src/dotnet/Musoq.Plugins.Conversions.Tests", "TypeConversionEdgeCasesTests*.cs", 319),
    ];

    private const int ExecutionExpressionFingerprintImplementationBudget = 0;
}
