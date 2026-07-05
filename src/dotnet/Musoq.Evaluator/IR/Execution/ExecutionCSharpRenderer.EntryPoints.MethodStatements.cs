using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderMethodStatements(
        ExecutionBlock block,
        ExecutionRenderContext context)
    {
        var session = context.Session;
        var statements = new List<StatementSyntax>();
        var pending = new List<ExecutionNode>();
        var nodes = block.Nodes;
        var valueTupleAggregateHelperIndex = 0;
        var singleKeyAggregateHelperIndex = 0;
        var extractedStoredTableIndexes = new HashSet<int>();
        var hashJoinHelperSets = CollectHashJoinHelperSets(block)
            .Where(helperSet => CanUseHashJoinHelperSetInCurrentSink(helperSet, context))
            .ToArray();
        var keySetHelperSets = CollectKeySetHelperSets(block)
            .Where(helperSet => CanUseKeySetHelperSetInCurrentSink(helperSet, context))
            .ToArray();
        var hashBuildHelpersByIndex = hashJoinHelperSets.ToDictionary(
            static helperSet => helperSet.BuildLoopIndex,
            static helperSet => helperSet.Build);
        var hashProbeHelpersByIndex = hashJoinHelperSets.ToDictionary(
            static helperSet => helperSet.ProbeLoopIndex,
            static helperSet => helperSet.Probe);
        var keySetBuildHelpersByIndex = keySetHelperSets.ToDictionary(
            static helperSet => helperSet.BuildLoopIndex,
            static helperSet => helperSet.Build);
        var keySetProbeHelpersByIndex = keySetHelperSets.ToDictionary(
            static helperSet => helperSet.ProbeLoopIndex,
            static helperSet => helperSet.Probe);
        var windowAppendHelpersByIndex = CollectWindowAppendRowsHelpersWithIndexes(block)
            .Where(item => CanUseWindowAppendRowsHelperInCurrentSink(item.Helper, context))
            .ToDictionary(
            static item => item.Index,
            static item => item.Helper);
        var sortedCopyHelpersByIndex = CollectSortedCopyHelpersWithIndexes(block)
            .Where(static item => item.Helper is not null)
            .Where(item => CanUseSortedCopyHelperInCurrentSink(item.Helper, context))
            .ToDictionary(
            static item => item.Index,
            static item => item.Helper);

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            if (hashBuildHelpersByIndex.TryGetValue(index, out var hashBuildHelper))
            {
                FlushPendingMethodNodes(statements, pending, context);
                statements.Add(CreateHashBuildInvocation(hashBuildHelper, context));
                continue;
            }

            if (hashProbeHelpersByIndex.TryGetValue(index, out var hashProbeHelper))
            {
                FlushPendingMethodNodes(statements, pending, context);
                statements.Add(CreateHashProbeInvocation(hashProbeHelper, context));
                continue;
            }

            if (keySetBuildHelpersByIndex.TryGetValue(index, out var keySetBuildHelper))
            {
                FlushPendingMethodNodes(statements, pending, context);
                statements.Add(CreateKeySetBuildInvocation(keySetBuildHelper, context));
                continue;
            }

            if (keySetProbeHelpersByIndex.TryGetValue(index, out var keySetProbeHelper))
            {
                FlushPendingMethodNodes(statements, pending, context);
                statements.Add(CreateKeySetProbeInvocation(keySetProbeHelper, context));
                continue;
            }

            if (node is not ExecutionStoreTable && IsInsidePendingStoredTableBuild(nodes, index, pending))
            {
                pending.Add(node);
                continue;
            }

            if (node is ExecutionParallelBlock)
            {
                FlushPendingMethodNodes(statements, pending, context);
                statements.AddRange(RenderNode(node, context));
                continue;
            }

            var aggregateHelper = CreateValueTupleAggregateHelper(nodes, index);
            if (aggregateHelper is not null &&
                CanUseAggregateFinalizeHelperInCurrentSink(aggregateHelper.EnsureCapacity.Table.Name, context))
            {
                aggregateHelper = AssignValueTupleAggregateHelperNames(aggregateHelper, valueTupleAggregateHelperIndex);
                valueTupleAggregateHelperIndex++;

                FlushPendingMethodNodes(statements, pending, context);
                statements.AddRange(RenderNode(aggregateHelper.Context, context));
                statements.Add(CreateHelperInvocation(
                    aggregateHelper.PopulateFunctionName,
                    CreateValueTuplePopulateArguments(aggregateHelper, context)));
                statements.Add(CreateHelperInvocation(
                    aggregateHelper.FinalizeFunctionName,
                    CreateValueTupleFinalizeArguments(aggregateHelper)));
                index += 3;
                continue;
            }

            var singleKeyAggregateHelper = CreateSingleKeyHashAggregateHelper(nodes, index);
            if (singleKeyAggregateHelper is not null &&
                CanUseAggregateFinalizeHelperInCurrentSink(singleKeyAggregateHelper.EnsureCapacity.Table.Name, context))
            {
                singleKeyAggregateHelper = AssignSingleKeyAggregateHelperNames(
                    singleKeyAggregateHelper,
                    singleKeyAggregateHelperIndex);
                singleKeyAggregateHelperIndex++;

                FlushPendingMethodNodes(statements, pending, context);
                statements.AddRange(RenderNode(singleKeyAggregateHelper.Context, context));
                statements.Add(CreateHelperInvocationWithArguments(
                    singleKeyAggregateHelper.PopulateFunctionName,
                    CreateSingleKeyPopulateArguments(singleKeyAggregateHelper, context)));
                statements.Add(CreateHelperInvocation(
                    singleKeyAggregateHelper.FinalizeFunctionName,
                    CreateSingleKeyFinalizeArguments(singleKeyAggregateHelper)));
                index += 3;
                continue;
            }

            if (windowAppendHelpersByIndex.TryGetValue(index, out var windowAppendHelper))
            {
                FlushPendingMethodNodes(statements, pending, context);
                statements.Add(CreateWindowAppendRowsInvocation(windowAppendHelper));
                continue;
            }

            if (sortedCopyHelpersByIndex.TryGetValue(index, out var sortedCopyHelper))
            {
                FlushPendingMethodNodes(statements, pending, context);
                statements.Add(CreateSortedCopyInvocation(sortedCopyHelper));
                continue;
            }

            if (node is ExecutionStoreTable store &&
                extractedStoredTableIndexes.Add(store.TableIndex) &&
                TryCreateStoredTableBuild(nodes, index, pending, store, out var storedTableBuild))
            {
                storedTableBuild = storedTableBuild with
                {
                    Captures = CollectStoredTableBuildCaptures(storedTableBuild, context)
                };
                statements.Add(CreateStoredTableBuildInvocation(storedTableBuild, context));
                pending.Clear();
                continue;
            }

            pending.Add(node);
        }

        FlushPendingMethodNodes(statements, pending, context);

        return statements;
    }

    private void FlushPendingMethodNodes(
        List<StatementSyntax> statements,
        List<ExecutionNode> pending,
        ExecutionRenderContext context)
    {
        if (pending.Count == 0)
            return;

        statements.AddRange(RenderBlock(new ExecutionBlock(pending), context).Statements);
        pending.Clear();
    }

    private StatementSyntax[] RenderIsolatedHelperBlock(
        ExecutionBlock block,
        ExecutionRenderContext context,
        bool profileRecorderInScope = false,
        bool emitChunkLoopCancellationChecks = false,
        IEnumerable<StatementSyntax>? trailingStatements = null)
    {
        var session = context.Session;
        var previousDeclaredStoredRowsCaches = session.DeclaredStoredRowsCaches;
        var previousStoredGeneratedRowsLoopNameCounts = session.StoredGeneratedRowsLoopNameCounts;
        var previousProfileRecorderInScope = session.ProfileRecorderInScope;
        var previousEmitChunkLoopCancellationChecks = session.EmitChunkLoopCancellationChecks;
        session.DeclaredStoredRowsCaches = new HashSet<int>(previousDeclaredStoredRowsCaches);
        session.StoredGeneratedRowsLoopNameCounts = [];
        session.ProfileRecorderInScope = profileRecorderInScope;
        session.EmitChunkLoopCancellationChecks = emitChunkLoopCancellationChecks;

        try
        {
            var statements = RenderBlock(block, context).Statements.ToList();
            if (trailingStatements is not null)
                statements.AddRange(trailingStatements);
            return CreateProfiledHelperBody(statements, context).Statements.ToArray();
        }
        finally
        {
            session.DeclaredStoredRowsCaches = previousDeclaredStoredRowsCaches;
            session.StoredGeneratedRowsLoopNameCounts = previousStoredGeneratedRowsLoopNameCounts;
            session.ProfileRecorderInScope = previousProfileRecorderInScope;
            session.EmitChunkLoopCancellationChecks = previousEmitChunkLoopCancellationChecks;
        }
    }

    private static IDisposable SuppressChunkLoopCancellationChecks(ExecutionRenderContext context)
    {
        return new ChunkLoopCancellationCheckScope(context, false);
    }

    private sealed class ChunkLoopCancellationCheckScope : IDisposable
    {
        private readonly ExecutionRenderContext _context;
        private readonly bool _previous;

        public ChunkLoopCancellationCheckScope(
            ExecutionRenderContext context,
            bool enabled)
        {
            _context = context;
            _previous = context.Session.EmitChunkLoopCancellationChecks;
            _context.Session.EmitChunkLoopCancellationChecks = enabled;
        }

        public void Dispose()
        {
            _context.Session.EmitChunkLoopCancellationChecks = _previous;
        }
    }
}
