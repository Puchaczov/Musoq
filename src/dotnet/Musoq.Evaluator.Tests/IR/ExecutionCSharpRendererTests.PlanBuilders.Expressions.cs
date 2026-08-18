using System;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    private static ExecutionPlan CreateConstantInCheckPlan(string queryName, int valueCount)
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
        var values = Enumerable.Range(0, valueCount)
            .Select(index => (object)((char)('A' + index)).ToString())
            .ToArray();
        var constantSet = new ExecutionConstantInSet(
            typeof(string),
            values,
            GetConstantInSetKind(valueCount));
        var predicate = new ExecutionInCheck(
            new ExecutionFieldRead("p", "Name", typeof(string)),
            values.Select(value => new ExecutionLiteral(value, typeof(string))).ToArray(),
            typeof(bool),
            constantSet);

        return new ExecutionPlan(
            queryName,
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
    }

    private static ExecutionConstantInSetKind GetConstantInSetKind(int valueCount)
    {
        if (valueCount > PrimitiveArrayInValueThreshold)
            return ExecutionConstantInSetKind.FrozenSet;

        if (valueCount > DefaultArrayInValueThreshold)
            return ExecutionConstantInSetKind.Switch;

        return ExecutionConstantInSetKind.Array;
    }

    private static ExecutionPlan CreateScalarEnumerableLoopPlan()
    {
        return CreateScalarEnumerableLoopPlan(breakAfterFirstRow: false);
    }

    private static ExecutionPlan CreateScalarEnumerableBreakLoopPlan()
    {
        return CreateScalarEnumerableLoopPlan(breakAfterFirstRow: true);
    }

    private static ExecutionPlan CreateScalarEnumerableLoopPlan(bool breakAfterFirstRow)
    {
        var sourceShape = new SourceEntityShape(
            "n",
            typeof(int),
            [
                new FieldBinding("Value", "n.Value", 0, typeof(int), FieldNullability.Unknown, new DirectScalarValueAccess())
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Value", "Value", 0, typeof(int), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))
            ]);
        var numbers = new ExecutionVariable("numbers", typeof(int[]));
        var sourceRows = new ExecutionVariable("nRows", typeof(object));
        var source = new ExecutionVariable("n", typeof(int));
        var resultTable = new ExecutionVariable("result", typeof(object));
        var append = new ExecutionAppendRow(
            resultTable,
            resultShape,
            [new ExecutionRowValue("Value", new ExecutionFieldRead("n", "Value", typeof(int), new DirectScalarValueAccess()))]);
        var loopBody = breakAfterFirstRow
            ? new ExecutionBlock([append, new ExecutionBreak()])
            : new ExecutionBlock([append]);

        return new ExecutionPlan(
            breakAfterFirstRow ? "Q_ScalarEnumerableBreakLoop" : "Q_ScalarEnumerableLoop",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionLet(numbers, new ExecutionLiteral(null, typeof(int[]))),
                new ExecutionEnumerableSource(
                    sourceRows,
                    new ExecutionVariableRead(numbers),
                    typeof(int[]),
                    ExecutionEnumerableChunkMode.DirectScalar),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEach(
                    source,
                    new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks),
                    loopBody),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateScalarMethodCallEnumerableLoopPlan()
    {
        var sourceShape = new SourceEntityShape(
            "s",
            typeof(string),
            [
                new FieldBinding("Value", "s.Value", 0, typeof(string), FieldNullability.Unknown, new DirectScalarValueAccess())
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Value", "Value", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))
            ]);
        var method = typeof(Library).GetMethod(nameof(Library.JustReturnArrayOfString), Type.EmptyTypes) ??
                     throw new InvalidOperationException("Access method was not found.");
        var sourceRows = new ExecutionVariable("sRows", typeof(object));
        var source = new ExecutionVariable("s", typeof(string));
        var resultTable = new ExecutionVariable("result", typeof(object));
        var library = new ExecutionVariable("library0", typeof(Library));

        return new ExecutionPlan(
            "Q_ScalarMethodCallEnumerableLoop",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateObject(library),
                new ExecutionEnumerableSource(
                    sourceRows,
                    new ExecutionMethodCall(method, [], null, typeof(string[]), null, library),
                    typeof(string[]),
                    ExecutionEnumerableChunkMode.DirectScalar),
                new ExecutionCreateTable(resultTable, resultShape),
                new ExecutionForEach(
                    source,
                    new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Chunks),
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [new ExecutionRowValue("Value", new ExecutionFieldRead("s", "Value", typeof(string), new DirectScalarValueAccess()))])
                    ])),
                new ExecutionReturnTable(resultTable)
            ]));
    }

    private static ExecutionPlan CreateMethodCallPlan()
    {
        return CreateProjectionPlan("Q_MethodCall", "UpperName", typeof(string), CreateToUpperNameCall());
    }

    private static ExecutionPlan CreateMethodCallInsideBinaryPlan()
    {
        var expression = new ExecutionBinary(
            BinaryOpKind.StringConcatenate,
            CreateToUpperNameCall(),
            new ExecutionLiteral("!", typeof(string)),
            typeof(string));

        return CreateProjectionPlan("Q_MethodCallInsideBinary", "UpperName", typeof(string), expression);
    }

    private static ExecutionPlan CreateMethodCallInsideArithmeticBinaryPlan()
    {
        var expression = new ExecutionBinary(
            BinaryOpKind.Add,
            CreateMathAbsCall(),
            new ExecutionLiteral(1u, typeof(uint)),
            typeof(uint));

        return CreateProjectionPlan("Q_MethodCallInsideArithmeticBinary", "Score", typeof(uint), expression);
    }

    private static ExecutionPlan CreateMethodCallInsideUnaryPlan()
    {
        var expression = new ExecutionUnary(
            UnaryOpKind.Negate,
            CreateMathAbsCall(),
            typeof(int));

        return CreateProjectionPlan("Q_MethodCallInsideUnary", "Score", typeof(int), expression);
    }

    private static ExecutionPlan CreateNullableMethodCallInsideUnaryPlan()
    {
        var expression = new ExecutionUnary(
            UnaryOpKind.Negate,
            CreateToFloatCall(),
            typeof(float?));

        return CreateProjectionPlan("Q_NullableMethodCallInsideUnary", "Score", typeof(float?), expression);
    }

    private static ExecutionPlan CreateNullableMethodCallInsideBinaryPlan()
    {
        var expression = new ExecutionBinary(
            BinaryOpKind.Add,
            CreateToFloatCall(),
            new ExecutionLiteral(1ul, typeof(ulong)),
            typeof(float));

        return CreateProjectionPlan("Q_NullableMethodCallInsideBinary", "Score", typeof(float), expression);
    }

    private static ExecutionPlan CreateCharStringEqualityPlan()
    {
        var expression = new ExecutionBinary(
            BinaryOpKind.Equal,
            new ExecutionLiteral('A', typeof(char)),
            new ExecutionLiteral("A", typeof(string)),
            typeof(bool));

        return CreateProjectionPlan("Q_CharStringEquality", "Matches", typeof(bool), expression);
    }

    private static ExecutionPlan CreateNullableTemporalSubtractionPlan()
    {
        var expression = new ExecutionBinary(
            BinaryOpKind.Subtract,
            new ExecutionFieldRead("p", "Start", typeof(DateTime?)),
            new ExecutionFieldRead("p", "End", typeof(DateTime?)),
            typeof(TimeSpan));

        return CreateProjectionPlan(
            "Q_NullableTemporalSubtraction",
            "Duration",
            typeof(TimeSpan?),
            expression,
            typeof(TimeSpan));
    }

    private static ExecutionPlan CreateProjectTablePlan()
    {
        var sourceShape = new GeneratedRowShape(
            "WorkingRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Name", "Name", 0, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ],
            [
                new FieldBinding("p", "p", 0, typeof(object), FieldNullability.Unknown, new ContextAccess(0))
            ]);
        var working = new ExecutionVariable("working", typeof(object));
        var result = new ExecutionVariable("result", typeof(object));

        return new ExecutionPlan(
            "Q_ProjectTable",
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionCreateTable(working, sourceShape),
                new ExecutionProjectTable(
                    working,
                    result,
                    resultShape,
                    [0]),
                new ExecutionReturnTable(result)
            ]));
    }
}
