using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal sealed partial class LikeStrategyLoweringPass
{
    private sealed partial class Rewriter
    {
        public override ExecutionNode RewriteNode(ExecutionNode node)
        {
            if (node is not ExecutionScopedBlock scopedBlock)
                return base.RewriteNode(node);

            return RewriteWithMatcherHoistingDisabled(() =>
            {
                var body = RewriteBlock(scopedBlock.Body);
                return ReferenceEquals(body, scopedBlock.Body)
                    ? scopedBlock
                    : scopedBlock with { Body = body };
            });
        }

        protected override ExecutionNode RewriteForEach(ExecutionForEach node)
        {
            var source = RewriteExpression(node.Source);
            var frame = new LoopFrame(node.Item);
            _loopPath.Add(frame);
            try
            {
                var body = InsertMatcherPreparations(RewriteBlock(node.Body), frame.Region);
                return ReferenceEquals(source, node.Source) && ReferenceEquals(body, node.Body)
                    ? node
                    : node with { Source = source, Body = body };
            }
            finally
            {
                _loopPath.RemoveAt(_loopPath.Count - 1);
            }
        }

        protected override ExecutionNode RewriteForEachWithOrdinality(ExecutionForEachWithOrdinality node)
        {
            var source = RewriteExpression(node.Source);
            var frame = new LoopFrame(node.Item, node.Ordinal);
            _loopPath.Add(frame);
            try
            {
                var body = InsertMatcherPreparations(RewriteBlock(node.Body), frame.Region);
                return ReferenceEquals(source, node.Source) && ReferenceEquals(body, node.Body)
                    ? node
                    : node with { Source = source, Body = body };
            }
            finally
            {
                _loopPath.RemoveAt(_loopPath.Count - 1);
            }
        }

        protected override ExecutionNode RewriteForEachIndexed(ExecutionForEachIndexed node)
        {
            var frame = new LoopFrame(node.Item, node.Index, node.Source);
            _loopPath.Add(frame);
            try
            {
                var body = InsertMatcherPreparations(RewriteBlock(node.Body), frame.Region);
                return ReferenceEquals(body, node.Body) ? node : node with { Body = body };
            }
            finally
            {
                _loopPath.RemoveAt(_loopPath.Count - 1);
            }
        }

        protected override ExecutionNode RewriteLet(ExecutionLet node)
        {
            var rewritten = (ExecutionLet)base.RewriteLet(node);
            if (_loopPath.Count > 0)
                _variableOwners[rewritten.Variable.Name] = _loopPath[^1];
            return rewritten;
        }

        protected override ExecutionNode RewriteIf(ExecutionIf node)
        {
            var condition = RewriteExpression(node.Condition);
            _matcherHoistingDisabledDepth++;
            try
            {
                var body = RewriteBlock(node.Body);
                return ReferenceEquals(condition, node.Condition) && ReferenceEquals(body, node.Body)
                    ? node
                    : node with { Condition = condition, Body = body };
            }
            finally
            {
                _matcherHoistingDisabledDepth--;
            }
        }

        protected override ExecutionNode RewriteRecursiveCte(ExecutionRecursiveCte node) =>
            RewriteWithMatcherHoistingDisabled(() => base.RewriteRecursiveCte(node));

        protected override ExecutionNode RewriteMaterializeList(ExecutionMaterializeList node) =>
            RewriteWithMatcherHoistingDisabled(() => base.RewriteMaterializeList(node));

        protected override ExecutionNode RewriteMaterializeFilteredList(ExecutionMaterializeFilteredList node) =>
            RewriteWithMatcherHoistingDisabled(() => base.RewriteMaterializeFilteredList(node));

        protected override ExecutionNode RewriteMaterializeExpandoList(ExecutionMaterializeExpandoList node) =>
            RewriteWithMatcherHoistingDisabled(() => base.RewriteMaterializeExpandoList(node));

        protected override ExecutionNode RewriteWindowKernelPlan(ExecutionWindowKernelPlan node) =>
            RewriteWithMatcherHoistingDisabled(() => base.RewriteWindowKernelPlan(node));

        private bool TryGetLoopInvariantMatcher(
            ExecutionExpression input,
            ExecutionExpression pattern,
            PatternKind kind,
            out ExecutionVariableRead matcher)
        {
            matcher = null!;
            if (_matcherHoistingDisabledDepth != 0 ||
                !IsEligibleStablePattern(pattern) ||
                ContainsConditionalExpression(pattern))
            {
                return false;
            }

            var inputOwnerIndex = FindDeepestOwnerIndex(input);
            var patternOwnerIndex = FindDeepestOwnerIndex(pattern);
            if (inputOwnerIndex < 0 || patternOwnerIndex >= inputOwnerIndex)
                return false;

            Region region;
            if (patternOwnerIndex < 0)
            {
                if (pattern is not (ExecutionLiteral or ExecutionScriptParameterRead or ExecutionScriptVariableRead))
                    return false;
                region = _rootRegion;
            }
            else
            {
                region = _loopPath[patternOwnerIndex].Region;
            }

            var signature = ExecutionExpressionFingerprint.ForHoist(pattern);
            var loopInvariantMatchers = kind == PatternKind.Like
                ? region.LoopInvariantLikeMatchers
                : region.LoopInvariantRLikeMatchers;
            if (!loopInvariantMatchers.TryGetValue(signature, out var variable))
            {
                variable = CreateVariable(
                    kind == PatternKind.Like ? "__likeMatcher" : "__rlikeMatcher",
                    PreparedMatcherType(kind));
                loopInvariantMatchers.Add(signature, variable);
                region.MatcherPreparations.Add(new MatcherPreparation(
                    new ExecutionLet(
                        variable,
                        CreatePrepareMatcher(pattern, kind),
                        ExecutionLetCacheMode.SuppressMethodCache),
                    GetDependencyNames(pattern)));
            }

            matcher = new ExecutionVariableRead(variable);
            return true;
        }

        private int FindDeepestOwnerIndex(ExecutionExpression expression)
        {
            var deepest = -1;
            foreach (var dependency in GetDependencyNames(expression))
            {
                for (var index = _loopPath.Count - 1; index >= 0; index--)
                {
                    if (_loopPath[index].Names.Contains(dependency, StringComparer.OrdinalIgnoreCase) ||
                        (_variableOwners.TryGetValue(dependency, out var owner) &&
                         ReferenceEquals(owner, _loopPath[index])))
                    {
                        deepest = Math.Max(deepest, index);
                        break;
                    }
                }
            }

            return deepest;
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
                }
            }

            return dependencies;
        }

        private static bool IsEligibleStablePattern(ExecutionExpression expression)
        {
            return expression switch
            {
                ExecutionLiteral => true,
                ExecutionScriptParameterRead => true,
                ExecutionScriptVariableRead => true,
                ExecutionVariableRead => true,
                ExecutionFieldRead { Stability: ColumnStability.Stable } => true,
                ExecutionMemberRead { IsDynamic: false } memberRead =>
                    ExpressionStabilityAnalyzer.IsStable(memberRead) && IsEligibleStablePattern(memberRead.Receiver),
                _ => false
            };
        }

        private static bool ContainsConditionalExpression(ExecutionExpression expression) =>
            ExecutionIrAnalysis.FlattenExpressions(expression).Any(static current =>
                current is ExecutionBinary { Kind: BinaryOpKind.And or BinaryOpKind.Or } or
                    ExecutionCaseWhen or ExecutionCoalesce);

        private static ExecutionBlock InsertMatcherPreparations(ExecutionBlock body, Region region)
        {
            if (region.MatcherPreparations.Count == 0)
                return body;

            var localDeclarationIndexes = body.Nodes
                .Select((node, index) => (node, index))
                .Where(static item => item.node is ExecutionLet)
                .ToDictionary(
                    static item => ((ExecutionLet)item.node).Variable.Name,
                    static item => item.index,
                    StringComparer.OrdinalIgnoreCase);
            var minimumIndex = FindLoopPreludeInsertionIndex(body.Nodes);
            var preparationsByIndex = new SortedDictionary<int, List<ExecutionLet>>();
            foreach (var preparation in region.MatcherPreparations)
            {
                var insertionIndex = minimumIndex;
                foreach (var dependency in preparation.Dependencies)
                {
                    if (localDeclarationIndexes.TryGetValue(dependency, out var dependencyIndex))
                        insertionIndex = Math.Max(insertionIndex, dependencyIndex + 1);
                }

                if (!preparationsByIndex.TryGetValue(insertionIndex, out var preparations))
                {
                    preparations = [];
                    preparationsByIndex.Add(insertionIndex, preparations);
                }

                preparations.Add(preparation.Let);
            }

            var rewritten = new List<ExecutionNode>(body.Nodes.Count + region.MatcherPreparations.Count);
            for (var index = 0; index <= body.Nodes.Count; index++)
            {
                if (preparationsByIndex.TryGetValue(index, out var preparations))
                    rewritten.AddRange(preparations);
                if (index < body.Nodes.Count)
                    rewritten.Add(body.Nodes[index]);
            }

            return body with { Nodes = rewritten };
        }

        private static int FindLoopPreludeInsertionIndex(IReadOnlyList<ExecutionNode> nodes)
        {
            var index = 0;
            while (index < nodes.Count && nodes[index] is ExecutionAdaptExpando or ExecutionMethodTargetDeclarationCandidate)
                index++;
            return index;
        }

        private T RewriteWithMatcherHoistingDisabled<T>(Func<T> rewrite)
        {
            _matcherHoistingDisabledDepth++;
            try
            {
                return rewrite();
            }
            finally
            {
                _matcherHoistingDisabledDepth--;
            }
        }

        private sealed record MatcherPreparation(
            ExecutionLet Let,
            IReadOnlySet<string> Dependencies);

        private sealed class LoopFrame
        {
            public LoopFrame(
                ExecutionVariable item,
                ExecutionVariable? ordinal = null,
                ExecutionVariable? source = null)
            {
                var names = new List<string> { item.Name };
                if (ordinal != null)
                    names.Add(ordinal.Name);
                if (source != null)
                    names.Add(source.Name);
                Names = names;
            }

            public IReadOnlyList<string> Names { get; }

            public Region Region { get; } = new();
        }
    }
}
