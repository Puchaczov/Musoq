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
        private static readonly ExecutionTypeRef PreparedMatcherType =
            ExecutionClrBindingFactory.FromClr(typeof(PreparedLikeMatcher));
        private static readonly ExecutionTypeRef MatcherCacheSlotType =
            ExecutionClrBindingFactory.FromClr(typeof(LikeMatcherCacheSlot));

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
            $"Lowered LIKE expressions: direct={_directCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"prepared={_preparedCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"dynamic={_dynamicCount.ToString(CultureInfo.InvariantCulture)}; " +
            $"loop-invariant prepared={_loopInvariantPreparedCount.ToString(CultureInfo.InvariantCulture)}; " +
            $"cache slots: serial={_serialCacheCount.ToString(CultureInfo.InvariantCulture)}, " +
            $"parallel-worker={_workerCacheCount.ToString(CultureInfo.InvariantCulture)}.";

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
            if (rewritten.Kind != PatternKind.Like)
                return rewritten;

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
                    GetPreparedMatcher(literal),
                    rewritten.ReturnType);
            }

            if (TryGetLoopInvariantMatcher(rewritten.Expression, rewritten.Pattern, out var matcher))
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
                GetCacheSlot(),
                ExecutionStringMatchComparison.LikeIgnoreCase,
                rewritten.ReturnType);
        }

        private ExecutionVariableRead GetPreparedMatcher(ExecutionLiteral pattern)
        {
            var key = pattern.Value.ToClrValue() as string;
            if (key is null)
            {
                if (_rootRegion.NullMatcher is { } nullMatcher)
                    return new ExecutionVariableRead(nullMatcher);
            }
            else if (_rootRegion.PreparedMatchers.TryGetValue(key, out var existing))
            {
                return new ExecutionVariableRead(existing);
            }

            var variable = CreateVariable("__likeMatcher", PreparedMatcherType);
            if (key is null)
                _rootRegion.NullMatcher = variable;
            else
                _rootRegion.PreparedMatchers.Add(key, variable);
            _rootRegion.Prelude.Add(new ExecutionLet(
                variable,
                new ExecutionPrepareLikeMatcher(
                    pattern,
                    ExecutionStringMatchComparison.LikeIgnoreCase,
                    PreparedMatcherType),
                ExecutionLetCacheMode.SuppressMethodCache));
            return new ExecutionVariableRead(variable);
        }

        private ExecutionVariableRead GetCacheSlot()
        {
            if (_currentCacheScope.Variable is not { } variable)
            {
                variable = CreateVariable("__likeCache", MatcherCacheSlotType);
                _currentCacheScope.Variable = variable;
                _currentCacheScope.Region.Prelude.Add(new ExecutionLet(
                    variable,
                    new ExecutionLikeMatcherCacheSlot(
                        _currentCacheScope.WorkerLocal,
                        MatcherCacheSlotType),
                    ExecutionLetCacheMode.SuppressMethodCache));
                if (_currentCacheScope.WorkerLocal)
                    _workerCacheCount++;
                else
                    _serialCacheCount++;
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

        private sealed class Region
        {
            public List<ExecutionNode> Prelude { get; } = [];

            public Dictionary<string, ExecutionVariable> PreparedMatchers { get; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, ExecutionVariable> LoopInvariantMatchers { get; } =
                new(StringComparer.Ordinal);

            public List<MatcherPreparation> MatcherPreparations { get; } = [];

            public ExecutionVariable? NullMatcher { get; set; }
        }

        private sealed class CacheScope(Region region, bool workerLocal)
        {
            public Region Region { get; } = region;

            public bool WorkerLocal { get; } = workerLocal;

            public ExecutionVariable? Variable { get; set; }
        }

    }
}
