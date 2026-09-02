using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Optimization.Execution;

/// <summary>
///     Moves stable scalar expressions that are repeated by a descendant serial loop into
///     an eager local owned by the earliest loop that supplies their row dependencies.
/// </summary>
internal sealed class LoopInvariantCodeMotionPass : IExecutionIrOptimizationPass
{
    public string Name => "LoopInvariantCodeMotion";

    public OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Options.LoopInvariantCodeMotionEnabled)
        {
            return OptimizationResult<ExecutionPlan>.NoChange(
                plan,
                "Loop-invariant code motion is disabled by compilation options.");
        }

        var rewriter = new Rewriter();
        var optimized = rewriter.RewritePlan(plan);
        var diagnostics = rewriter.FormatDiagnostics();
        if (ReferenceEquals(optimized, plan))
        {
            return OptimizationResult<ExecutionPlan>.NoChange(
                plan,
                $"No stable loop-invariant scalar candidates were hoisted. {diagnostics}");
        }

        return OptimizationResult<ExecutionPlan>.Changed(
            optimized,
            $"Inserted {rewriter.InsertedLets} eager loop-invariant local(s) in {rewriter.RewrittenLoops} loop scope(s). {diagnostics}");
    }

    private sealed class Rewriter
    {
        private readonly HashSet<string> _usedNames = new(StringComparer.Ordinal);
        private readonly List<LoopFrame> _loopPath = [];
        private readonly List<string> _placements = [];
        private readonly Dictionary<string, int> _skipReasons = new(StringComparer.Ordinal);

        public int InsertedLets { get; private set; }

        public int RewrittenLoops { get; private set; }

        public ExecutionPlan RewritePlan(ExecutionPlan plan)
        {
            foreach (var name in ExecutionIrAnalysis.CollectDeclaredVariableNames(plan.Body))
                _usedNames.Add(name);

            var body = RewriteBlock(plan.Body);
            return ReferenceEquals(body, plan.Body) ? plan : plan with { Body = body };
        }

        public string FormatDiagnostics()
        {
            var details = _skipReasons.Count == 0
                ? "none"
                : string.Join(
                    ", ",
                    _skipReasons
                        .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                        .Select(static pair => $"{pair.Key}={pair.Value.ToString(CultureInfo.InvariantCulture)}"));
            var placements = _placements.Count == 0
                ? "none"
                : string.Join(", ", _placements.OrderBy(static placement => placement, StringComparer.Ordinal));
            return $"Placements: {placements}. Skipped candidates: {details}.";
        }

        private ExecutionBlock RewriteBlock(ExecutionBlock block)
        {
            var rewritten = new ExecutionNode[block.Nodes.Count];
            var changed = false;

            for (var index = 0; index < block.Nodes.Count; index++)
            {
                var node = block.Nodes[index];
                var next = node switch
                {
                    ExecutionForEach forEach => RewriteForEach(forEach),
                    ExecutionForEachWithOrdinality forEach => RewriteForEachWithOrdinality(forEach),
                    ExecutionForEachIndexed forEach => RewriteForEachIndexed(forEach),
                    ExecutionIf branch => RewriteIf(branch),
                    _ => node
                };

                rewritten[index] = next;
                changed |= !ReferenceEquals(node, next);
            }

            return changed ? block with { Nodes = rewritten } : block;
        }

        private ExecutionNode RewriteForEach(ExecutionForEach loop)
        {
            var frame = new LoopFrame(loop.Item, null, null);
            _loopPath.Add(frame);
            try
            {
                var (body, lets) = HoistInLoopBody(loop.Body, frame);
                body = RewriteBlock(body);
                body = InsertLets(body, lets, frame);
                return lets.Count == 0 && ReferenceEquals(body, loop.Body)
                    ? loop
                    : loop with { Body = body };
            }
            finally
            {
                _loopPath.RemoveAt(_loopPath.Count - 1);
            }
        }

        private ExecutionNode RewriteForEachWithOrdinality(ExecutionForEachWithOrdinality loop)
        {
            var frame = new LoopFrame(loop.Item, loop.Ordinal, null);
            _loopPath.Add(frame);
            try
            {
                var (body, lets) = HoistInLoopBody(loop.Body, frame);
                body = RewriteBlock(body);
                body = InsertLets(body, lets, frame);
                return lets.Count == 0 && ReferenceEquals(body, loop.Body)
                    ? loop
                    : loop with { Body = body };
            }
            finally
            {
                _loopPath.RemoveAt(_loopPath.Count - 1);
            }
        }

        private ExecutionNode RewriteForEachIndexed(ExecutionForEachIndexed loop)
        {
            var frame = new LoopFrame(loop.Item, loop.Index, loop.Source);
            _loopPath.Add(frame);
            try
            {
                var (body, lets) = HoistInLoopBody(loop.Body, frame);
                body = RewriteBlock(body);
                body = InsertLets(body, lets, frame);
                return lets.Count == 0 && ReferenceEquals(body, loop.Body)
                    ? loop
                    : loop with { Body = body };
            }
            finally
            {
                _loopPath.RemoveAt(_loopPath.Count - 1);
            }
        }

        private ExecutionNode RewriteIf(ExecutionIf branch)
        {
            // A conditional body is its own legal evaluation region. Hoist
            // only into that body (never across the guard), and only when the
            // value is repeated by a descendant serial loop. This preserves
            // short-circuiting, CASE/COALESCE timing, and empty APPLY shape.
            var guardedBody = branch.Body;
            if (_loopPath.Count > 0)
            {
                var hoisted = HoistInLoopBody(guardedBody, _loopPath[^1]);
                guardedBody = InsertLets(hoisted.Body, hoisted.Lets, _loopPath[^1]);
            }
            var body = RewriteBlock(guardedBody);
            return ReferenceEquals(body, branch.Body) ? branch : branch with { Body = body };
        }

        private (ExecutionBlock Body, IReadOnlyList<ExecutionLet> Lets) HoistInLoopBody(
            ExecutionBlock body,
            LoopFrame currentFrame)
        {
            var observations = new Dictionary<string, CandidateObservation>(StringComparer.Ordinal);
            WalkBlock(body, 0, _loopPath.ToArray(), currentFrame, observations);

            var candidates = observations.Values
                .Where(observation => observation.MaxDescendantDepth > 0)
                .OrderBy(observation => ExecutionExpressionCseFacts.GetExpressionDepth(observation.Expression))
                .ThenBy(static observation => observation.Signature, StringComparer.Ordinal)
                .ToArray();
            if (candidates.Length == 0)
                return (body, []);

            var variables = new Dictionary<string, ExecutionVariable>(StringComparer.Ordinal);
            foreach (var candidate in candidates)
            {
                var variable = new ExecutionVariable(
                    CreateVariableName(candidate.Expression),
                    candidate.Expression.ReturnType);
                variables.Add(candidate.Signature, variable);
            }

            var lets = new List<ExecutionLet>(candidates.Length);
            foreach (var candidate in candidates)
            {
                var value = new SubstitutionRewriter(variables, candidate.Signature).RewriteExpression(candidate.Expression);
                lets.Add(new ExecutionLet(variables[candidate.Signature], value, ExecutionLetCacheMode.SuppressMethodCache));
            }

            var rewrittenBody = ReplaceInBlock(body, variables);
            InsertedLets += lets.Count;
            RewrittenLoops++;
            return (rewrittenBody, lets);
        }

        private void WalkBlock(
            ExecutionBlock block,
            int descendantDepth,
            IReadOnlyList<LoopFrame> frames,
            LoopFrame currentFrame,
            IDictionary<string, CandidateObservation> observations)
        {
            foreach (var node in block.Nodes)
            {
                switch (node)
                {
                    case ExecutionForEach forEach:
                        WalkNestedLoop(forEach.Item, null, null, forEach.Body, descendantDepth, frames, currentFrame, observations);
                        continue;
                    case ExecutionForEachWithOrdinality forEach:
                        WalkNestedLoop(forEach.Item, forEach.Ordinal, null, forEach.Body, descendantDepth, frames, currentFrame, observations);
                        continue;
                    case ExecutionForEachIndexed forEach:
                        WalkNestedLoop(forEach.Item, forEach.Index, forEach.Source, forEach.Body, descendantDepth, frames, currentFrame, observations);
                        continue;
                    case ExecutionIf:
                    case ExecutionContinueIf:
                    case ExecutionAssign:
                        IncrementSkip("conditional-or-mutable scope");
                        continue;
                    case ExecutionScopedBlock:
                    case ExecutionRecursiveCte:
                    case ExecutionParallelBlock:
                    case ExecutionParallelFilterProjectLoop:
                    case ExecutionParallelSingleKeyAggregateLoop:
                    case ExecutionMaterializeList:
                    case ExecutionMaterializeFilteredList:
                    case ExecutionMaterializeExpandoList:
                    case ExecutionWindowKernelPlan:
                    case ExecutionComputeRankingWindow:
                    case ExecutionComputeOffsetWindow:
                    case ExecutionComputePluginWindow:
                    case ExecutionWindowAggregateKernel:
                        IncrementSkip("boundary");
                        continue;
                }

                foreach (var expression in GetCandidateExpressions(node))
                    CollectExpression(expression, descendantDepth, frames, currentFrame, observations);
            }
        }

        private void WalkNestedLoop(
            ExecutionVariable item,
            ExecutionVariable? ordinal,
            ExecutionVariable? index,
            ExecutionBlock body,
            int descendantDepth,
            IReadOnlyList<LoopFrame> frames,
            LoopFrame currentFrame,
            IDictionary<string, CandidateObservation> observations)
        {
            var nestedFrame = new LoopFrame(item, ordinal, index);
            WalkBlock(
                body,
                descendantDepth + 1,
                [..frames, nestedFrame],
                currentFrame,
                observations);
        }

        private void CollectExpression(
            ExecutionExpression expression,
            int descendantDepth,
            IReadOnlyList<LoopFrame> frames,
            LoopFrame currentFrame,
            IDictionary<string, CandidateObservation> observations)
        {
            if (IsConditionalExpression(expression))
            {
                IncrementSkip("conditional");
                return;
            }

            var dependencies = GetDependencyNames(expression);
            var owner = FindDeepestOwner(frames, dependencies);
            var isStable = ExpressionStabilityAnalyzer.IsStable(expression);
            if (!ExecutionExpressionCseFacts.IsCseResultTypeStable(expression.ReturnType.ResolveClrType()))
            {
                IncrementSkip("non-scalar result");
                foreach (var child in ExecutionIrAnalysis.GetChildExpressions(expression))
                    CollectExpression(child, descendantDepth, frames, currentFrame, observations);
                return;
            }

            if (isStable && !IsTrivial(expression) && ReferenceEquals(owner, currentFrame))
            {
                var signature = ExecutionExpressionFingerprint.ForHoist(expression);
                if (!observations.TryGetValue(signature, out var observation))
                {
                    observations.Add(
                        signature,
                        new CandidateObservation(signature, expression, descendantDepth));
                }
                else if (descendantDepth > observation.MaxDescendantDepth)
                {
                    observations[signature] = observation with { MaxDescendantDepth = descendantDepth };
                }

                return;
            }

            if (!isStable)
                IncrementSkip("volatile or unknown");
            else if (owner != null && !ReferenceEquals(owner, currentFrame))
                IncrementSkip("inner dependency");

            foreach (var child in ExecutionIrAnalysis.GetChildExpressions(expression))
                CollectExpression(child, descendantDepth, frames, currentFrame, observations);
        }

        private static bool IsConditionalExpression(ExecutionExpression expression)
        {
            return expression switch
            {
                ExecutionBinary { Kind: BinaryOpKind.And or BinaryOpKind.Or } => true,
                ExecutionCaseWhen or ExecutionCoalesce => true,
                _ => false
            };
        }

        private static bool IsTrivial(ExecutionExpression expression)
        {
            return expression is ExecutionLiteral or
                ExecutionVariableRead or
                ExecutionScriptVariableRead or
                ExecutionScriptParameterRead;
        }

        private static IReadOnlyList<ExecutionExpression> GetCandidateExpressions(ExecutionNode node)
        {
            return node switch
            {
                ExecutionAppendRow appendRow => appendRow.Values
                    .Select(static value => value.Value)
                    .Concat(appendRow.Contexts)
                    .Concat(ExecutionNodeFacts.GetContextLayoutExpressions(appendRow.ContextLayout))
                    .ToArray(),
                ExecutionAppendRecord appendRecord => appendRecord.Values.Select(static value => value.Value).ToArray(),
                ExecutionCreateGeneratedRow createRow => createRow.Values
                    .Select(static value => value.Value)
                    .Concat(createRow.Contexts)
                    .Concat(ExecutionNodeFacts.GetContextLayoutExpressions(createRow.ContextLayout))
                    .ToArray(),
                ExecutionLet let => [let.Value],
                ExecutionHashAdd hashAdd => [hashAdd.Key],
                ExecutionHashProbe hashProbe => [hashProbe.Key],
                ExecutionKeySetAdd keySetAdd => [keySetAdd.Key],
                ExecutionKeySetProbe keySetProbe => [keySetProbe.Key],
                ExecutionCreateHashPayload payload => payload.Values.Select(static value => value.Value).ToArray(),
                ExecutionAggregateSet aggregateSet => aggregateSet.Arguments
                    .Concat(aggregateSet.AccumulatorInput is null
                        ? []
                        : [aggregateSet.AccumulatorInput])
                    .ToArray(),
                _ => []
            };
        }

        private static IReadOnlySet<string> GetDependencyNames(ExecutionExpression expression)
        {
            var dependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var current in ExecutionIrAnalysis.FlattenExpressions(expression))
            {
                switch (current)
                {
                    case ExecutionFieldRead { Alias: { } alias } when !string.IsNullOrWhiteSpace(alias):
                        dependencies.Add(alias);
                        break;
                    case ExecutionVariableRead variableRead:
                        dependencies.Add(variableRead.Variable.Name);
                        break;
                    case ExecutionRowStream rows:
                        dependencies.Add(rows.Variable.Name);
                        break;
                    case ExecutionScalarRowStream rows:
                        dependencies.Add(rows.Variable.Name);
                        break;
                    case ExecutionRowContextsRead contexts:
                        dependencies.Add(contexts.Row.Name);
                        break;
                    case ExecutionMethodCall methodCall:
                        if (methodCall.Target != null)
                            dependencies.Add(methodCall.Target.Name);
                        if (methodCall.Cache != null)
                            dependencies.Add(methodCall.Cache.Name);
                        break;
                    case ExecutionStrictCast strictCast when strictCast.Target != null:
                        dependencies.Add(strictCast.Target.Name);
                        break;
                    case ExecutionIndexedHashRowCreate indexed:
                        dependencies.Add(indexed.Row.Name);
                        dependencies.Add(indexed.Index.Name);
                        break;
                    case ExecutionIndexedHashRowRowRead rowRead:
                        dependencies.Add(rowRead.IndexedRow.Name);
                        break;
                    case ExecutionIndexedHashRowIndexRead indexRead:
                        dependencies.Add(indexRead.IndexedRow.Name);
                        break;
                }
            }

            return dependencies;
        }

        private static LoopFrame? FindDeepestOwner(
            IReadOnlyList<LoopFrame> frames,
            IReadOnlySet<string> dependencies)
        {
            for (var index = frames.Count - 1; index >= 0; index--)
            {
                if (frames[index].Names.Any(dependencies.Contains))
                    return frames[index];
            }

            return null;
        }

        private ExecutionBlock ReplaceInBlock(
            ExecutionBlock block,
            IReadOnlyDictionary<string, ExecutionVariable> variables)
        {
            var rewriter = new SubstitutionRewriter(variables, null);
            var rewritten = new ExecutionNode[block.Nodes.Count];
            var changed = false;
            for (var index = 0; index < block.Nodes.Count; index++)
            {
                var node = block.Nodes[index];
                var next = node switch
                {
                    ExecutionForEach forEach => forEach with { Body = ReplaceInBlock(forEach.Body, variables) },
                    ExecutionForEachWithOrdinality forEach => forEach with { Body = ReplaceInBlock(forEach.Body, variables) },
                    ExecutionForEachIndexed forEach => forEach with { Body = ReplaceInBlock(forEach.Body, variables) },
                    ExecutionIf branch => branch with { Body = ReplaceInBlock(branch.Body, variables) },
                    ExecutionScopedBlock => node,
                    ExecutionRecursiveCte => node,
                    ExecutionParallelBlock => node,
                    ExecutionParallelFilterProjectLoop => node,
                    ExecutionParallelSingleKeyAggregateLoop => node,
                    _ => RewriteCandidateNode(node, rewriter)
                };
                rewritten[index] = next;
                changed |= !ReferenceEquals(node, next);
            }

            return changed ? block with { Nodes = rewritten } : block;
        }

        private static ExecutionNode RewriteCandidateNode(
            ExecutionNode node,
            SubstitutionRewriter rewriter)
        {
            return node switch
            {
                ExecutionAppendRow appendRow => appendRow with
                {
                    Values = appendRow.Values
                        .Select(value => value with { Value = rewriter.RewriteExpression(value.Value) })
                        .ToArray(),
                    Contexts = appendRow.Contexts.Select(rewriter.RewriteExpression).ToArray(),
                    ContextLayout = rewriter.RewriteLayout(appendRow.ContextLayout)
                },
                ExecutionAppendRecord appendRecord => appendRecord with
                {
                    Values = appendRecord.Values
                        .Select(value => value with { Value = rewriter.RewriteExpression(value.Value) })
                        .ToArray()
                },
                ExecutionCreateGeneratedRow createRow => createRow with
                {
                    Values = createRow.Values
                        .Select(value => value with { Value = rewriter.RewriteExpression(value.Value) })
                        .ToArray(),
                    Contexts = createRow.Contexts.Select(rewriter.RewriteExpression).ToArray(),
                    ContextLayout = rewriter.RewriteLayout(createRow.ContextLayout)
                },
                ExecutionLet let => let with { Value = rewriter.RewriteExpression(let.Value) },
                ExecutionHashAdd hashAdd => hashAdd with { Key = rewriter.RewriteExpression(hashAdd.Key) },
                ExecutionHashProbe hashProbe => hashProbe with { Key = rewriter.RewriteExpression(hashProbe.Key) },
                ExecutionKeySetAdd keySetAdd => keySetAdd with { Key = rewriter.RewriteExpression(keySetAdd.Key) },
                ExecutionKeySetProbe keySetProbe => keySetProbe with { Key = rewriter.RewriteExpression(keySetProbe.Key) },
                ExecutionCreateHashPayload payload => payload with
                {
                    Values = payload.Values
                        .Select(value => value with { Value = rewriter.RewriteExpression(value.Value) })
                        .ToArray()
                },
                ExecutionAggregateSet aggregateSet => aggregateSet with
                {
                    Arguments = aggregateSet.Arguments.Select(rewriter.RewriteExpression).ToArray(),
                    FilterPredicate = aggregateSet.FilterPredicate is null
                        ? null
                        : rewriter.RewriteExpression(aggregateSet.FilterPredicate),
                    AccumulatorInput = aggregateSet.AccumulatorInput is null
                        ? null
                        : rewriter.RewriteExpression(aggregateSet.AccumulatorInput)
                },
                _ => node
            };
        }

        private ExecutionBlock InsertLets(
            ExecutionBlock body,
            IReadOnlyList<ExecutionLet> lets,
            LoopFrame frame)
        {
            if (lets.Count == 0)
                return body;

            var insertionIndex = 0;
            while (insertionIndex < body.Nodes.Count && IsPrologueNode(body.Nodes[insertionIndex]))
                insertionIndex++;

            var nodes = new List<ExecutionNode>(body.Nodes.Count + lets.Count);
            nodes.AddRange(body.Nodes.Take(insertionIndex));
            nodes.AddRange(lets);
            nodes.AddRange(body.Nodes.Skip(insertionIndex));
            _placements.Add($"{frame.Item.Name}: {string.Join(",", lets.Select(static let => let.Variable.Name))}");
            return body with { Nodes = nodes };
        }

        private static bool IsPrologueNode(ExecutionNode node)
        {
            return node is ExecutionAdaptExpando or
                ExecutionLet or
                ExecutionMethodTargetDeclarationCandidate;
        }

        private string CreateVariableName(ExecutionExpression expression)
        {
            var candidate = expression switch
            {
                ExecutionFieldRead fieldRead => string.IsNullOrWhiteSpace(fieldRead.Alias)
                    ? fieldRead.FieldName
                    : fieldRead.Alias + fieldRead.FieldName,
                ExecutionMemberRead memberRead => memberRead.MemberName,
                ExecutionMethodCall methodCall => methodCall.Method.MethodName,
                ExecutionMethodTargetReuseCandidate candidateCall => candidateCall.MethodCall.Method.MethodName,
                ExecutionStrictCast => "cast",
                _ => "expr"
            };

            var normalized = new string(candidate
                .Where(static character => char.IsLetterOrDigit(character) || character == '_')
                .ToArray());
            if (string.IsNullOrWhiteSpace(normalized))
                normalized = "expr";
            normalized = char.ToLowerInvariant(normalized[0]) + normalized[1..];
            if (normalized.StartsWith("__", StringComparison.Ordinal))
                normalized = "expr" + normalized.TrimStart('_');

            var name = normalized;
            var suffix = 1;
            while (!_usedNames.Add(name))
                name = normalized + suffix++.ToString(CultureInfo.InvariantCulture);
            return name;
        }

        private void IncrementSkip(string reason)
        {
            _skipReasons.TryGetValue(reason, out var count);
            _skipReasons[reason] = count + 1;
        }
    }

    private sealed record LoopFrame(
        ExecutionVariable Item,
        ExecutionVariable? Ordinal,
        ExecutionVariable? Index)
    {
        public IReadOnlyList<string> Names { get; } = CreateNames(Item, Ordinal, Index);

        private static IReadOnlyList<string> CreateNames(
            ExecutionVariable item,
            ExecutionVariable? ordinal,
            ExecutionVariable? index)
        {
            var names = new List<string> { item.Name };
            if (ordinal != null)
                names.Add(ordinal.Name);
            if (index != null)
                names.Add(index.Name);
            return names;
        }
    }

    private sealed record CandidateObservation(
        string Signature,
        ExecutionExpression Expression,
        int MaxDescendantDepth);

    private sealed class SubstitutionRewriter(
        IReadOnlyDictionary<string, ExecutionVariable> variables,
        string? rootSignature) : ExecutionExpressionSubstitutionRewriterBase
    {
        public override ExecutionExpression RewriteExpression(ExecutionExpression expression)
        {
            if (rootSignature == null || !string.Equals(
                    ExecutionExpressionFingerprint.ForHoist(expression),
                    rootSignature,
                    StringComparison.Ordinal))
            {
                if (variables.TryGetValue(ExecutionExpressionFingerprint.ForHoist(expression), out var variable))
                    return new ExecutionVariableRead(variable);
            }

            return base.RewriteExpression(expression);
        }

        protected override ExecutionExpression RewriteBinary(ExecutionBinary expression)
        {
            return expression.Kind is BinaryOpKind.And or BinaryOpKind.Or
                ? expression
                : base.RewriteBinary(expression);
        }

        protected override ExecutionExpression RewriteCaseWhen(ExecutionCaseWhen expression) => expression;

        protected override ExecutionExpression RewriteCoalesce(ExecutionCoalesce expression) => expression;

        public ExecutionContextLayout? RewriteLayout(ExecutionContextLayout? layout)
        {
            if (layout == null)
                return null;

            var segments = layout.Segments
                .Select(segment => segment with { Value = RewriteExpression(segment.Value) })
                .ToArray();
            return layout with { Segments = segments };
        }
    }

    private abstract class ExecutionExpressionSubstitutionRewriterBase : ExecutionIrRewriter;
}
