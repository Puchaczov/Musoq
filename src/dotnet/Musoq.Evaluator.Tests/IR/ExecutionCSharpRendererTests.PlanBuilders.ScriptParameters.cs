using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    private static ExecutionPlan CreateHashBuildScriptParameterCapturePlan()
    {
        return CreateHashBuildCapturePlan(
            "Q_HashBuildParameterCapture",
            new ExecutionScriptParameterRead("country", typeof(string)));
    }

    private static ExecutionPlan CreateHashBuildScriptVariableCapturePlan()
    {
        return CreateHashBuildCapturePlan(
            "Q_HashBuildVariableCapture",
            new ExecutionScriptVariableRead("country", typeof(string)));
    }

    private static ExecutionPlan CreateHashBuildCapturePlan(string queryName, ExecutionExpression keyExpression)
    {
        var resultShape = CreateSingleStringResultShape("Label");
        var resultTable = new ExecutionVariable("result", typeof(object));
        var hash = new ExecutionVariable("hash", typeof(object));
        var leftRows = new ExecutionVariable("leftRows", typeof(object));
        var rightRows = new ExecutionVariable("rightRows", typeof(object));
        var left = new ExecutionVariable("left", typeof(Row));
        var right = new ExecutionVariable("right", typeof(Row));
        var matches = new ExecutionVariable("matches", typeof(object));

        return new ExecutionPlan(
            queryName,
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateHash(hash, typeof(string), typeof(Row)),
                new ExecutionForEach(
                    left,
                    new ExecutionVariableRead(leftRows),
                    new ExecutionBlock(
                    [
                        new ExecutionHashAdd(
                            hash,
                            keyExpression,
                            left,
                            typeof(string),
                            typeof(Row))
                    ])),
                new ExecutionForEach(
                    right,
                    new ExecutionVariableRead(rightRows),
                    new ExecutionBlock(
                    [
                        new ExecutionHashProbe(
                            hash,
                            matches,
                            new ExecutionLiteral("PL", typeof(string)),
                            typeof(string),
                            typeof(Row),
                            new ExecutionBlock(
                            [
                                new ExecutionAppendRow(
                                    resultTable,
                                    resultShape,
                                    [new ExecutionRowValue("Label", new ExecutionLiteral("match", typeof(string)))])
                            ]),
                            new ExecutionBlock(
                            [
                                new ExecutionAppendRow(
                                    resultTable,
                                    resultShape,
                                    [new ExecutionRowValue("Label", new ExecutionLiteral("missing", typeof(string)))])
                            ]))
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateHashProbeScriptParameterCapturePlan()
    {
        var resultShape = CreateSingleStringResultShape("Label");
        var resultTable = new ExecutionVariable("result", typeof(object));
        var hash = new ExecutionVariable("hash", typeof(object));
        var leftRows = new ExecutionVariable("leftRows", typeof(object));
        var rightRows = new ExecutionVariable("rightRows", typeof(object));
        var left = new ExecutionVariable("left", typeof(Row));
        var right = new ExecutionVariable("right", typeof(Row));
        var matches = new ExecutionVariable("matches", typeof(object));

        return new ExecutionPlan(
            "Q_HashProbeParameterCapture",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateHash(hash, typeof(int), typeof(Row)),
                new ExecutionForEach(
                    left,
                    new ExecutionVariableRead(leftRows),
                    new ExecutionBlock(
                    [
                        new ExecutionHashAdd(
                            hash,
                            new ExecutionLiteral(1, typeof(int)),
                            left,
                            typeof(int),
                            typeof(Row))
                    ])),
                new ExecutionForEach(
                    right,
                    new ExecutionVariableRead(rightRows),
                    new ExecutionBlock(
                    [
                        new ExecutionHashProbe(
                            hash,
                            matches,
                            new ExecutionLiteral(1, typeof(int)),
                            typeof(int),
                            typeof(Row),
                            new ExecutionBlock(
                            [
                                new ExecutionAppendRow(
                                    resultTable,
                                    resultShape,
                                    [
                                        new ExecutionRowValue(
                                            "Label",
                                            new ExecutionScriptParameterRead("label", typeof(string)))
                                    ])
                            ]),
                            new ExecutionBlock(
                            [
                                new ExecutionAppendRow(
                                    resultTable,
                                    resultShape,
                                    [new ExecutionRowValue("Label", new ExecutionLiteral("missing", typeof(string)))])
                            ]))
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateSingleKeyAggregateScriptParameterCapturePlan()
    {
        var keyField = new AggregateGroupKeyField("Country", "__key0", typeof(string));
        var aggregateShape = new AggregateGroupShape(
            "ResultAggregateGroup",
            [keyField],
            [],
            []);
        var aggregatePlan = new AggregateGroupPlan(
            aggregateShape,
            [new AggregateGroupLevelPlan(0, aggregateShape)]);
        var resultShape = CreateSingleStringResultShape("Country");
        var resultTable = new ExecutionVariable("result", typeof(object));
        var rootGroup = new ExecutionVariable("rootGroup", typeof(object));
        var groups = new ExecutionVariable("groups", typeof(object));
        var groupsToFinalize = new ExecutionVariable("groupsToFinalize", typeof(object));
        var group = new ExecutionVariable("currentGroup", typeof(object));
        var rows = new ExecutionVariable("rows", typeof(object));
        var row = new ExecutionVariable("row", typeof(Row));

        return new ExecutionPlan(
            "Q_SingleKeyAggregateParameterCapture",
            [aggregateShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateSingleKeyAggregateContext(
                    rootGroup,
                    groups,
                    groupsToFinalize,
                    null,
                    typeof(string),
                    aggregatePlan),
                new ExecutionForEach(
                    row,
                    new ExecutionVariableRead(rows),
                    new ExecutionBlock(
                    [
                        new ExecutionGetOrAddSingleKeyAggregateGroup(
                            rootGroup,
                            groups,
                            groupsToFinalize,
                            group,
                            new ExecutionScriptParameterRead("country", typeof(string)),
                            "Country",
                            typeof(string),
                            null,
                            aggregatePlan)
                    ])),
                new ExecutionEnsureTableCapacity(
                    resultTable,
                    new ExecutionCollectionCountCapacityHint(groupsToFinalize)),
                new ExecutionForEach(
                    group,
                    new ExecutionVariableRead(groupsToFinalize),
                    new ExecutionBlock(
                    [
                        new ExecutionIf(
                            new ExecutionScriptParameterRead("include", typeof(bool)),
                            new ExecutionBlock(
                            [
                                new ExecutionAppendRow(
                                    resultTable,
                                    resultShape,
                                    [
                                        new ExecutionRowValue(
                                            "Country",
                                            new ExecutionGroupKeyRead(
                                                group,
                                                "Country",
                                                typeof(string),
                                                keyField))
                                    ])
                            ]))
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateParallelAggregateScriptParameterCapturePlan()
    {
        var keyField = new AggregateGroupKeyField("Country", "__key0", typeof(string));
        var aggregateShape = new AggregateGroupShape(
            "ResultAggregateGroup",
            [keyField],
            [],
            []);
        var aggregatePlan = new AggregateGroupPlan(
            aggregateShape,
            [new AggregateGroupLevelPlan(0, aggregateShape)]);
        var resultShape = CreateSingleStringResultShape("Country");
        var resultTable = new ExecutionVariable("result", typeof(object));
        var rootGroup = new ExecutionVariable("rootGroup", typeof(object));
        var groups = new ExecutionVariable("groups", typeof(object));
        var groupsToFinalize = new ExecutionVariable("groupsToFinalize", typeof(object));
        var group = new ExecutionVariable("currentGroup", typeof(object));
        var rows = new ExecutionVariable("rows", typeof(object));
        var row = new ExecutionVariable("row", typeof(Row));

        return new ExecutionPlan(
            "Q_ParallelAggregateParameterCapture",
            [aggregateShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateSingleKeyAggregateContext(
                    rootGroup,
                    groups,
                    groupsToFinalize,
                    null,
                    typeof(string),
                    aggregatePlan),
                new ExecutionParallelSingleKeyAggregateLoop(
                    new ExecutionForEach(
                        row,
                        new ExecutionVariableRead(rows),
                        new ExecutionBlock(
                        [
                            new ExecutionGetOrAddSingleKeyAggregateGroup(
                                rootGroup,
                                groups,
                                groupsToFinalize,
                                group,
                                new ExecutionScriptParameterRead("country", typeof(string)),
                                "Country",
                                typeof(string),
                                null,
                                aggregatePlan)
                        ])),
                    row,
                    new ExecutionVariableRead(rows),
                    new ExecutionScriptParameterRead("country", typeof(string)),
                    "Country",
                    typeof(string),
                    rootGroup,
                    groupsToFinalize,
                    group,
                    ExecutionBlock.Empty,
                    aggregateShape,
                    1,
                    16,
                    8,
                    4),
                new ExecutionEnsureTableCapacity(
                    resultTable,
                    new ExecutionCollectionCountCapacityHint(groupsToFinalize)),
                new ExecutionForEach(
                    group,
                    new ExecutionVariableRead(groupsToFinalize),
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [
                                new ExecutionRowValue(
                                    "Country",
                                    new ExecutionGroupKeyRead(
                                        group,
                                        "Country",
                                        typeof(string),
                                        keyField))
                            ])
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateParallelFilterProjectScriptParameterCapturePlan()
    {
        var resultShape = CreateSingleStringResultShape("Label");
        var resultTable = new ExecutionVariable("result", typeof(object));
        var rows = new ExecutionVariable("rows", typeof(object));
        var row = new ExecutionVariable("row", typeof(Row));
        var appendRow = new ExecutionAppendRow(
            resultTable,
            resultShape,
            [
                new ExecutionRowValue(
                    "Label",
                    new ExecutionScriptParameterRead("label", typeof(string)))
            ]);

        return new ExecutionPlan(
            "Q_ParallelFilterProjectParameterCapture",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionParallelFilterProjectLoop(
                    new ExecutionForEach(
                        row,
                        new ExecutionVariableRead(rows),
                        new ExecutionBlock(
                        [
                            new ExecutionIf(
                                new ExecutionScriptParameterRead("include", typeof(bool)),
                                new ExecutionBlock([appendRow]))
                        ])),
                    row,
                    new ExecutionVariableRead(rows),
                    new ExecutionScriptParameterRead("include", typeof(bool)),
                    appendRow,
                    1,
                    4),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateStoredTableBuildScriptParameterCapturePlan()
    {
        var resultShape = CreateSingleStringResultShape("Country");
        var cteTable = new ExecutionVariable("cte", typeof(object));
        var resultTable = new ExecutionVariable("result", typeof(object));
        var cteRow = new ExecutionVariable("cteRow", typeof(Row));

        return new ExecutionPlan(
            "Q_StoredTableParameterCapture",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(cteTable, resultShape),
                new ExecutionAppendRow(
                    cteTable,
                    resultShape,
                    [
                        new ExecutionRowValue(
                            "Country",
                            new ExecutionScriptParameterRead("country", typeof(string)))
                    ]),
                new ExecutionStoreTable(cteTable, 0),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEach(
                    cteRow,
                    new ExecutionStoredTableRows(0, resultShape),
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [
                                new ExecutionRowValue(
                                    "Country",
                                    new ExecutionFieldRead(
                                        "cteRow",
                                        "Country",
                                        typeof(string),
                                        new GeneratedFieldAccess("Country")))
                            ])
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateRankingWindowKeyExtractionScriptParameterCapturePlan()
    {
        var buffer = new ExecutionVariable("windowRows", typeof(object));
        var item = new ExecutionVariable("row", typeof(Row));
        var results = new ExecutionVariable("rankings", typeof(long[]));

        return new ExecutionPlan(
            "Q_RankingWindowParameterCapture",
            [],
            new ExecutionBlock(
            [
                new ExecutionComputeRankingWindow(
                    buffer,
                    item,
                    ExecutionRowAccessMode.Direct,
                    new ExecutionScriptParameterRead("country", typeof(string)),
                    [new ExecutionWindowOrderKey(new ExecutionScriptParameterRead("sortLabel", typeof(string)), false)],
                    ExecutionRankingWindowFunction.Rank,
                    results)
            ]));
    }

    private static ExecutionPlan CreateWindowAppendRowsScriptParameterCapturePlan()
    {
        var resultShape = CreateSingleStringResultShape("Label");
        var resultTable = new ExecutionVariable("result", typeof(object));
        var windowRows = new ExecutionVariable("resultWindowRows", typeof(object));
        var row = new ExecutionVariable("row", typeof(Row));
        var index = new ExecutionVariable("windowIndex", typeof(int));

        return new ExecutionPlan(
            "Q_WindowAppendParameterCapture",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEachIndexed(
                    row,
                    index,
                    windowRows,
                    ExecutionRowAccessMode.Direct,
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [
                                new ExecutionRowValue(
                                    "Label",
                                    new ExecutionScriptParameterRead("label", typeof(string)))
                            ])
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateDirectAppendPlan()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));

        return new ExecutionPlan(
            "Q_DirectAppend",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionAppendRow(
                    resultTable,
                    resultShape,
                    [new ExecutionRowValue("Name", new ExecutionLiteral("Ada", typeof(string)))],
                    ExecutionAppendMode.Direct),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateContextLayoutAppendPlan()
    {
        var leftShape = new SourceEntityShape(
            "p",
            typeof(Person),
            [
                new FieldBinding("Name", "p.Name", 0, typeof(string), FieldNullability.Unknown, new ClrPropertyAccess("Name"))
            ]);
        var rightShape = new SourceEntityShape(
            "q",
            typeof(Person),
            [
                new FieldBinding("Name", "q.Name", 0, typeof(string), FieldNullability.Unknown, new ClrPropertyAccess("Name"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name")),
                new FieldBinding("OtherName", "OtherName", 1, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("OtherName"))
            ]);
        var left = new ExecutionVariable("p", typeof(Person));
        var right = new ExecutionVariable("q", typeof(Person));
        var leftRows = new ExecutionVariable("pRows", typeof(object));
        var rightRows = new ExecutionVariable("qRows", typeof(object));
        var resultTable = new ExecutionVariable("result", typeof(object));

        return new ExecutionPlan(
            "Q_ContextLayout",
            [leftShape, rightShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(
                    left,
                    leftRows,
                    new ExecutionSourceBinding("test", "data", "p:1", 0, [], leftShape.Fields)),
                new ExecutionSourceScan(
                    right,
                    rightRows,
                    new ExecutionSourceBinding("test", "data", "q:1", 1, [], rightShape.Fields)),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEach(
                    left,
                    new ExecutionRowStream(leftRows, ExecutionRowStreamKind.Rows),
                    new ExecutionBlock(
                    [
                        new ExecutionForEach(
                            right,
                            new ExecutionRowStream(rightRows, ExecutionRowStreamKind.Rows),
                            new ExecutionBlock(
                            [
                                new ExecutionAppendRow(
                                    resultTable,
                                    resultShape,
                                    [
                                        new ExecutionRowValue("Name", new ExecutionFieldRead("p", "Name", typeof(string))),
                                        new ExecutionRowValue("OtherName", new ExecutionFieldRead("q", "Name", typeof(string)))
                                    ],
                                    [
                                        new ExecutionVariableRead(left),
                                        new ExecutionVariableRead(right)
                                    ],
                                    ExecutionAppendMode.Direct,
                                    new ExecutionContextLayout(
                                    [
                                        new ExecutionContextSegment(
                                            ExecutionContextSegmentKind.Single,
                                            new ExecutionVariableRead(left),
                                            1),
                                        new ExecutionContextSegment(
                                            ExecutionContextSegmentKind.Single,
                                            new ExecutionVariableRead(right),
                                            1)
                                    ]))
                            ]))
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }
}
