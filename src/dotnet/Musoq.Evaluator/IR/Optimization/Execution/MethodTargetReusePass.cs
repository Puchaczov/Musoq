using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed class MethodTargetReusePass : IExecutionIrOptimizationPass
{
    public string Name => "MethodTargetReuse";
    public OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);
        var candidateRewriter = new MethodTargetCandidateLoweringRewriter();
        var candidateOptimized = candidateRewriter.RewritePlan(plan);
        var loweredCandidateText = FormatCandidateLowering(candidateRewriter);
        var rewriter = new MethodTargetReuseRewriter(plan.Identifier);
        var optimized = rewriter.RewritePlan(candidateOptimized);
        var methodCalls = ExecutionIrAnalysis.CollectExpressions<ExecutionMethodCall>(optimized.Body).ToArray();
        var targetCount = methodCalls.Count(static call => call.Target != null);
        var cacheCount = methodCalls.Count(static call => call.Cache != null);
        var declarationCount = ExecutionIrAnalysis.CollectNodes<ExecutionCreateObject>(optimized.Body).Count();
        if (ReferenceEquals(optimized, plan))
        {
            return OptimizationResult<ExecutionPlan>.NoChange(
                plan,
                $"{loweredCandidateText}Observed {targetCount} target-bound method call(s), {cacheCount} cached method call(s), and {declarationCount} target declaration(s); no unbound reusable method calls required rewriting.");
        }

        return OptimizationResult<ExecutionPlan>.Changed(
            optimized,
            $"{loweredCandidateText}Bound {rewriter.AssignedTargets} method call target(s), assigned {rewriter.AssignedCaches} method cache(s), and inserted {rewriter.InsertedDeclarations} target declaration(s).");
    }
    private static string FormatCandidateLowering(MethodTargetCandidateLoweringRewriter rewriter)
    {
        return rewriter.LoweredCalls == 0 && rewriter.LoweredDeclarations == 0
            ? string.Empty
            : $"Lowered {rewriter.LoweredCalls} method target reuse candidate call(s) and {rewriter.LoweredDeclarations} declaration candidate(s). ";
    }
    private sealed class MethodTargetCandidateLoweringRewriter : ExecutionIrRewriter
    {
        public int LoweredCalls { get; private set; }
        public int LoweredDeclarations { get; private set; }
        protected override ExecutionNode RewriteMethodTargetDeclarationCandidate(ExecutionMethodTargetDeclarationCandidate node)
        {
            LoweredDeclarations++;
            return new ExecutionCreateObject(node.Target);
        }
        protected override ExecutionExpression RewriteMethodTargetReuseCandidate(ExecutionMethodTargetReuseCandidate expression)
        {
            LoweredCalls++;
            return RewriteMethodCall(expression.MethodCall);
        }
    }

    private sealed class MethodTargetReuseRewriter(string planIdentifier) : ExecutionIrRewriter
    {
        private MethodTargetRegistry? _currentRegistry;
        private string? _currentTableTargetNamePrefix;
        private int _nextRegistryIndex;
        private int _deferMethodTargetDeclarationsDepth;
        private string? _preferredTargetNamePrefix;
        private int _valueTypeMethodCacheScopeDepth;
        private int _suppressMethodCacheScopeDepth;
        public int AssignedTargets { get; private set; }
        public int AssignedCaches { get; private set; }
        public int InsertedDeclarations { get; private set; }
        public override ExecutionBlock RewriteBlock(ExecutionBlock block)
        {
            if (_deferMethodTargetDeclarationsDepth > 0)
                return RewriteBlockInCurrentRegistry(block);
            var previousRegistry = _currentRegistry;
            var registry = new MethodTargetRegistry(CreateRegistryPrefix(), previousRegistry);
            _currentRegistry = registry;
            try
            {
                var builder = new ExecutionBlockRewriteBuilder(block);
                for (var index = 0; index < block.Nodes.Count; index++)
                {
                    var node = block.Nodes[index];
                    if (node is ExecutionCreateTable createTable)
                        _currentTableTargetNamePrefix = createTable.Table.Name;
                    if (node is ExecutionCreateObject createObject)
                        registry.AddExisting(createObject.Target);
                    else if (node is ExecutionCreateAggregateLibrary aggregateLibrary)
                        registry.AddExisting(aggregateLibrary.Library);
                    var targetStartIndex = registry.CreatedTargets.Count;
                    var rewrittenNode = RewriteNode(node);
                    var hasNewTargets = registry.CreatedTargets.Count > targetStartIndex;
                    var isNodeChanged = !ReferenceEquals(rewrittenNode, node);
                    if (!hasNewTargets && !isNodeChanged && !builder.HasChanges)
                    {
                        if (IsMethodTargetScopeBarrier(node))
                            registry.ForgetReusableTargets();
                        continue;
                    }
                    builder.EnsureStartedAt(index);
                    if (hasNewTargets)
                    {
                        InsertedDeclarations += MethodTargetDeclarationPlacement.InsertCreatedTargetDeclarations(
                            builder,
                            registry.CreatedTargets.Skip(targetStartIndex),
                            block,
                            index);
                    }
                    builder.Add(rewrittenNode);
                    if (IsMethodTargetScopeBarrier(node))
                        registry.ForgetReusableTargets();
                }
                return builder.ToBlock();
            }
            finally
            {
                _currentRegistry = previousRegistry;
            }
        }

        private ExecutionBlock RewriteBlockInCurrentRegistry(ExecutionBlock block)
        {
            var builder = new ExecutionBlockRewriteBuilder(block);
            for (var index = 0; index < block.Nodes.Count; index++)
            {
                var node = block.Nodes[index];
                if (node is ExecutionCreateTable createTable)
                    _currentTableTargetNamePrefix = createTable.Table.Name;
                if (node is ExecutionCreateObject createObject)
                    CurrentRegistry.AddExisting(createObject.Target);
                else if (node is ExecutionCreateAggregateLibrary aggregateLibrary)
                    CurrentRegistry.AddExisting(aggregateLibrary.Library);
                var rewrittenNode = RewriteNode(node);
                if (ReferenceEquals(rewrittenNode, node) && !builder.HasChanges)
                {
                    if (IsMethodTargetScopeBarrier(node))
                        CurrentRegistry.ForgetReusableTargets();
                    continue;
                }
                builder.EnsureStartedAt(index);
                builder.Add(rewrittenNode);
                if (IsMethodTargetScopeBarrier(node))
                    CurrentRegistry.ForgetReusableTargets();
            }
            return builder.ToBlock();
        }
        protected override ExecutionExpression RewriteMethodCall(ExecutionMethodCall expression)
        {
            var rewritten = (ExecutionMethodCall)base.RewriteMethodCall(expression);
            if (ExecutionMethodTargetReuse.CanRenderWithoutTarget(rewritten))
                return rewritten;
            var target = rewritten.Target;
            if (target == null)
            {
                target = CurrentRegistry.GetOrAdd(rewritten.Method.ResolveClrMethod(), _preferredTargetNamePrefix);
                if (target == null)
                    return rewritten;
                AssignedTargets++;
                rewritten = rewritten with { Target = target };
            }
            if (_suppressMethodCacheScopeDepth > 0)
                return rewritten.Cache == null ? rewritten : rewritten with { Cache = null };

            if (rewritten.Cache != null ||
                !MethodTargetCachePolicy.ShouldCache(rewritten, _valueTypeMethodCacheScopeDepth > 0))
            {
                return rewritten;
            }
            var cache = CurrentRegistry.GetOrAddCache(rewritten, target);
            if (cache == null)
                return rewritten;
            AssignedCaches++;
            return rewritten with { Cache = cache };
        }
        protected override ExecutionExpression RewriteStrictCast(ExecutionStrictCast expression)
        {
            return (ExecutionStrictCast)base.RewriteStrictCast(expression);
        }

        protected override ExecutionNode RewriteParallelFilterProjectLoop(ExecutionParallelFilterProjectLoop node)
        {
            var prefix = node.AppendRow.Table.Name;
            return RewriteWithPreferredTargetNamePrefix(prefix, () =>
            {
                return RewriteWithDeferredMethodTargetDeclarations(() =>
                {
                    var sourceRows = RewriteExpression(node.SourceRows);
                    var predicate = RewriteWithSuppressedMethodCache(() => RewriteOptionalExpression(node.Predicate));
                    var appendRow = (ExecutionAppendRow)RewriteWithSuppressedMethodCache(() => RewriteAppendRow(node.AppendRow));
                    var projectionBody = RewriteBlock(node.ProjectionBody);

                    return ReferenceEquals(sourceRows, node.SourceRows) &&
                           ReferenceEquals(predicate, node.Predicate) &&
                           ReferenceEquals(appendRow, node.AppendRow) &&
                           ReferenceEquals(projectionBody, node.ProjectionBody)
                        ? node
                        : node with
                        {
                            SourceRows = sourceRows,
                            Predicate = predicate,
                            AppendRow = appendRow,
                            ProjectionBody = projectionBody
                        };
                });
            });
        }

        protected override ExecutionNode RewriteForEach(ExecutionForEach node)
        {
            var prefix = MethodTargetScopeFacts.GetLoopPrefix(node.Body, _currentTableTargetNamePrefix);
            return string.IsNullOrWhiteSpace(prefix)
                ? base.RewriteForEach(node)
                : RewriteWithPreferredTargetNamePrefix(
                    prefix,
                    () => RewriteWithDeferredMethodTargetDeclarations(() => (ExecutionNode)RewriteForEachCore(node)));
        }

        protected override ExecutionNode RewriteForEachWithOrdinality(ExecutionForEachWithOrdinality node)
        {
            var prefix = MethodTargetScopeFacts.GetLoopPrefix(node.Body, _currentTableTargetNamePrefix);
            return string.IsNullOrWhiteSpace(prefix)
                ? base.RewriteForEachWithOrdinality(node)
                : RewriteWithPreferredTargetNamePrefix(
                    prefix,
                    () => RewriteWithDeferredMethodTargetDeclarations(() => (ExecutionNode)RewriteForEachWithOrdinalityCore(node)));
        }

        protected override ExecutionNode RewriteForEachIndexed(ExecutionForEachIndexed node)
        {
            var prefix = MethodTargetScopeFacts.GetLoopPrefix(node.Body, _currentTableTargetNamePrefix);
            return string.IsNullOrWhiteSpace(prefix)
                ? base.RewriteForEachIndexed(node)
                : RewriteWithPreferredTargetNamePrefix(
                    prefix,
                    () => RewriteWithDeferredMethodTargetDeclarations(() => (ExecutionNode)RewriteForEachIndexedCore(node)));
        }

        protected override ExecutionNode RewriteMaterializeFilteredList(ExecutionMaterializeFilteredList node)
        {
            return RewriteWithPreferredTargetNamePrefix(
                MethodTargetScopeFacts.CreateMaterializationTargetPrefix(node.Buffer.Name),
                () => base.RewriteMaterializeFilteredList(node));
        }

        protected override ExecutionNode RewriteMaterializeExpandoList(ExecutionMaterializeExpandoList node)
        {
            return node.Predicate == null
                ? base.RewriteMaterializeExpandoList(node)
                : RewriteWithPreferredTargetNamePrefix(
                    MethodTargetScopeFacts.CreateMaterializationTargetPrefix(node.Buffer.Name),
                    () => base.RewriteMaterializeExpandoList(node));
        }

        private ExecutionForEach RewriteForEachCore(ExecutionForEach node)
        {
            var source = RewriteExpression(node.Source);
            var body = RewriteBlock(node.Body);
            return ReferenceEquals(source, node.Source) && ReferenceEquals(body, node.Body)
                ? node
                : node with { Source = source, Body = body };
        }

        private ExecutionForEachWithOrdinality RewriteForEachWithOrdinalityCore(ExecutionForEachWithOrdinality node)
        {
            var source = RewriteExpression(node.Source);
            var body = RewriteBlock(node.Body);
            return ReferenceEquals(source, node.Source) && ReferenceEquals(body, node.Body)
                ? node
                : node with { Source = source, Body = body };
        }

        private ExecutionForEachIndexed RewriteForEachIndexedCore(ExecutionForEachIndexed node)
        {
            var body = RewriteBlock(node.Body);
            return ReferenceEquals(body, node.Body) ? node : node with { Body = body };
        }

        private T RewriteWithDeferredMethodTargetDeclarations<T>(Func<T> rewrite)
        {
            _deferMethodTargetDeclarationsDepth++;
            try
            {
                return rewrite();
            }
            finally
            {
                _deferMethodTargetDeclarationsDepth--;
            }
        }

        private T RewriteWithPreferredTargetNamePrefix<T>(string? prefix, Func<T> rewrite)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return rewrite();

            var previousPrefix = _preferredTargetNamePrefix;
            _preferredTargetNamePrefix = prefix;
            try
            {
                return rewrite();
            }
            finally
            {
                _preferredTargetNamePrefix = previousPrefix;
            }
        }

        private T RewriteWithSuppressedMethodCache<T>(Func<T> rewrite)
        {
            _suppressMethodCacheScopeDepth++;
            try { return rewrite(); }
            finally { _suppressMethodCacheScopeDepth--; }
        }

        protected override ExecutionNode RewriteIf(ExecutionIf node)
        {
            if (_deferMethodTargetDeclarationsDepth == 0)
                return base.RewriteIf(node);

            var condition = RewriteExpression(node.Condition);
            var body = RewriteBlock(node.Body);
            return ReferenceEquals(condition, node.Condition) && ReferenceEquals(body, node.Body)
                ? node
                : node with { Condition = condition, Body = body };
        }

        protected override ExecutionNode RewriteHashProbe(ExecutionHashProbe node)
        {
            if (_deferMethodTargetDeclarationsDepth == 0)
                return base.RewriteHashProbe(node);

            var key = RewriteExpression(node.Key);
            var body = RewriteBlock(node.Body);
            var noMatchBody = RewriteOptionalBlock(node.NoMatchBody);
            return ReferenceEquals(key, node.Key) &&
                   ReferenceEquals(body, node.Body) &&
                   ReferenceEquals(noMatchBody, node.NoMatchBody)
                ? node
                : node with { Key = key, Body = body, NoMatchBody = noMatchBody };
        }

        protected override ExecutionNode RewriteKeySetProbe(ExecutionKeySetProbe node)
        {
            if (_deferMethodTargetDeclarationsDepth == 0)
                return base.RewriteKeySetProbe(node);

            var key = RewriteExpression(node.Key);
            var body = RewriteBlock(node.Body);
            var noMatchBody = RewriteOptionalBlock(node.NoMatchBody);
            return ReferenceEquals(key, node.Key) &&
                   ReferenceEquals(body, node.Body) &&
                   ReferenceEquals(noMatchBody, node.NoMatchBody)
                ? node
                : node with { Key = key, Body = body, NoMatchBody = noMatchBody };
        }

        protected override ExecutionNode RewriteAsOfProbe(ExecutionAsOfProbe node)
        {
            if (_deferMethodTargetDeclarationsDepth == 0)
                return base.RewriteAsOfProbe(node);

            var candidates = RewriteExpression(node.Candidates);
            var equalityKeys = RewriteAsOfEqualityKeys(node.EqualityKeys);
            var probeKey = RewriteExpression(node.ProbeKey);
            var candidateKey = RewriteExpression(node.CandidateKey);
            var body = RewriteBlock(node.Body);
            var noMatchBody = RewriteOptionalBlock(node.NoMatchBody);
            return ReferenceEquals(candidates, node.Candidates) &&
                   ReferenceEquals(equalityKeys, node.EqualityKeys) &&
                   ReferenceEquals(probeKey, node.ProbeKey) &&
                   ReferenceEquals(candidateKey, node.CandidateKey) &&
                   ReferenceEquals(body, node.Body) &&
                   ReferenceEquals(noMatchBody, node.NoMatchBody)
                ? node
                : node with
                    {
                        Candidates = candidates,
                        EqualityKeys = equalityKeys,
                        ProbeKey = probeKey,
                        CandidateKey = candidateKey,
                        Body = body,
                        NoMatchBody = noMatchBody
                    };
        }

        protected override ExecutionNode RewriteRangeProbe(ExecutionRangeProbe node)
        {
            if (_deferMethodTargetDeclarationsDepth == 0)
                return base.RewriteRangeProbe(node);

            var probeKey = RewriteExpression(node.ProbeKey);
            var body = RewriteBlock(node.Body);
            return ReferenceEquals(probeKey, node.ProbeKey) && ReferenceEquals(body, node.Body)
                ? node
                : node with { ProbeKey = probeKey, Body = body };
        }

        protected override ExecutionNode RewriteComputePluginWindow(ExecutionComputePluginWindow node)
        {
            return RewriteWithHelperTargets(
                node.MethodTargets,
                () => (ExecutionComputePluginWindow)base.RewriteComputePluginWindow(node),
                static (rewritten, targets) => rewritten with { MethodTargets = targets });
        }

        protected override ExecutionNode RewriteWindowAggregateKernel(ExecutionWindowAggregateKernel node)
        {
            return RewriteWithHelperTargets(
                node.MethodTargets,
                () => (ExecutionWindowAggregateKernel)base.RewriteWindowAggregateKernel(node),
                static (rewritten, targets) => rewritten with { MethodTargets = targets });
        }

        protected override ExecutionNode RewriteLet(ExecutionLet node)
        {
            if (node.CacheMode == ExecutionLetCacheMode.SuppressMethodCache)
            {
                return RewriteWithSuppressedMethodCache(() => base.RewriteLet(node));
            }

            _valueTypeMethodCacheScopeDepth++;
            try
            {
                return base.RewriteLet(node);
            }
            finally
            {
                _valueTypeMethodCacheScopeDepth--;
            }
        }

        private T RewriteWithHelperTargets<T>(
            IReadOnlyList<ExecutionVariable>? methodTargets,
            Func<T> rewrite,
            Func<T, IReadOnlyList<ExecutionVariable>, T> withTargets)
            where T : ExecutionNode
        {
            var previousRegistry = _currentRegistry;
            var registry = new MethodTargetRegistry(CreateRegistryPrefix(), previousRegistry);
            foreach (var target in methodTargets ?? [])
                registry.AddExisting(target);

            _currentRegistry = registry;
            try
            {
                var rewritten = rewrite();
                if (registry.CreatedTargets.Count == 0)
                    return rewritten;

                var existingTargets = methodTargets ?? [];
                InsertedDeclarations += registry.CreatedTargets.Count;
                return withTargets(rewritten, [..existingTargets, ..registry.CreatedTargets]);
            }
            finally
            {
                _currentRegistry = previousRegistry;
            }
        }

        private MethodTargetRegistry CurrentRegistry =>
            _currentRegistry ?? throw new InvalidOperationException("Method target reuse requires a current Execution IR block registry.");

        private string CreateRegistryPrefix()
        {
            var prefix = string.IsNullOrWhiteSpace(planIdentifier) ? "execution" : planIdentifier;
            return ExecutionSymbolicNamePolicy.CreateLoweringIdentifierCandidate(
                $"{prefix}{_nextRegistryIndex++.ToString(CultureInfo.InvariantCulture)}",
                0);
        }

        private static bool IsMethodTargetScopeBarrier(ExecutionNode node)
        {
            return node is ExecutionStoreTable or ExecutionRelatedCtePhase;
        }

    }
}
