using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed class FieldExpressionHoistingPass : IExecutionIrOptimizationPass
{
    public string Name => "FieldExpressionHoisting";

    public OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var candidateRewriter = new HoistCandidateLoweringRewriter();
        var candidateOptimized = candidateRewriter.RewritePlan(plan);
        var candidateText = FormatCandidateLowering(candidateRewriter);

        if (!IsFieldReadDiscoveryEnabled(context))
        {
            if (ReferenceEquals(candidateOptimized, plan))
            {
                return OptimizationResult<ExecutionPlan>.NoChange(
                    plan,
                    "Field-read discovery is disabled by compilation options and no hoist candidates were present.");
            }

            return OptimizationResult<ExecutionPlan>.Changed(
                candidateOptimized,
                $"{candidateText}Field-read discovery is disabled by compilation options.");
        }

        var rewriter = new FieldReadHoistingRewriter();
        var optimized = rewriter.RewritePlan(candidateOptimized);
        if (ReferenceEquals(optimized, plan))
        {
            return OptimizationResult<ExecutionPlan>.NoChange(
                plan,
                $"{candidateText}No repeated source-qualified field reads were found in supported local or nested append nodes.");
        }

        return OptimizationResult<ExecutionPlan>.Changed(
            optimized,
            $"{candidateText}Inserted {rewriter.InsertedLets} local or nested field-read hoist(s) in {rewriter.RewrittenNodes} Execution IR node(s).");
    }

    private static bool IsFieldReadDiscoveryEnabled(OptimizationContext context)
    {
        return context.Options.FieldReadDiscoveryEnabled;
    }

    private static string FormatCandidateLowering(HoistCandidateLoweringRewriter rewriter)
    {
        return rewriter.LoweredCandidates == 0
            ? string.Empty
            : $"Lowered {rewriter.LoweredCandidates} hoist candidate let(s). ";
    }

    private sealed class HoistCandidateLoweringRewriter : ExecutionIrRewriter
    {
        public int LoweredCandidates { get; private set; }

        protected override ExecutionNode RewriteHoistCandidateLet(ExecutionHoistCandidateLet node)
        {
            LoweredCandidates++;
            return new ExecutionLet(node.Variable, RewriteExpression(node.Value));
        }
    }

    private sealed class FieldReadHoistingRewriter : ExecutionIrRewriter
    {
        private HashSet<string>? _usedNames;
        private int _blockDepth;

        public int InsertedLets { get; private set; }

        public int RewrittenNodes { get; private set; }

        public override ExecutionPlan RewritePlan(ExecutionPlan plan)
        {
            _usedNames = ExecutionIrAnalysis.CollectDeclaredVariableNames(plan.Body).ToHashSet(StringComparer.Ordinal);

            try
            {
                return base.RewritePlan(plan);
            }
            finally
            {
                _usedNames = null;
            }
        }

        public override ExecutionBlock RewriteBlock(ExecutionBlock block)
        {
            var includeAppendContexts = _blockDepth == 0;
            var usedNames = _usedNames ?? ExecutionIrAnalysis.CollectDeclaredVariableNames(block).ToHashSet(StringComparer.Ordinal);
            var builder = new ExecutionBlockRewriteBuilder(block);

            _blockDepth++;
            try
            {
                for (var index = 0; index < block.Nodes.Count; index++)
                {
                    var node = block.Nodes[index];
                    var rewrittenNode = RewriteNode(node);
                    var hoisted = TryHoistFieldReads(rewrittenNode, usedNames, includeAppendContexts);
                    var isChanged = !ReferenceEquals(rewrittenNode, node) || hoisted.Lets.Count > 0;

                    if (!isChanged && !builder.HasChanges)
                        continue;

                    builder.EnsureStartedAt(index);
                    foreach (var let in hoisted.Lets)
                    {
                        builder.Add(let);
                        usedNames.Add(let.Variable.Name);
                        InsertedLets++;
                    }

                    if (hoisted.Lets.Count > 0)
                        RewrittenNodes++;

                    builder.Add(hoisted.Node);
                }

                return builder.ToBlock();
            }
            finally
            {
                _blockDepth--;
            }
        }

        private static HoistedNode TryHoistFieldReads(
            ExecutionNode node,
            HashSet<string> usedNames,
            bool includeAppendContexts)
        {
            var reads = CollectSupportedFieldReads(node, includeAppendContexts)
                .Where(static read => !string.IsNullOrWhiteSpace(read.Alias))
                .GroupBy(static read => read)
                .Where(static group => group.Count() > 1)
                .Select(static group => group.Key)
                .ToArray();

            if (reads.Length == 0)
                return new HoistedNode([], node);

            var variables = reads.ToDictionary(
                static read => read,
                read => new ExecutionVariable(CreateHoistedVariableName(read, usedNames), read.ReturnType));
            var lets = reads
                .Select(read => new ExecutionLet(variables[read], read))
                .ToArray();
            var rewritten = ReplaceSupportedFieldReads(node, variables, includeAppendContexts);

            return new HoistedNode(lets, rewritten);
        }

        private static IEnumerable<ExecutionFieldRead> CollectSupportedFieldReads(
            ExecutionNode node,
            bool includeAppendContexts)
        {
            return node switch
            {
                ExecutionAppendRow appendRow => appendRow.Values
                    .SelectMany(static value => CollectHoistableFieldReads(value.Value))
                    .Concat(includeAppendContexts
                        ? appendRow.Contexts.SelectMany(CollectHoistableFieldReads)
                        : [])
                    .Concat(includeAppendContexts && appendRow.ContextLayout != null
                        ? appendRow.ContextLayout.Segments.SelectMany(static segment => CollectHoistableFieldReads(segment.Value))
                        : []),
                ExecutionAppendRecord appendRecord => appendRecord.Values
                    .SelectMany(static value => CollectHoistableFieldReads(value.Value)),
                _ => []
            };
        }

        private static IEnumerable<ExecutionFieldRead> CollectHoistableFieldReads(ExecutionExpression expression)
        {
            if (expression is ExecutionFieldRead fieldRead)
            {
                yield return fieldRead;
                yield break;
            }

            if (expression is ExecutionArrayAccess arrayAccess)
            {
                foreach (var read in CollectHoistableFieldReads(arrayAccess.Index))
                    yield return read;

                yield break;
            }

            foreach (var child in ExecutionIrAnalysis.GetChildExpressions(expression))
            {
                foreach (var read in CollectHoistableFieldReads(child))
                    yield return read;
            }
        }

        private static ExecutionNode ReplaceSupportedFieldReads(
            ExecutionNode node,
            IReadOnlyDictionary<ExecutionFieldRead, ExecutionVariable> variablesByRead,
            bool includeAppendContexts)
        {
            var rewriter = new ExecutionExpressionSubstitutionRewriter(expression =>
                expression is ExecutionFieldRead fieldRead &&
                variablesByRead.TryGetValue(fieldRead, out var variable)
                    ? new ExecutionVariableRead(variable)
                    : null);

            return includeAppendContexts || node is not ExecutionAppendRow appendRow
                ? rewriter.RewriteNode(node)
                : RewriteAppendRowValuesOnly(appendRow, rewriter);
        }

        private static ExecutionNode RewriteAppendRowValuesOnly(
            ExecutionAppendRow appendRow,
            ExecutionExpressionSubstitutionRewriter rewriter)
        {
            var values = rewriter.RewriteRows(appendRow.Values);
            return ReferenceEquals(values, appendRow.Values)
                ? appendRow
                : appendRow with { Values = values };
        }

        private static string CreateHoistedVariableName(ExecutionFieldRead read, ISet<string> usedNames)
        {
            var candidate = read.FieldName.Split('.').LastOrDefault(static part => !string.IsNullOrWhiteSpace(part)) ?? "field";
            candidate = ExecutionSymbolicNamePolicy.CreateLoweringIdentifierCandidate(candidate, usedNames.Count);
            candidate = char.ToLowerInvariant(candidate[0]) + candidate[1..];

            var variableName = candidate;
            var suffix = 1;
            while (!usedNames.Add(variableName))
            {
                variableName = string.Concat(candidate, suffix.ToString(CultureInfo.InvariantCulture));
                suffix++;
            }

            return variableName;
        }

    }

    private sealed record HoistedNode(
        IReadOnlyList<ExecutionLet> Lets,
        ExecutionNode Node);
}

