using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionPlanPrinterTests
{
    [TestMethod]
    public void Print_WhenPlanContainsPlainScanFilterProject_ShouldReturnStableText()
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
        var plan = new ExecutionPlan(
            "Q_Plain",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(source, sourceRows, sourceBinding),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEach(
                    source,
                    new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Rows),
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
            ]));

        var expected = string.Join("\n",
            "ExecutionPlan [Q_Plain]",
            "  Shapes",
            "    SourceEntity [p: Person]",
            "      Name: string <- property Name",
            "      Age: int <- property Age",
            "    Generated [ResultRow0]",
            "      Name: string <- field Name",
            string.Empty,
            "  Body",
            "    SourceScan [p: Person] -> pRows",
            "    CreateTable [result: ResultRow0]",
            "    ForEach [p in pRows]",
            "      If [(p.Age > 18)]",
            "        AppendRow [result <- ResultRow0(Name: p.Name)]",
            "    ReturnTable [result]");

        Assert.AreEqual(expected, ExecutionPlanPrinter.Print(plan));
    }

    [TestMethod]
    public void Print_WhenPlanContainsCapacityHintCandidate_ShouldShowCandidateSource()
    {
        var hash = new ExecutionVariable("ordersHash", typeof(object));
        var plan = new ExecutionPlan(
            "Q_CapacityCandidate",
            [],
            new ExecutionBlock(
            [
                new ExecutionCreateHash(
                    hash,
                    typeof(int),
                    typeof(object),
                    new ExecutionRowsCapacityHintCandidate(hash, new ExecutionStoredTableRows(2)))
            ]));

        StringAssert.Contains(
            ExecutionPlanPrinter.Print(plan),
            "CreateHash [ordersHash: int -> object; capacity: Candidate(ordersHash <- _tableResults[2].Rows)]");
    }

    [TestMethod]
    public void Print_WhenPlanContainsStrategyCapacityHintCandidates_ShouldShowCandidateFormulas()
    {
        var source = new ExecutionVariable("source", typeof(object));
        var skipped = new ExecutionVariable("skipped", typeof(object));
        var taken = new ExecutionVariable("taken", typeof(object));
        var sliced = new ExecutionVariable("sliced", typeof(object));
        var plan = new ExecutionPlan(
            "Q_StrategyCapacityCandidates",
            [],
            new ExecutionBlock(
            [
                new ExecutionSkipTable(
                    source,
                    skipped,
                    1,
                    ExecutionCapacityHintCandidates.CreateSkipCandidate(skipped, source, 1)),
                new ExecutionTakeTable(
                    skipped,
                    taken,
                    2,
                    ExecutionCapacityHintCandidates.CreateTakeCandidate(taken, skipped, 2)),
                new ExecutionSliceTable(
                    taken,
                    sliced,
                    3,
                    4,
                    ExecutionCapacityHintCandidates.CreateSkipTakeCandidate(sliced, taken, 3, 4))
            ]));
        var printed = ExecutionPlanPrinter.Print(plan);

        StringAssert.Contains(printed, "SkipTable [source -> skipped, 1; capacity: Candidate(skipped <- Max(source.Count - 1, 0))]");
        StringAssert.Contains(printed, "TakeTable [skipped -> taken, 2; capacity: Candidate(taken <- Min(skipped.Count, 2))]");
        StringAssert.Contains(
            printed,
            "SliceTable [taken -> sliced, skip 3, take 4; capacity: Candidate(sliced <- Min(Max(taken.Count - 3, 0), 4))]");
    }

    [TestMethod]
    public void Print_WhenPlanContainsMethodTargetCandidates_ShouldShowCandidateMetadata()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.GetTypeName), [typeof(object)]);
        Assert.IsNotNull(method, "Expected LibraryBase.GetTypeName(object) to exist.");
        var target = new ExecutionVariable("__library", typeof(LibraryBase));
        var plan = new ExecutionPlan(
            "Q_MethodTargetCandidate",
            [],
            new ExecutionBlock(
            [
                new ExecutionMethodTargetDeclarationCandidate(target),
                new ExecutionLet(
                    new ExecutionVariable("value", typeof(string)),
                    new ExecutionMethodTargetReuseCandidate(new ExecutionMethodCall(
                        method,
                        [new ExecutionLiteral("value", typeof(object))],
                        null,
                        typeof(string),
                        null,
                        target)))
            ]));
        var printed = ExecutionPlanPrinter.Print(plan);

        StringAssert.Contains(printed, "CreateObjectCandidate [__library: LibraryBase]");
        StringAssert.Contains(printed, "Let [value: string = Candidate(GetTypeName('value') -> target __library)]");
    }

    [TestMethod]
    public void Print_WhenPlanContainsHoistCandidate_ShouldShowScopeMetadata()
    {
        var plan = new ExecutionPlan(
            "Q_HoistCandidate",
            [],
            new ExecutionBlock(
            [
                new ExecutionHoistCandidateLet(
                    new ExecutionVariable("name", typeof(string)),
                    new ExecutionFieldRead("p", "Name", typeof(string)),
                    ExecutionHoistKind.FieldRead,
                    ExecutionHoistScope.FilterCondition,
                    "field:p:Name")
            ]));
        var printed = ExecutionPlanPrinter.Print(plan);

        StringAssert.Contains(printed, "HoistCandidate [name: string = p.Name; FieldRead/FilterCondition]");
    }

    private sealed class Person
    {
        public string Name { get; init; } = string.Empty;

        public int Age { get; init; }
    }
}
