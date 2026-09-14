using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed partial class LikeStrategyLoweringPass : IExecutionIrOptimizationPass
{
    public string Name => "LikeStrategyLowering";

    public OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        var rewriter = new Rewriter(plan);
        var optimized = rewriter.RewritePlan(plan);
        var details = rewriter.FormatTrace();
        return ReferenceEquals(optimized, plan)
            ? OptimizationResult<ExecutionPlan>.NoChange(plan, details)
            : OptimizationResult<ExecutionPlan>.Changed(optimized, details);
    }

    private sealed partial class Rewriter : ExecutionIrRewriter
    {
        private static readonly ExecutionTypeRef PreparedLikeMatcherType =
            ExecutionClrBindingFactory.FromClr(typeof(PreparedLikeMatcher));
        private static readonly ExecutionTypeRef LikeMatcherCacheSlotType =
            ExecutionClrBindingFactory.FromClr(typeof(LikeMatcherCacheSlot));
        private static readonly ExecutionTypeRef PreparedRLikeMatcherType =
            ExecutionClrBindingFactory.FromClr(typeof(PreparedRLikeMatcher));
        private static readonly ExecutionTypeRef RLikeMatcherCacheSlotType =
            ExecutionClrBindingFactory.FromClr(typeof(RLikeMatcherCacheSlot));

        private readonly HashSet<string> _usedVariableNames;
        private readonly Dictionary<string, LoopFrame> _variableOwners = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<LoopFrame> _loopPath = [];
        private readonly Region _rootRegion;
        private Region _currentRegion;
        private CacheScope _currentCacheScope;
        private int _matcherHoistingDisabledDepth;
        private int _directCount;
        private int _preparedCount;
        private int _dynamicCount;
        private int _loopInvariantPreparedCount;
        private int _serialCacheCount;
        private int _workerCacheCount;
        private int _rLikeDirectCount;
        private int _rLikePreparedCount;
        private int _rLikeDynamicCount;
        private int _rLikeLoopInvariantPreparedCount;
        private int _rLikeSerialCacheCount;
        private int _rLikeWorkerCacheCount;
        private readonly Dictionary<RLikeLiteralRejectionReason, int> _rLikeFallbackReasons = [];

        public Rewriter(ExecutionPlan plan)
        {
            _usedVariableNames = new HashSet<string>(
                ExecutionIrAnalysis.CollectDeclaredVariableNames(plan.Body),
                StringComparer.Ordinal);
            _rootRegion = new Region();
            _currentRegion = _rootRegion;
            _currentCacheScope = new CacheScope(_currentRegion, workerLocal: false);
        }

        public string FormatTrace() =>
            $"Lowered pattern matches: LIKE(direct={_directCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"prepared={_preparedCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"dynamic={_dynamicCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"loop-invariant={_loopInvariantPreparedCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"serial-cache={_serialCacheCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"worker-cache={_workerCacheCount.ToString(CultureInfo.InvariantCulture)}); " +
            $"RLIKE(prepared={_rLikePreparedCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"dynamic={_rLikeDynamicCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"loop-invariant={_rLikeLoopInvariantPreparedCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"direct={_rLikeDirectCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"serial-cache={_rLikeSerialCacheCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"worker-cache={_rLikeWorkerCacheCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"fallbacks=[{FormatRLikeFallbacks()}]).";

        public override ExecutionPlan RewritePlan(ExecutionPlan plan)
        {
            var body = RewriteBlock(plan.Body);
            body = PrependPrelude(body, _currentRegion);
            return ReferenceEquals(body, plan.Body)
                ? plan
                : plan with { Body = body };
        }

        protected override ExecutionParallelTask RewriteParallelTask(ExecutionParallelTask task)
        {
            var previousRegion = _currentRegion;
            var previousCacheScope = _currentCacheScope;
            var taskRegion = new Region();
            _currentRegion = taskRegion;
            _currentCacheScope = new CacheScope(taskRegion, workerLocal: false);
            _matcherHoistingDisabledDepth++;

            try
            {
                var body = PrependPrelude(RewriteBlock(task.Body), taskRegion);
                return ReferenceEquals(body, task.Body) ? task : task with { Body = body };
            }
            finally
            {
                _currentRegion = previousRegion;
                _currentCacheScope = previousCacheScope;
                _matcherHoistingDisabledDepth--;
            }
        }

        protected override ExecutionNode RewriteParallelFilterProjectLoop(ExecutionParallelFilterProjectLoop node)
        {
            var sourceRows = RewriteExpression(node.SourceRows);
            var previousCacheScope = _currentCacheScope;
            _currentCacheScope = new CacheScope(_currentRegion, workerLocal: true);

            try
            {
                var predicate = RewriteOptionalExpression(node.Predicate);
                var appendRow = (ExecutionAppendRow)RewriteAppendRow(node.AppendRow);
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
            }
            finally
            {
                _currentCacheScope = previousCacheScope;
            }
        }

        protected override ExecutionNode RewriteParallelSingleKeyAggregateLoop(
            ExecutionParallelSingleKeyAggregateLoop node)
        {
            var sourceRows = RewriteExpression(node.SourceRows);
            var previousCacheScope = _currentCacheScope;
            _currentCacheScope = new CacheScope(_currentRegion, workerLocal: true);

            try
            {
                var key = RewriteExpression(node.Key);
                var aggregateBody = RewriteBlock(node.AggregateBody);
                return ReferenceEquals(sourceRows, node.SourceRows) &&
                       ReferenceEquals(key, node.Key) &&
                       ReferenceEquals(aggregateBody, node.AggregateBody)
                    ? node
                    : node with
                    {
                        SourceRows = sourceRows,
                        Key = key,
                        AggregateBody = aggregateBody
                    };
            }
            finally
            {
                _currentCacheScope = previousCacheScope;
            }
        }

        protected override ExecutionExpression RewritePatternMatch(ExecutionPatternMatch expression)
        {
            var rewritten = (ExecutionPatternMatch)base.RewritePatternMatch(expression);
            return rewritten.Kind switch
            {
                PatternKind.Like => LowerLike(rewritten),
                PatternKind.RLike => LowerRLike(rewritten),
                _ => rewritten
            };
        }

        private ExecutionExpression LowerLike(ExecutionPatternMatch rewritten)
        {
            if (rewritten.Pattern is ExecutionLiteral literal)
            {
                if (literal.Value.ToClrValue() is string pattern)
                {
                    var classification = LikePatternClassifier.Classify(pattern).Classification;
                    if (classification is { Domain: LikePatternCharacterDomain.Ascii } match)
                    {
                        _directCount++;
                        return new ExecutionStringMatch(
                            rewritten.Expression,
                            pattern,
                            pattern.Substring(match.LiteralStart, match.LiteralLength),
                            ToExecutionKind(match.Kind),
                            ExecutionStringMatchComparison.LikeIgnoreCase,
                            rewritten.ReturnType);
                    }
                }

                _preparedCount++;
                return new ExecutionPreparedLikeMatch(
                    rewritten.Expression,
                    GetPreparedMatcher(literal, PatternKind.Like),
                    rewritten.ReturnType);
            }

            if (TryGetLoopInvariantMatcher(rewritten.Expression, rewritten.Pattern, PatternKind.Like, out var matcher))
            {
                _preparedCount++;
                _loopInvariantPreparedCount++;
                return new ExecutionPreparedLikeMatch(
                    rewritten.Expression,
                    matcher,
                    rewritten.ReturnType);
            }

            _dynamicCount++;
            return new ExecutionDynamicLikeMatch(
                rewritten.Expression,
                rewritten.Pattern,
                GetCacheSlot(PatternKind.Like),
                ExecutionStringMatchComparison.LikeIgnoreCase,
                rewritten.ReturnType);
        }

        private ExecutionExpression LowerRLike(ExecutionPatternMatch rewritten)
        {
            if (rewritten.Pattern is ExecutionLiteral literal)
            {
                if (literal.Value.ToClrValue() is string pattern)
                {
                    var result = RLikeLiteralPatternClassifier.Classify(pattern);
                    if (result.Classification is { } match)
                    {
                        _rLikeDirectCount++;
                        return new ExecutionStringMatch(
                            rewritten.Expression,
                            pattern,
                            pattern.Substring(match.LiteralStart, match.LiteralLength),
                            ToExecutionKind(match.Kind),
                            ExecutionStringMatchComparison.Ordinal,
                            rewritten.ReturnType);
                    }

                    _rLikeFallbackReasons.TryGetValue(result.RejectionReason, out var count);
                    _rLikeFallbackReasons[result.RejectionReason] = count + 1;
                }

                _rLikePreparedCount++;
                return new ExecutionPreparedRLikeMatch(
                    rewritten.Expression,
                    GetPreparedMatcher(literal, PatternKind.RLike),
                    rewritten.ReturnType);
            }

            if (TryGetLoopInvariantMatcher(rewritten.Expression, rewritten.Pattern, PatternKind.RLike, out var matcher))
            {
                _rLikePreparedCount++;
                _rLikeLoopInvariantPreparedCount++;
                return new ExecutionPreparedRLikeMatch(
                    rewritten.Expression,
                    matcher,
                    rewritten.ReturnType);
            }

            _rLikeDynamicCount++;
            return new ExecutionDynamicRLikeMatch(
                rewritten.Expression,
                rewritten.Pattern,
                GetCacheSlot(PatternKind.RLike),
                rewritten.ReturnType);
        }

        private ExecutionVariableRead GetPreparedMatcher(ExecutionLiteral pattern, PatternKind kind)
        {
            var key = pattern.Value.ToClrValue() as string;
            var preparedMatchers = kind == PatternKind.Like
                ? _rootRegion.PreparedLikeMatchers
                : _rootRegion.PreparedRLikeMatchers;
            if (key is null)
            {
                var nullMatcher = kind == PatternKind.Like
                    ? _rootRegion.NullLikeMatcher
                    : _rootRegion.NullRLikeMatcher;
                if (nullMatcher is not null)
                    return new ExecutionVariableRead(nullMatcher);
            }
            else if (preparedMatchers.TryGetValue(key, out var existing))
            {
                return new ExecutionVariableRead(existing);
            }

            var variable = CreateVariable(
                kind == PatternKind.Like ? "__likeMatcher" : "__rlikeMatcher",
                PreparedMatcherType(kind));
            if (key is null)
            {
                if (kind == PatternKind.Like)
                    _rootRegion.NullLikeMatcher = variable;
                else
                    _rootRegion.NullRLikeMatcher = variable;
            }
            else
            {
                preparedMatchers.Add(key, variable);
            }
            _rootRegion.Prelude.Add(new ExecutionLet(
                variable,
                CreatePrepareMatcher(pattern, kind),
                ExecutionLetCacheMode.SuppressMethodCache));
            return new ExecutionVariableRead(variable);
        }

        private ExecutionVariableRead GetCacheSlot(PatternKind kind)
        {
            var variable = kind == PatternKind.Like
                ? _currentCacheScope.LikeVariable
                : _currentCacheScope.RLikeVariable;
            if (variable is null)
            {
                variable = CreateVariable(
                    kind == PatternKind.Like ? "__likeCache" : "__rlikeCache",
                    MatcherCacheSlotType(kind));
                if (kind == PatternKind.Like)
                    _currentCacheScope.LikeVariable = variable;
                else
                    _currentCacheScope.RLikeVariable = variable;
                _currentCacheScope.Region.Prelude.Add(new ExecutionLet(
                    variable,
                    CreateMatcherCacheSlot(kind, _currentCacheScope.WorkerLocal),
                    ExecutionLetCacheMode.SuppressMethodCache));
                IncrementCacheCount(kind, _currentCacheScope.WorkerLocal);
            }

            return new ExecutionVariableRead(variable);
        }

        private ExecutionVariable CreateVariable(string prefix, ExecutionTypeRef type)
        {
            for (var suffix = 0;; suffix++)
            {
                var name = string.Concat(prefix, suffix.ToString(CultureInfo.InvariantCulture));
                if (_usedVariableNames.Add(name))
                    return new ExecutionVariable(name, type);
            }
        }

        private static ExecutionBlock PrependPrelude(ExecutionBlock body, Region region)
        {
            if (region.Prelude.Count == 0 && region.MatcherPreparations.Count == 0)
                return body;

            return new ExecutionBlock(
                region.Prelude
                    .Concat(region.MatcherPreparations.Select(static preparation => preparation.Let))
                    .Concat(body.Nodes)
                    .ToArray());
        }

        private static ExecutionStringMatchKind ToExecutionKind(LikePatternMatchKind kind) => kind switch
        {
            LikePatternMatchKind.Exact => ExecutionStringMatchKind.Exact,
            LikePatternMatchKind.Prefix => ExecutionStringMatchKind.Prefix,
            LikePatternMatchKind.Suffix => ExecutionStringMatchKind.Suffix,
            LikePatternMatchKind.Contains => ExecutionStringMatchKind.Contains,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown LIKE match kind.")
        };

        private static ExecutionStringMatchKind ToExecutionKind(RLikeLiteralMatchKind kind) => kind switch
        {
            RLikeLiteralMatchKind.Exact => ExecutionStringMatchKind.Exact,
            RLikeLiteralMatchKind.Prefix => ExecutionStringMatchKind.Prefix,
            RLikeLiteralMatchKind.Suffix => ExecutionStringMatchKind.Suffix,
            RLikeLiteralMatchKind.Contains => ExecutionStringMatchKind.Contains,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown literal RLIKE match kind.")
        };

        private string FormatRLikeFallbacks() => _rLikeFallbackReasons.Count == 0
            ? "none"
            : string.Join(
                ",",
                _rLikeFallbackReasons
                    .OrderBy(static pair => pair.Key)
                    .Select(static pair => $"{pair.Key}={pair.Value.ToString(CultureInfo.InvariantCulture)}"));

        private static ExecutionExpression CreatePrepareMatcher(ExecutionExpression pattern, PatternKind kind) =>
            kind switch
            {
                PatternKind.Like => new ExecutionPrepareLikeMatcher(
                    pattern,
                    ExecutionStringMatchComparison.LikeIgnoreCase,
                    PreparedLikeMatcherType),
                PatternKind.RLike => new ExecutionPrepareRLikeMatcher(pattern, PreparedRLikeMatcherType),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown prepared matcher kind.")
            };

        private static ExecutionExpression CreateMatcherCacheSlot(PatternKind kind, bool workerLocal) =>
            kind switch
            {
                PatternKind.Like => new ExecutionLikeMatcherCacheSlot(workerLocal, LikeMatcherCacheSlotType),
                PatternKind.RLike => new ExecutionRLikeMatcherCacheSlot(workerLocal, RLikeMatcherCacheSlotType),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown matcher cache kind.")
            };

        private static ExecutionTypeRef PreparedMatcherType(PatternKind kind) =>
            kind == PatternKind.Like ? PreparedLikeMatcherType : PreparedRLikeMatcherType;

        private static ExecutionTypeRef MatcherCacheSlotType(PatternKind kind) =>
            kind == PatternKind.Like ? LikeMatcherCacheSlotType : RLikeMatcherCacheSlotType;

        private void IncrementCacheCount(PatternKind kind, bool workerLocal)
        {
            if (kind == PatternKind.Like)
            {
                if (workerLocal)
                    _workerCacheCount++;
                else
                    _serialCacheCount++;
                return;
            }

            if (workerLocal)
                _rLikeWorkerCacheCount++;
            else
                _rLikeSerialCacheCount++;
        }

        private sealed class Region
        {
            public List<ExecutionNode> Prelude { get; } = [];

            public Dictionary<string, ExecutionVariable> PreparedLikeMatchers { get; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, ExecutionVariable> PreparedRLikeMatchers { get; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, ExecutionVariable> LoopInvariantLikeMatchers { get; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, ExecutionVariable> LoopInvariantRLikeMatchers { get; } =
                new(StringComparer.Ordinal);

            public List<MatcherPreparation> MatcherPreparations { get; } = [];

            public ExecutionVariable? NullLikeMatcher { get; set; }

            public ExecutionVariable? NullRLikeMatcher { get; set; }
        }

        private sealed class CacheScope(Region region, bool workerLocal)
        {
            public Region Region { get; } = region;

            public bool WorkerLocal { get; } = workerLocal;

            public ExecutionVariable? LikeVariable { get; set; }

            public ExecutionVariable? RLikeVariable { get; set; }
        }

    }
}
