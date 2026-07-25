using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static bool CanUsePayloadFreeMarkKeySet(HashJoinBuildContext context)
    {
        return context.Join.Residual == null &&
               context.KeyType != typeof(object) &&
               context.Sides.Build.Shape is not ExpandoAdapterShape &&
               context.Sides.Probe.Shape is not ExpandoAdapterShape;
    }

    private TableBuildResult? TryBuildMarkKeySetJoinTable(HashJoinBuildContext context)
    {
        context = DropUnusedFusedPayloadForKeySet(context);
        var resultTable = new ExecutionVariable(context.ResultTableName, typeof(object));
        var nullAlias = RowShapeLookup.ResolveSourceAlias(context.Sides.Build.Shape);
        var projection = CreateNullExtendedProjection(new NullExtendedProjectionContext(
            context.ResultShapeName,
            resultTable,
            context.Pipeline.Project.Fields,
            context.SourceLookup,
            nullAlias));
        if (!projection.IsBuilt)
            return null;

        var appendBlocks = CreateOuterApplyAppendBlocks(
            context.Pipeline.Filter,
            projection.MatchedAppendRow,
            projection.UnmatchedAppendRow,
            context.SourceLookup,
            nullAlias);
        if (!appendBlocks.IsBuilt)
            return null;

        var matchedBody = CreateOuterJoinMatchedAppendBlock(
            context.Pipeline.Filter,
            projection.MatchedAppendRow,
            context.ConversionLookup);
        if (!TryRewriteMarkBuildKeyReads(context, matchedBody, out matchedBody))
            return null;

        var keySet = new ExecutionVariable(CreateKeySetName(context.Hash.Name), typeof(object));
        var nodes = CreateJoinPrelude(
            context.Sources,
            resultTable,
            projection.ResultShape,
            CreateJoinResultCapacityCandidate(resultTable, context.Sides.Probe));
        nodes.Add(new ExecutionCreateKeySet(
            keySet,
            context.KeyType,
            CreateHashCapacityCandidate(keySet, context.Sides.Build)));
        nodes.Add(CreateKeySetBuildLoop(context, keySet));
        nodes.Add(CreateSourceLoop(
            context.Sides.Probe.Shape,
            context.Sides.Probe.Rows,
            context.Sides.Probe.Variable,
            new ExecutionBlock(
            [
                new ExecutionKeySetProbe(
                    keySet,
                    CreateHashJoinKeyExpression(context.Join.ProbeKeys, context.ConversionLookup, context.KeyType),
                    context.KeyType,
                    matchedBody,
                    appendBlocks.UnmatchedAppendBlock)
            ])));

        return CompleteTableBuild(
            [..context.Sources.Left.Shapes, ..context.Sources.Right.Shapes, projection.ResultShape],
            nodes,
            resultTable,
            projection.ResultShape,
            context.Pipeline.PostOperations,
            context.Pipeline.Project.IsDistinct);
    }

    private static bool TryRewriteMarkBuildKeyReads(
        HashJoinBuildContext context,
        ExecutionBlock body,
        out ExecutionBlock rewrittenBody)
    {
        var replacements = new Dictionary<string, ExecutionExpression>(StringComparer.OrdinalIgnoreCase);
        var buildAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < context.Join.BuildKeys.Length; index++)
        {
            var buildKey = ExecutionExpressionConverter.Convert(
                context.Join.BuildKeys[index],
                context.ConversionLookup);
            if (buildKey is not ExecutionFieldRead buildField)
            {
                rewrittenBody = body;
                return false;
            }

            var probeKey = ExecutionExpressionConverter.Convert(
                context.Join.ProbeKeys[index],
                context.ConversionLookup);
            buildAliases.Add(buildField.Alias ?? string.Empty);
            var key = CreateMarkFieldKey(buildField);
            if (replacements.TryGetValue(key, out var existing) && existing != probeKey)
            {
                rewrittenBody = body;
                return false;
            }

            replacements[key] = probeKey;
        }

        var rewriter = new ExecutionExpressionSubstitutionRewriter(expression =>
        {
            if (expression is not ExecutionFieldRead field)
                return null;

            var key = CreateMarkFieldKey(field);
            return replacements.GetValueOrDefault(key);
        });
        rewrittenBody = rewriter.RewriteBlock(body);
        return !MarkBodyReadsBuildValues(rewrittenBody, buildAliases);
    }

    private static bool MarkBodyReadsBuildValues(
        ExecutionBlock body,
        IReadOnlySet<string> buildAliases)
    {
        foreach (var node in body.Nodes)
        {
            switch (node)
            {
                case ExecutionAppendRow appendRow when appendRow.Values.Any(value =>
                    buildAliases.Any(alias => ReferencesExecutionAlias(value.Value, alias))):
                    return true;
                case ExecutionIf branch when
                    buildAliases.Any(alias => ReferencesExecutionAlias(branch.Condition, alias)) ||
                    MarkBodyReadsBuildValues(branch.Body, buildAliases):
                    return true;
            }
        }

        return false;
    }

    private static string CreateMarkFieldKey(ExecutionFieldRead field) =>
        $"{field.Alias ?? string.Empty}\0{field.FieldName}";
}
