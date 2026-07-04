using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed class TableControlFlowRenderer(ExecutionCSharpRenderer renderer, ExecutionRenderContext renderContext)
    {
        public bool TryRender(ExecutionNode node, out IEnumerable<StatementSyntax> statements) {
            statements = node switch
            {
                ExecutionSourceScan sourceScan => renderer.RenderSourceScan(sourceScan),
                ExecutionInterpretSource interpret => renderer.RenderInterpretSource(interpret),
                ExecutionEnumerableSource enumerable => [renderer.RenderEnumerableSource(enumerable)],
                ExecutionCreateTable createTable => renderer.RenderCreateTable(createTable),
                ExecutionCreateValuesRows valuesRows => [renderer.RenderCreateValuesRows(valuesRows)],
                ExecutionCreateRecordList createList => [renderer.RenderCreateRecordList(createList)],
                ExecutionCreateBoundedRecordList createList => [ExecutionCSharpRenderer.RenderCreateBoundedRecordList(createList)],
                ExecutionEnsureTableCapacity ensureCapacity => [renderer.RenderEnsureTableCapacity(ensureCapacity)],
                ExecutionForEach forEach => renderer.RenderForEachStream(forEach, renderContext),
                ExecutionForEachWithOrdinality forEach => renderer.RenderForEachWithOrdinalityStream(forEach, renderContext),
                ExecutionScopedBlock scopedBlock => [renderer.RenderBlock(scopedBlock.Body, renderContext)],
                ExecutionForEachIndexed forEachIndexed => [renderer.RenderForEachIndexed(forEachIndexed, renderContext)],
                ExecutionParallelBlock parallel => renderer.RenderParallelBlock(parallel, renderContext),
                ExecutionFusedCteProducer fusedCte => renderer.RenderFusedCteProducer(fusedCte, renderContext),
                ExecutionLet let => [renderer.RenderLet(let)],
                ExecutionAssign assign => [renderer.RenderAssign(assign)],
                ExecutionCreateBooleanArray createArray => [ExecutionCSharpRenderer.RenderCreateBooleanArray(createArray)],
                ExecutionArrayAssign arrayAssign => [renderer.RenderArrayAssign(arrayAssign)],
                ExecutionContinue => [SyntaxFactory.ContinueStatement()],
                ExecutionContinueIf continueIf => [renderer.RenderContinueIf(continueIf)],
                ExecutionBreak => [SyntaxFactory.BreakStatement()],
                ExecutionAdaptExpando adapt => [ExecutionCSharpRenderer.RenderAdaptExpando(adapt)],
                ExecutionCreateObject createObject => [ExecutionCSharpRenderer.RenderCreateObject(createObject)],
                ExecutionIf branch => [renderer.RenderIf(branch, renderContext)],
                ExecutionCreateGeneratedRow createRow => [renderer.RenderCreateGeneratedRow(createRow)],
                ExecutionAppendRow appendRow => [renderer.RenderAppendRow(appendRow)],
                ExecutionAppendExistingRow appendRow => [renderer.RenderAppendExistingRow(appendRow)],
                ExecutionAppendRecord appendRecord => [renderer.RenderAppendRecord(appendRecord)],
                ExecutionMaterializeList materialize => [renderer.RenderMaterializeListStream(materialize)],
                ExecutionMaterializeFilteredList materialize => renderer.RenderMaterializeFilteredListStream(materialize),
                ExecutionMaterializeExpandoList materialize => renderer.RenderMaterializeExpandoListStream(materialize),
                ExecutionSetOperation setOperation => renderer.RenderSetOperation(setOperation),
                ExecutionDistinctTable distinct => renderer.RenderDistinctTable(distinct),
                ExecutionSortTable sort => renderer.RenderSortTable(sort),
                ExecutionTopNTable topN => renderer.RenderTopNTable(topN),
                ExecutionTopOffsetTable topOffset => renderer.RenderTopOffsetTable(topOffset),
                ExecutionSkipTable skip => renderer.RenderSkipTable(skip),
                ExecutionTakeTable take => renderer.RenderTakeTable(take),
                ExecutionSliceTable slice => renderer.RenderSliceTable(slice),
                ExecutionProjectTable project => renderer.RenderProjectTable(project),
                ExecutionOrderRecordList orderRecords => [ExecutionCSharpRenderer.RenderOrderRecordList(orderRecords)],
                ExecutionMaterializeRecordListToTable materializeRecords => renderer.RenderMaterializeRecordListToTable(materializeRecords),
                ExecutionStoreTable store => [renderer.RenderStoreTable(store)],
                ExecutionStoreCteIndex storeCteIndex => [ExecutionCSharpRenderer.RenderStoreCteIndex(storeCteIndex)],
                ExecutionLoadCteIndex loadCteIndex => [ExecutionCSharpRenderer.RenderLoadCteIndex(loadCteIndex)],
                ExecutionRelatedCtePhase => [],
                ExecutionReturnDesc desc => renderer.RenderReturnDesc(desc),
                ExecutionReturnTable returnTable => [StatementEmitter.CreateReturn(SyntaxFactory.IdentifierName(returnTable.Table.Name))],
                _ => null!
            };

            return statements != null;
        }
    }
}
