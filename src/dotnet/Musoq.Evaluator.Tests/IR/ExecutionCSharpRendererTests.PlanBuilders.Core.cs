using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    private static ExecutionPlan CreateFinalResultPlan(
        string identifier,
        IReadOnlyList<RowShape> shapes,
        ExecutionBlock body,
        ExecutionVariable finalTable,
        GeneratedRowShape finalShape,
        ExecutionColumnMetadata? columnMetadata = null)
    {
        return new ExecutionPlan(
            identifier,
            shapes,
            body,
            new FinalShapeResult(
                finalTable.Name,
                finalTable,
                finalShape,
                columnMetadata ?? CreateFinalResultColumnMetadata(finalTable.Name, finalShape)));
    }

    private static ExecutionColumnMetadata CreateFinalResultColumnMetadata(
        string tableName,
        GeneratedRowShape finalShape)
    {
        return new ExecutionColumnMetadata(
            tableName,
            finalShape.Fields
                .Select(static field => ExecutionColumnMetadataFields.FromFieldBinding(field))
                .ToArray(),
            ExecutionColumnMetadataKind.TableColumns);
    }

    private static ExecutionPlan CreatePlan()
    {
        var sourceShape = new SourceEntityShape(
            "p",
            typeof(Person),
            [
                new FieldBinding("Name", "p.Name", 0, typeof(string), FieldNullability.Unknown, new ClrPropertyAccess("Name")),
                new FieldBinding("Age", "p.Age", 1, typeof(int), FieldNullability.Unknown, new ClrPropertyAccess("Age"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var source = new ExecutionVariable("p", typeof(Person));
        var sourceRows = new ExecutionVariable("pRows", typeof(object));
        var sourceBinding = new ExecutionSourceBinding(
            "test",
            "data",
            "p:1",
            0,
            [],
            sourceShape.Fields);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var predicate = new ExecutionBinary(
            BinaryOpKind.GreaterThan,
            new ExecutionFieldRead("p", "Age", typeof(int)),
            new ExecutionLiteral(18, typeof(int)),
            typeof(bool));

        return CreateFinalResultPlan(
            "Q_Plain",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(source, sourceRows, sourceBinding),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEach(
                    source,
                    new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks),
                    new ExecutionBlock(
                    [
                        new ExecutionIf(
                            predicate,
                            new ExecutionBlock(
                            [
                                new ExecutionAppendRow(
                                    resultTable,
                                    resultShape,
                                    [new ExecutionRowValue("Name", new ExecutionFieldRead("p", "Name", typeof(string)))])
                            ]))
                    ])),
                new ExecutionReturnTable(resultTable)
            ]),
            resultTable,
            resultShape);
    }

    private static ExecutionPlan CreateParallelProjectionPlan()
    {
        var sourceShape = new SourceEntityShape(
            "p",
            typeof(Person),
            [
                new FieldBinding("Name", "p.Name", 0, typeof(string), FieldNullability.Unknown, new ClrPropertyAccess("Name")),
                new FieldBinding("Age", "p.Age", 1, typeof(int), FieldNullability.Unknown, new ClrPropertyAccess("Age"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var source = new ExecutionVariable("p", typeof(Person));
        var sourceRows = new ExecutionVariable("pRows", typeof(object));
        var sourceBinding = new ExecutionSourceBinding(
            "test",
            "data",
            "p:1",
            0,
            [],
            sourceShape.Fields);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var predicate = new ExecutionBinary(
            BinaryOpKind.GreaterThan,
            new ExecutionFieldRead("p", "Age", typeof(int)),
            new ExecutionLiteral(18, typeof(int)),
            typeof(bool));
        var appendRow = new ExecutionAppendRow(
            resultTable,
            resultShape,
            [new ExecutionRowValue("Name", new ExecutionFieldRead("p", "Name", typeof(string)))]);
        var serialLoop = new ExecutionForEach(
            source,
            new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks),
            new ExecutionBlock(
            [
                new ExecutionIf(
                    predicate,
                    new ExecutionBlock([appendRow]))
            ]));

        return CreateFinalResultPlan(
            "Q_ParallelProjection",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(source, sourceRows, sourceBinding),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionParallelFilterProjectLoop(
                    serialLoop,
                    source,
                    new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks),
                    predicate,
                    appendRow,
                    1,
                    4),
                new ExecutionReturnTable(resultTable)
            ]),
            resultTable,
            resultShape);
    }

    private static ExecutionPlan CreateParallelProjectionPlanWithRowLocalMethodCse()
    {
        var sourceShape = new SourceEntityShape(
            "p",
            typeof(Person),
            [
                new FieldBinding("Name", "p.Name", 0, typeof(string), FieldNullability.Unknown, new ClrPropertyAccess("Name"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name")),
                new FieldBinding("Upper", "Upper", 1, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Upper"))
            ]);
        var source = new ExecutionVariable("p", typeof(Person));
        var sourceRows = new ExecutionVariable("pRows", typeof(object));
        var sourceBinding = new ExecutionSourceBinding(
            "test",
            "data",
            "p:1",
            0,
            [],
            sourceShape.Fields);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var upper = new ExecutionVariable("upper", typeof(string));
        var methodCall = CreateToUpperNameCall();
        var predicate = new ExecutionBinary(
            BinaryOpKind.NotEqual,
            methodCall,
            new ExecutionLiteral(null, typeof(string)),
            typeof(bool));
        var appendRow = new ExecutionAppendRow(
            resultTable,
            resultShape,
            [
                new ExecutionRowValue("Name", new ExecutionFieldRead("p", "Name", typeof(string))),
                new ExecutionRowValue("Upper", methodCall)
            ]);
        var serialAppendRow = new ExecutionAppendRow(
            resultTable,
            resultShape,
            [
                new ExecutionRowValue("Name", new ExecutionFieldRead("p", "Name", typeof(string))),
                new ExecutionRowValue("Upper", new ExecutionVariableRead(upper))
            ]);
        var serialLoop = new ExecutionForEach(
            source,
            new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks),
            new ExecutionBlock(
            [
                new ExecutionLet(upper, methodCall, ExecutionLetCacheMode.SuppressMethodCache),
                new ExecutionIf(
                    new ExecutionBinary(
                        BinaryOpKind.NotEqual,
                        new ExecutionVariableRead(upper),
                        new ExecutionLiteral(null, typeof(string)),
                        typeof(bool)),
                    new ExecutionBlock([serialAppendRow]))
            ]));

        return CreateFinalResultPlan(
            "Q_ParallelProjection_RowLocalCse",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(source, sourceRows, sourceBinding),
                new ExecutionCreateObject(methodCall.Target!),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionParallelFilterProjectLoop(
                    serialLoop,
                    source,
                    new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks),
                    predicate,
                    appendRow,
                    1,
                    4),
                new ExecutionReturnTable(resultTable)
            ]),
            resultTable,
            resultShape);
    }

    private static ExecutionPlan CreateProjectionPostOperationPlan()
    {
        var sourceShape = new SourceEntityShape(
            "p",
            typeof(Person),
            [
                new FieldBinding("Name", "p.Name", 0, typeof(string), FieldNullability.Unknown, new ClrPropertyAccess("Name")),
                new FieldBinding("Age", "p.Age", 1, typeof(int), FieldNullability.Unknown, new ClrPropertyAccess("Age"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var source = new ExecutionVariable("p", typeof(Person));
        var sourceRows = new ExecutionVariable("pRows", typeof(object));
        var sourceBinding = new ExecutionSourceBinding(
            "test",
            "data",
            "p:1",
            0,
            [],
            sourceShape.Fields);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var distinctTable = new ExecutionVariable("resultDistinct", typeof(object));
        var sortedTable = new ExecutionVariable("resultSorted", typeof(object));
        var skippedTable = new ExecutionVariable("resultSkipped", typeof(object));
        var takenTable = new ExecutionVariable("resultTaken", typeof(object));

        return CreateFinalResultPlan(
            "Q_PostOperationProjection",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(source, sourceRows, sourceBinding),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEach(
                    source,
                    new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks),
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [new ExecutionRowValue("Name", new ExecutionFieldRead("p", "Name", typeof(string)))])
                    ])),
                new ExecutionDistinctTable(resultTable, distinctTable),
                new ExecutionSortTable(
                    distinctTable,
                    sortedTable,
                    [new ExecutionOrderField("Name", 0, typeof(string), true)]),
                new ExecutionSkipTable(sortedTable, skippedTable, 1),
                new ExecutionTakeTable(skippedTable, takenTable, 2),
                new ExecutionReturnTable(takenTable)
            ]),
            takenTable,
            resultShape);
    }

    private static ExecutionPlan CreateSameReferenceDifferentMetadataPlan()
    {
        var idShape = new GeneratedRowShape(
            "IdRow",
            [
                new FieldBinding("Id", "Id", 0, typeof(int), FieldNullability.Unknown, new GeneratedFieldAccess("Id"))
            ]);
        var nameShape = new GeneratedRowShape(
            "NameRow",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var firstTable = new ExecutionVariable("first", typeof(object));
        var secondTable = new ExecutionVariable("second", typeof(object));

        return new ExecutionPlan(
            "Q_MetadataCollision",
            [idShape, nameShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(
                    firstTable,
                    idShape,
                    ColumnMetadata: new ExecutionColumnMetadata(
                        "shared",
                        [new ExecutionColumnMetadataField("Id", 0, typeof(int))],
                        ExecutionColumnMetadataKind.TableColumns)),
                new ExecutionCreateTable(
                    secondTable,
                    nameShape,
                    ColumnMetadata: new ExecutionColumnMetadata(
                        "shared",
                        [new ExecutionColumnMetadataField("Name", 0, typeof(string))],
                        ExecutionColumnMetadataKind.TableColumns))
            ]));
    }

    private static ExecutionPlan CreateSameReferenceDifferentSourceModifierMetadataPlan()
    {
        var sourceShape = new SourceEntityShape("p", typeof(Person), []);
        var firstBinding = CreateModifierMetadataSourceBinding("p:1", "utf-8");
        var secondBinding = CreateModifierMetadataSourceBinding("p:2", "windows-1250");

        return new ExecutionPlan(
            "Q_SourceModifierMetadataCollision",
            [sourceShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(
                    new ExecutionVariable("p1", typeof(Person)),
                    new ExecutionVariable("p1Rows", typeof(object)),
                    firstBinding),
                new ExecutionSourceScan(
                    new ExecutionVariable("p2", typeof(Person)),
                    new ExecutionVariable("p2Rows", typeof(object)),
                    secondBinding)
            ]));
    }

    private static ExecutionSourceBinding CreateModifierMetadataSourceBinding(string runtimeContextId, string encoding)
    {
        var metadata = new ExecutionColumnMetadata(
            "shared",
            [
                new ExecutionColumnMetadataField(
                    "Name",
                    0,
                    typeof(string),
                    new Dictionary<string, string> { ["encoding"] = encoding })
            ],
            ExecutionColumnMetadataKind.SourceSchemaColumns);

        return new ExecutionSourceBinding(
            "test",
            "data",
            runtimeContextId,
            0,
            [],
            [],
            metadata,
            typeof(Person));
    }

    private static ExecutionPlan CreatePostOperationMetadataPlan()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var sortedTable = new ExecutionVariable("resultSorted", typeof(object));
        var skippedTable = new ExecutionVariable("resultSortedSkipped", typeof(object));
        var takenTable = new ExecutionVariable("resultSortedSkippedTaken", typeof(object));

        return CreateFinalResultPlan(
            "Q_PostOps",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionSortTable(
                    resultTable,
                    sortedTable,
                    [new ExecutionOrderField("Name", 0, typeof(string), false)],
                    [],
                    new ExecutionCollectionCountCapacityHint(resultTable),
                    ExecutionAppendMode.Direct,
                    CreateTestColumnMetadata(sortedTable.Name)),
                new ExecutionSkipTable(
                    sortedTable,
                    skippedTable,
                    1,
                    new ExecutionSkipCapacityHint(sortedTable, 1),
                    ExecutionAppendMode.Direct,
                    CreateTestColumnMetadata(skippedTable.Name)),
                new ExecutionTakeTable(
                    skippedTable,
                    takenTable,
                    2,
                    new ExecutionTakeCapacityHint(skippedTable, 2),
                    ExecutionAppendMode.Direct,
                    CreateTestColumnMetadata(takenTable.Name)),
                new ExecutionReturnTable(takenTable)
            ]),
            takenTable,
            resultShape);
    }

    private static ExecutionColumnMetadata CreateTestColumnMetadata(string referenceName)
    {
        return new ExecutionColumnMetadata(
            referenceName,
            [new ExecutionColumnMetadataField("Name", 0, typeof(string))],
            ExecutionColumnMetadataKind.TableColumns);
    }

    private static ExecutionPlan CreateTopNMetadataPlan()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var topNTable = new ExecutionVariable("resultTopN", typeof(object));

        return new ExecutionPlan(
            "Q_TopN",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionTopNTable(
                    resultTable,
                    topNTable,
                    [new ExecutionOrderField("Name", 0, typeof(string), false)],
                    2,
                    [],
                    new ExecutionTakeCapacityHint(resultTable, 2),
                    ExecutionAppendMode.Direct,
                    CreateTestColumnMetadata(topNTable.Name)),
                new ExecutionReturnTable(topNTable)
            ]));
    }

    private static ExecutionPlan CreateTopOffsetMetadataPlan()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var topOffsetTable = new ExecutionVariable("resultTopOffset", typeof(object));

        return new ExecutionPlan(
            "Q_TopOffset",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionTopOffsetTable(
                    resultTable,
                    topOffsetTable,
                    [new ExecutionOrderField("Name", 0, typeof(string), false)],
                    1,
                    2,
                    [],
                    ExecutionTopOffsetStrategy.BoundedHeap,
                    new ExecutionSkipTakeCapacityHint(resultTable, 1, 2),
                    ExecutionAppendMode.Direct,
                    CreateTestColumnMetadata(topOffsetTable.Name)),
                new ExecutionReturnTable(topOffsetTable)
            ]));
    }

    private static ExecutionPlan CreateSliceMetadataPlan()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var slicedTable = new ExecutionVariable("resultSliced", typeof(object));

        return new ExecutionPlan(
            "Q_Slice",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionSliceTable(
                    resultTable,
                    slicedTable,
                    1,
                    2,
                    new ExecutionSkipTakeCapacityHint(resultTable, 1, 2),
                    ExecutionAppendMode.Direct,
                    CreateTestColumnMetadata(slicedTable.Name)),
                new ExecutionReturnTable(slicedTable)
            ]));
    }

    private static ExecutionPlan CreateTableRowLoopPlan()
    {
        var sourceShape = new GeneratedRowShape(
            "SourceRow0",
            [
                new FieldBinding("Name", "t.Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var sourceTable = new ExecutionVariable("source", typeof(object));
        var resultTable = new ExecutionVariable("result", typeof(object));
        var tableRow = new ExecutionVariable("t", typeof(Row));

        return new ExecutionPlan(
            "Q_TableRowLoop",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(sourceTable, sourceShape),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEach(
                    tableRow,
                    new ExecutionRowStream(
                        sourceTable,
                        ExecutionRowStreamKind.Rows,
                        ExecutionRowStreamRowsAccess.TableRows),
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [new ExecutionRowValue("Name", new ExecutionFieldRead("t", "Name", typeof(string), new PositionalAccess(0)))])
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateRepeatedStoredRowsPlan()
    {
        var sourceShape = new GeneratedRowShape(
            "SourceRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var sourceTable = new ExecutionVariable("source", typeof(object));
        var resultTable = new ExecutionVariable("result", typeof(object));
        var leftRow = new ExecutionVariable("l", typeof(Row));
        var rightRow = new ExecutionVariable("r", typeof(Row));

        return new ExecutionPlan(
            "Q_RepeatedStoredRows",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(sourceTable, sourceShape),
                new ExecutionStoreTable(sourceTable, 0),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEach(
                    leftRow,
                    new ExecutionStoredTableRows(0, sourceShape),
                    new ExecutionBlock(
                    [
                        new ExecutionForEach(
                            rightRow,
                            new ExecutionStoredTableRows(0, sourceShape),
                            new ExecutionBlock(
                            [
                                new ExecutionAppendRow(
                                    resultTable,
                                    resultShape,
                                    [new ExecutionRowValue("Name", new ExecutionLiteral("cached", typeof(string)))])
                            ]))
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateTypedMaterializedRowsBufferPlan()
    {
        var sourceShape = new GeneratedRowShape(
            "SourceRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var sourceTable = new ExecutionVariable("source", typeof(object));
        var resultTable = new ExecutionVariable("result", typeof(object));
        var buffer = new ExecutionVariable("sourceRowsBuffer", typeof(IReadOnlyList<Row>), sourceShape.TypeName);
        var sourceRow = new ExecutionVariable("s", typeof(Row), sourceShape.TypeName);

        return new ExecutionPlan(
            "Q_TypedMaterializedRowsBuffer",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(sourceTable, sourceShape),
                new ExecutionMaterializeList(
                    new ExecutionRowStream(
                        sourceTable,
                        ExecutionRowStreamKind.Rows,
                        ExecutionRowStreamRowsAccess.TableRows),
                    buffer,
                    sourceShape),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEach(
                    sourceRow,
                    new ExecutionVariableRead(buffer),
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [
                                new ExecutionRowValue(
                                    "Name",
                                    new ExecutionFieldRead("s", "Name", typeof(string), new GeneratedFieldAccess("Name")))
                            ])
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateInternalStoredRowCarrierPlan(bool useRowContext)
    {
        var statementShape = new GeneratedRowShape(
            "Statement0Row0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ],
            [
                new FieldBinding("Source", "Source", 0, typeof(object), FieldNullability.Unknown, new GeneratedRowContextAccess("Statement0Row0", 0))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var cteTable = new ExecutionVariable("statement", typeof(object));
        var resultTable = new ExecutionVariable("result", typeof(object));
        var cteRow = new ExecutionVariable("s", typeof(Row), statementShape.TypeName);
        var cteRows = new ExecutionStoredTableRows(0, statementShape);
        var resultContexts = useRowContext
            ? (IReadOnlyList<ExecutionExpression>)[new ExecutionVariableRead(cteRow)]
            : [];
        var resultContextLayout = useRowContext
            ? new ExecutionContextLayout(
            [
                new ExecutionContextSegment(
                    ExecutionContextSegmentKind.Row,
                    new ExecutionVariableRead(cteRow),
                    1)
            ])
            : null;

        return new ExecutionPlan(
            "Q_InternalStoredRowCarrier",
            [statementShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(cteTable, statementShape),
                new ExecutionAppendRow(
                    cteTable,
                    statementShape,
                    [new ExecutionRowValue("Name", new ExecutionLiteral("Ada", typeof(string)))],
                    [new ExecutionLiteral("ctx", typeof(object))],
                    ExecutionAppendMode.Checked,
                    new ExecutionContextLayout(
                    [
                        new ExecutionContextSegment(
                            ExecutionContextSegmentKind.Single,
                            new ExecutionLiteral("ctx", typeof(object)),
                            1)
                    ])),
                new ExecutionStoreTable(cteTable, 0),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEach(
                    cteRow,
                    cteRows,
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [
                                new ExecutionRowValue(
                                    "Name",
                                    new ExecutionFieldRead("s", "Name", typeof(string), new GeneratedFieldAccess("Name")))
                            ],
                            resultContexts,
                            ExecutionAppendMode.Checked,
                            resultContextLayout)
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateParallelBlockPlan()
    {
        var cteShape = new GeneratedRowShape(
            "CteRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var cte0 = new ExecutionVariable("cte0", typeof(Table));
        var cte1 = new ExecutionVariable("cte1", typeof(Table));
        var output0 = new ExecutionVariable("__parallelCteLevel0Task0Result", typeof(Table));
        var output1 = new ExecutionVariable("__parallelCteLevel0Task1Result", typeof(Table));

        return new ExecutionPlan(
            "Q_ParallelBlock",
            [cteShape],
            new ExecutionBlock(
            [
                new ExecutionParallelBlock(
                    "cte-level-0",
                    2,
                    [
                        new ExecutionParallelTask(
                            "left",
                            output0,
                            new ExecutionBlock(
                            [
                                new ExecutionCreateTable(cte0, cteShape),
                                new ExecutionAssign(output0, new ExecutionVariableRead(cte0))
                            ])),
                        new ExecutionParallelTask(
                            "right",
                            output1,
                            new ExecutionBlock(
                            [
                                new ExecutionCreateTable(cte1, cteShape),
                                new ExecutionAssign(output1, new ExecutionVariableRead(cte1))
                            ]))
                    ],
                    new ExecutionParallelMerge(new ExecutionBlock(
                    [
                        new ExecutionStoreTable(output0, 0),
                        new ExecutionStoreTable(output1, 1)
                    ]))),
                new ExecutionReturnTable(output0)
            ]));
    }

    private static ExecutionPlan CreateTypedParallelBlockPlan()
    {
        var cteShape = new GeneratedRowShape(
            "CteRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var cte0 = new ExecutionVariable("cte0", typeof(Table));
        var cte1 = new ExecutionVariable("cte1", typeof(Table));
        var output0 = new ExecutionVariable("__parallelCteLevel0Task0Result", typeof(object), "List<CteRow0>");
        var output1 = new ExecutionVariable("__parallelCteLevel0Task1Result", typeof(object), "List<CteRow0>");

        return new ExecutionPlan(
            "Q_TypedParallelBlock",
            [cteShape],
            new ExecutionBlock(
            [
                new ExecutionParallelBlock(
                    "cte-level-0",
                    2,
                    [
                        new ExecutionParallelTask(
                            "left",
                            output0,
                            new ExecutionBlock(
                            [
                                new ExecutionCreateTable(cte0, cteShape),
                                new ExecutionAssign(output0, new ExecutionVariableRead(cte0))
                            ]),
                            0),
                        new ExecutionParallelTask(
                            "right",
                            output1,
                            new ExecutionBlock(
                            [
                                new ExecutionCreateTable(cte1, cteShape),
                                new ExecutionAssign(output1, new ExecutionVariableRead(cte1))
                            ]),
                            1)
                    ],
                    new ExecutionParallelMerge(new ExecutionBlock(
                    [
                        new ExecutionStoreTable(output0, 0),
                        new ExecutionStoreTable(output1, 1)
                    ]))),
                new ExecutionReturnTable(output0)
            ]));
    }

    private static ExecutionPlan CreateCapacityHintPlan()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));

        return new ExecutionPlan(
            "Q_CapacityHint",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape, new ExecutionConstantCapacityHint(16)),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateHashCapacityHintPlan()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var hash = new ExecutionVariable("hash", typeof(object));

        return new ExecutionPlan(
            "Q_HashCapacityHint",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateHash(hash, typeof(int), typeof(Row), new ExecutionConstantCapacityHint(32)),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateHashEnumerableCapacityHintPlan()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var hash = new ExecutionVariable("hash", typeof(object));
        var rows = new ExecutionVariable("rows", typeof(object));

        return new ExecutionPlan(
            "Q_HashEnumerableCapacityHint",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateHash(
                    hash,
                    typeof(int),
                    typeof(Row),
                    new ExecutionTryGetNonEnumeratedCountCapacityHint(rows, "hashCapacity")),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateValueTupleHashPlan()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var hash = new ExecutionVariable("hash", typeof(object));
        var row = new ExecutionVariable("row", typeof(Row));
        var tupleType = typeof(ValueTuple<int, int>);

        return new ExecutionPlan(
            "Q_ValueTupleHash",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateHash(hash, tupleType, typeof(Row)),
                new ExecutionHashAdd(
                    hash,
                    new ExecutionValueTupleKey(
                    [
                        new ExecutionLiteral(1, typeof(int)),
                        new ExecutionLiteral(2, typeof(int))
                    ], tupleType),
                    row,
                    tupleType,
                    typeof(Row)),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateHashPayloadShapePlan()
    {
        return new ExecutionPlan(
            "Q_HashPayloadShape",
            [CreateHashPayloadShape()],
            ExecutionBlock.Empty);
    }

    private static ExecutionPlan CreateHashPayloadJoinPlan()
    {
        var payloadShape = CreateHashPayloadShape();
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var hash = new ExecutionVariable("dHash", typeof(object));
        var rows = new ExecutionVariable("bRows", typeof(object));
        var b = new ExecutionVariable("b", typeof(BasicEntity));
        var d = new ExecutionVariable("d", typeof(object), payloadShape.TypeName);

        return new ExecutionPlan(
            "Q_HashPayloadJoin",
            [payloadShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateHash(hash, typeof(string), typeof(object), GeneratedRowTypeName: payloadShape.TypeName),
                new ExecutionForEach(
                    b,
                    new ExecutionVariableRead(rows),
                    new ExecutionBlock(
                    [
                        new ExecutionCreateHashPayload(
                            d,
                            payloadShape,
                            [
                                new ExecutionRowValue("b.City", new ExecutionFieldRead("b", "City", typeof(string))),
                                new ExecutionRowValue("b.Country", new ExecutionFieldRead("b", "Country", typeof(string)))
                            ]),
                        new ExecutionHashAdd(
                            hash,
                            new ExecutionFieldRead("d", "b.Country", typeof(string), new GeneratedFieldAccess("b_Country")),
                            d,
                            typeof(string),
                            typeof(object),
                            payloadShape.TypeName)
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static HashPayloadShape CreateHashPayloadShape()
    {
        return new HashPayloadShape(
            "DHashPayload0",
            [
                new FieldBinding("b.City", "b.City", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("b_City")),
                new FieldBinding("b.Country", "b.Country", 1, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("b_Country"))
            ]);
    }

}
