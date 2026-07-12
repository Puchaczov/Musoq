using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class ExecutionCSharpRendererTests
{
    private static ExecutionMethodCall CreateToUpperNameCall()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.ToUpper), [typeof(string)])!;
        var target = new ExecutionVariable("libraryBase0", typeof(LibraryBase));

        return new ExecutionMethodCall(
            method,
            [new ExecutionFieldRead("p", "Name", typeof(string))],
            null,
            typeof(string),
            null,
            target);
    }

    private static ExecutionMethodCall CreateMathAbsCall()
    {
        var method = typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!;

        return new ExecutionMethodCall(
            method,
            [new ExecutionLiteral(1, typeof(int))],
            null,
            typeof(int));
    }

    private static ExecutionMethodCall CreateToFloatCall()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.ToFloat), [typeof(int?)])!;
        var target = new ExecutionVariable("libraryBase0", typeof(LibraryBase));

        return new ExecutionMethodCall(
            method,
            [new ExecutionLiteral(1, typeof(int?))],
            null,
            typeof(float?),
            null,
            target);
    }

    private static ExecutionMethodCall CreateUnboundToUpperNameCall()
    {
        var method = typeof(LibraryBase).GetMethod(nameof(LibraryBase.ToUpper), [typeof(string)])!;
        return new ExecutionMethodCall(
            method,
            [new ExecutionFieldRead("p", "Name", typeof(string))],
            null,
            typeof(string));
    }

    private static ExecutionPlan CreateProjectionPlan(
        string queryName,
        string outputName,
        Type outputType,
        ExecutionExpression expression,
        Type? publicOutputType = null)
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
                new FieldBinding(outputName, outputName, 0, outputType, FieldNullability.Unknown, new GeneratedFieldAccess(outputName))
                {
                    PublicType = ExecutionTypeRef.FromOptionalClr(publicOutputType)
                }
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
        var targetDeclarations = ExecutionIrAnalysis.FlattenExpressions(expression)
            .OfType<ExecutionMethodCall>()
            .Select(static call => call.Target)
            .Where(static target => target != null)
            .GroupBy(static target => target!.Name, StringComparer.Ordinal)
            .Select(static group => new ExecutionCreateObject(group.First()!))
            .ToArray();

        return CreateFinalResultPlan(
            queryName,
            [sourceShape, resultShape],
            new ExecutionBlock(
            [
                new ExecutionSourceScan(source, sourceRows, sourceBinding),
                new ExecutionCreateTable(resultTable, resultShape),
                ..targetDeclarations,
                new ExecutionForEach(
                    source,
                    new ExecutionRowStream(sourceRows, ExecutionRowStreamKind.Rows),
                    new ExecutionBlock(
                    [
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [new ExecutionRowValue(outputName, expression)])
                    ])),
                new ExecutionReturnTable(resultTable)
            ]),
            resultTable,
            resultShape);
    }

    private static ExecutionPlan CreateDynamicPlan()
    {
        var sourceShape = new ExpandoAdapterShape(
            "p",
            "pDynamicRow0",
            typeof(ExpandoObject),
            [
                new FieldBinding("Id", "p.Id", 0, typeof(int), FieldNullability.Unknown, new ExpandoDictionaryAccess("Id")),
                new FieldBinding("Name", "p.Name", 1, typeof(string), FieldNullability.Unknown, new ExpandoDictionaryAccess("Name"))
            ]);
        var resultShape = new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("Id", "Id", 0, typeof(int), FieldNullability.Unknown, new GeneratedFieldAccess("Id")),
                new FieldBinding("Name", "Name", 1, typeof(string), FieldNullability.Unknown, new GeneratedFieldAccess("Name"))
            ]);
        var source = new ExecutionVariable("pDynamicSource", typeof(IReadOnlyDictionary<string, object>));
        var sourceRows = new ExecutionVariable("pRows", typeof(object));
        var adapter = new ExecutionVariable("p", typeof(object));
        var sourceBinding = new ExecutionSourceBinding(
            "test",
            "data",
            "p:1",
            0,
            [],
            sourceShape.Fields);
        var resultTable = new ExecutionVariable("result", typeof(object));

        return CreateFinalResultPlan(
            "Q_Dynamic",
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
                        new ExecutionAdaptExpando(adapter, source, sourceShape),
                        new ExecutionAppendRow(
                            resultTable,
                            resultShape,
                            [
                                new ExecutionRowValue("Id", new ExecutionFieldRead("p", "Id", typeof(int))),
                                new ExecutionRowValue("Name", new ExecutionFieldRead("p", "Name", typeof(string)))
                            ])
                    ])),
                new ExecutionReturnTable(resultTable)
            ]),
            resultTable,
            resultShape);
    }

}
