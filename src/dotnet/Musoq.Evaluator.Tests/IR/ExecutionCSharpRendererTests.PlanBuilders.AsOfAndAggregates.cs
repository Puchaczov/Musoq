using System.Collections.Generic;
using System.Dynamic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    private static ExecutionPlan CreateIndexedAsOfProbePlan()
    {
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var rightRows = new ExecutionVariable("rRows", typeof(object));
        var match = new ExecutionVariable("r", typeof(Person));
        var candidate = new ExecutionVariable("rCandidate", typeof(Person));
        var index = new ExecutionVariable("asOfIndex", typeof(object));
        var equalityKeys = new[]
        {
            new ExecutionAsOfEqualityKey(
                new ExecutionFieldRead("l", "Name", typeof(string)),
                new ExecutionFieldRead("rCandidate", "Name", typeof(string)))
        };
        var probeKey = new ExecutionFieldRead("l", "Age", typeof(int));
        var candidateKey = new ExecutionFieldRead("rCandidate", "Age", typeof(int));

        return CreateFinalResultPlan(
            "Q_IndexedAsOf",
            [resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateAsOfIndex(
                    index,
                    candidate,
                    new ExecutionRowStream(rightRows, ExecutionRowStreamKind.Chunks),
                    equalityKeys,
                    candidateKey,
                    BinaryOpKind.GreaterOrEqual,
                    typeof(int)),
                new ExecutionAsOfProbe(
                    match,
                    candidate,
                    new ExecutionRowStream(rightRows, ExecutionRowStreamKind.Chunks),
                    equalityKeys,
                    probeKey,
                    candidateKey,
                    BinaryOpKind.GreaterOrEqual,
                    ExecutionBlock.Empty,
                    Index: index,
                    ComparisonKeyType: ExecutionClrBindingFactory.FromClr(typeof(int))),
                new ExecutionReturnTable(resultTable)
            ]),
            resultTable,
            resultShape);
    }

    private static ExecutionPlan CreateKernelAggregatePlan()
    {
        var declaration = typeof(KernelAggregateLibrary).GetMethod(nameof(KernelAggregateLibrary.Sum))!;
        var kernel = AggregateKernelDescriptor.Create(declaration);
        var accumulator = new AggregateAccumulatorField(
            "Sum(Age)",
            "__agg0",
            kernel);
        var aggregateShape = new AggregateGroupShape(
            "ResultAggregateGroup",
            [],
            [],
            [accumulator]);
        var aggregatePlan = new AggregateGroupPlan(
            aggregateShape,
            [new AggregateGroupLevelPlan(0, aggregateShape)]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Total", "Total", 0, typeof(int?), FieldNullability.Nullable, new GeneratedFieldAccess("Total"))
            ]);
        var resultTable = new ExecutionVariable("result", typeof(object));
        var rootGroup = new ExecutionVariable("rootGroup", typeof(object));
        var currentGroup = new ExecutionVariable("currentGroup", typeof(object));
        var groups = new ExecutionVariable("groups", typeof(object));

        return CreateFinalResultPlan(
            "Q_KernelAggregate",
            [aggregateShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionCreateAggregateContext(rootGroup, currentGroup, groups, aggregatePlan),
                new ExecutionAggregateSet(
                    currentGroup,
                    declaration,
                    [],
                    accumulator,
                    new ExecutionLiteral(10, typeof(int?))),
                new ExecutionAppendRow(
                    resultTable,
                    resultShape,
                    [
                        new ExecutionRowValue(
                            "Total",
                            new ExecutionAggregateCall(
                                currentGroup,
                                declaration,
                                [],
                                typeof(int?),
                                accumulator))
                    ]),
                new ExecutionReturnTable(resultTable)
            ]),
            resultTable,
            resultShape);
    }

    private static ExecutionPlan CreateDynamicAsOfProbePlan()
    {
        var rightShape = new ExpandoAdapterShape(
            "r",
            "rDynamicRow0",
            typeof(IReadOnlyDictionary<string, object>),
            [
                new FieldBinding("Score", "r.Score", 0, typeof(int), FieldNullability.Unknown, new ExpandoDictionaryAccess("Score"))
            ]);
        var match = new ExecutionVariable("r", typeof(ExpandoObject));
        var candidate = new ExecutionVariable("rCandidate", typeof(ExpandoObject));
        var rightRows = new ExecutionVariable("rRows", typeof(object));

        return new ExecutionPlan(
            "Q_DynamicAsOf",
            [rightShape],
            new ExecutionBlock(
            [
                new ExecutionAsOfProbe(
                    match,
                    candidate,
                    new ExecutionRowStream(rightRows, ExecutionRowStreamKind.Rows),
                    [],
                    new ExecutionLiteral(2, typeof(int)),
                    new ExecutionFieldRead("rCandidate", "Score", typeof(int), new ExpandoDictionaryAccess("Score")),
                    BinaryOpKind.GreaterOrEqual,
                    ExecutionBlock.Empty)
            ]));
    }
}
