using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed class TableControlFlowRenderer(ExecutionCSharpRenderer renderer, ExecutionRenderContext renderContext)
    {
        public bool TryRender(ExecutionNode node, out IEnumerable<StatementSyntax> statements) {
            statements = node switch
            {
                ExecutionSourceScan sourceScan => renderer.RenderSourceScan(sourceScan, renderContext),
                ExecutionInterpretSource interpret => renderer.RenderInterpretSource(interpret),
                ExecutionEnumerableSource enumerable => [renderer.RenderEnumerableSource(enumerable)],
                ExecutionCreateTable createTable => renderer.RenderCreateTable(createTable, renderContext),
                ExecutionCreateValuesRows valuesRows => [renderer.RenderCreateValuesRows(valuesRows, renderContext)],
                ExecutionCreateRecordList createList => [renderer.RenderCreateRecordList(createList, renderContext)],
                ExecutionCreateBoundedRecordList createList => [RenderCreateBoundedRecordList(createList)],
                ExecutionEnsureTableCapacity ensureCapacity => [renderer.RenderEnsureTableCapacity(ensureCapacity, renderContext)],
                ExecutionForEach forEach => renderer.RenderForEachStream(forEach, renderContext),
                ExecutionForEachWithOrdinality forEach => renderer.RenderForEachWithOrdinalityStream(forEach, renderContext),
                ExecutionScopedBlock scopedBlock => [renderer.RenderBlock(scopedBlock.Body, renderContext)],
                ExecutionForEachIndexed forEachIndexed => [renderer.RenderForEachIndexed(forEachIndexed, renderContext)],
                ExecutionParallelBlock parallel => renderer.RenderParallelBlock(parallel, renderContext),
                ExecutionFusedCteProducer fusedCte => renderer.RenderFusedCteProducer(fusedCte, renderContext),
                ExecutionLet let => [renderer.RenderLet(let, renderContext)],
                ExecutionAssign assign => [renderer.RenderAssign(assign)],
                ExecutionCreateBooleanArray createArray => [RenderCreateBooleanArray(createArray)],
                ExecutionArrayAssign arrayAssign => [renderer.RenderArrayAssign(arrayAssign)],
                ExecutionContinue => [SyntaxFactory.ContinueStatement()],
                ExecutionContinueIf continueIf => [renderer.RenderContinueIf(continueIf, renderContext)],
                ExecutionBreak => [SyntaxFactory.BreakStatement()],
                ExecutionAdaptExpando adapt => [renderer.RenderAdaptExpando(adapt)],
                ExecutionCreateObject createObject => [RenderCreateObject(createObject)],
                ExecutionIf branch => [renderer.RenderIf(branch, renderContext)],
                ExecutionCreateGeneratedRow createRow => [renderer.RenderCreateGeneratedRow(createRow, renderContext)],
                ExecutionRecursiveCte recursiveCte => renderer.RenderRecursiveCte(recursiveCte, renderContext),
                ExecutionRecursiveCteAppend recursiveAppend => renderer.RenderRecursiveCteAppend(recursiveAppend, renderContext),
                ExecutionRecursiveCteSnapshotRowGuard snapshotGuard => RenderRecursiveCteSnapshotRowGuard(snapshotGuard),
                ExecutionAppendRow appendRow => [renderer.RenderAppendRow(appendRow, renderContext)],
                ExecutionAppendExistingRow appendRow => [renderer.RenderAppendExistingRow(appendRow, renderContext)],
                ExecutionAppendRecord appendRecord => [renderer.RenderAppendRecord(appendRecord)],
                ExecutionMaterializeList materialize => [renderer.RenderMaterializeListStream(materialize, renderContext)],
                ExecutionMaterializeFilteredList materialize => renderer.RenderMaterializeFilteredListStream(materialize, renderContext),
                ExecutionMaterializeExpandoList materialize => renderer.RenderMaterializeExpandoListStream(materialize, renderContext),
                ExecutionSetOperation setOperation => renderer.RenderSetOperation(setOperation, renderContext),
                ExecutionDistinctTable distinct => renderer.RenderDistinctTable(distinct, renderContext),
                ExecutionSortTable sort => renderer.RenderSortTable(sort, renderContext),
                ExecutionTopNTable topN => renderer.RenderTopNTable(topN, renderContext),
                ExecutionTopOffsetTable topOffset => renderer.RenderTopOffsetTable(topOffset, renderContext),
                ExecutionSkipTable skip => renderer.RenderSkipTable(skip, renderContext),
                ExecutionTakeTable take => renderer.RenderTakeTable(take, renderContext),
                ExecutionSliceTable slice => renderer.RenderSliceTable(slice, renderContext),
                ExecutionProjectTable project => renderer.RenderProjectTable(project, renderContext),
                ExecutionOrderRecordList orderRecords => [RenderOrderRecordList(orderRecords)],
                ExecutionMaterializeRecordListToTable materializeRecords => renderer.RenderMaterializeRecordListToTable(materializeRecords, renderContext),
                ExecutionStoreTable store => [renderer.RenderStoreTable(store, renderContext)],
                ExecutionPhaseBoundary boundary => renderer.RenderPhaseBoundary(boundary, renderContext),
                ExecutionStoreCteIndex storeCteIndex => [RenderStoreCteIndex(storeCteIndex)],
                ExecutionLoadCteIndex loadCteIndex => [RenderLoadCteIndex(loadCteIndex)],
                ExecutionRelatedCtePhase phase => [QueryEmitter.GeneratePhaseChangeStatement($"{renderContext.Session.QueryIdentifier}:cte{phase.TableIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}", QueryPhase.Begin)],
                ExecutionReturnDesc desc => renderer.RenderReturnDesc(desc, renderContext),
                ExecutionReturnTable returnTable => [StatementEmitter.CreateReturn(SyntaxFactory.IdentifierName(returnTable.Table.Name))],
                _ => null!
            };

            return statements != null;
        }
    }
}
