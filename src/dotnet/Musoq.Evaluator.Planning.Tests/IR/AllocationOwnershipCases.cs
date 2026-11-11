using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

internal static class AllocationOwnershipCases
{
    private static readonly (Type Type, string Property)[] Properties =
    [
        (typeof(ExecutionBlock), "Nodes"),
        (typeof(ExecutionCreateGeneratedRow), "Values"),
        (typeof(ExecutionCreateGeneratedRow), "Contexts"),
        (typeof(ExecutionCreateHashPayload), "Values"),
        (typeof(ExecutionPlan), "Shapes"),
        (typeof(ExecutionAggregateCall), "Arguments"),
        (typeof(ExecutionCaseWhen), "Branches"),
        (typeof(ExecutionCoalesce), "Expressions"),
        (typeof(ExecutionContextArray), "Segments"),
        (typeof(ExecutionInCheck), "Values"),
        (typeof(ExecutionMethodCall), "Arguments"),
        (typeof(ExecutionValueTupleKey), "Parts"),
        (typeof(GeneratedRecordShape), "Fields"),
        (typeof(GeneratedRowShape), "Fields"),
        (typeof(GeneratedRowShape), "Contexts"),
        (typeof(HashPayloadShape), "Fields"),
        (typeof(HashPayloadShape), "Contexts"),
        (typeof(SourceEntityShape), "Fields"),
        (typeof(TableRowShape), "Fields"),
        (typeof(TableRowShape), "Contexts"),
    ];

    internal static IEnumerable<object[]> Cases() =>
        Properties.SelectMany(entry =>
            new[] { "Array", "List", "ReadOnly" }.Select(kind => new object[] { entry.Type, entry.Property, kind })
        );

    internal static void AssertOwns(Type type, string property, string kind, bool clone)
    {
        var factory = new AllocationTraversalFixtures(true);
        if (type == typeof(ExecutionBlock) && property == "Nodes")
        {
            AssertOwnership<ExecutionBlock, global::Musoq.Evaluator.IR.Execution.ExecutionNode>(
                values => new ExecutionBlock(values),
                (record, values) => record with { Nodes = values },
                record => record.Nodes,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.ExecutionNode)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionNode)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(ExecutionCreateGeneratedRow) && property == "Values")
        {
            AssertOwnership<ExecutionCreateGeneratedRow, global::Musoq.Evaluator.IR.Execution.ExecutionRowValue>(
                values => new ExecutionCreateGeneratedRow(
                    (global::Musoq.Evaluator.IR.Execution.ExecutionVariable)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionVariable)),
                    (global::Musoq.Evaluator.IR.Execution.GeneratedRowShape)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.GeneratedRowShape)),
                    values,
                    (global::System.Collections.Generic.IReadOnlyList<global::Musoq.Evaluator.IR.Execution.ExecutionExpression>)
                        factory.Create(
                            typeof(global::System.Collections.Generic.IReadOnlyList<global::Musoq.Evaluator.IR.Execution.ExecutionExpression>)
                        ),
                    (global::Musoq.Evaluator.IR.Execution.ExecutionContextLayout)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionContextLayout))
                ),
                (record, values) => record with { Values = values },
                record => record.Values,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.ExecutionRowValue)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionRowValue)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(ExecutionCreateGeneratedRow) && property == "Contexts")
        {
            AssertOwnership<ExecutionCreateGeneratedRow, global::Musoq.Evaluator.IR.Execution.ExecutionExpression>(
                values => new ExecutionCreateGeneratedRow(
                    (global::Musoq.Evaluator.IR.Execution.ExecutionVariable)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionVariable)),
                    (global::Musoq.Evaluator.IR.Execution.GeneratedRowShape)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.GeneratedRowShape)),
                    (global::System.Collections.Generic.IReadOnlyList<global::Musoq.Evaluator.IR.Execution.ExecutionRowValue>)
                        factory.Create(
                            typeof(global::System.Collections.Generic.IReadOnlyList<global::Musoq.Evaluator.IR.Execution.ExecutionRowValue>)
                        ),
                    values,
                    (global::Musoq.Evaluator.IR.Execution.ExecutionContextLayout)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionContextLayout))
                ),
                (record, values) => record with { Contexts = values },
                record => record.Contexts,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.ExecutionExpression)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionExpression)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(ExecutionCreateHashPayload) && property == "Values")
        {
            AssertOwnership<ExecutionCreateHashPayload, global::Musoq.Evaluator.IR.Execution.ExecutionRowValue>(
                values => new ExecutionCreateHashPayload(
                    (global::Musoq.Evaluator.IR.Execution.ExecutionVariable)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionVariable)),
                    (global::Musoq.Evaluator.IR.Execution.HashPayloadShape)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.HashPayloadShape)),
                    values
                ),
                (record, values) => record with { Values = values },
                record => record.Values,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.ExecutionRowValue)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionRowValue)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(ExecutionPlan) && property == "Shapes")
        {
            AssertOwnership<ExecutionPlan, global::Musoq.Evaluator.IR.Execution.RowShape>(
                values => new ExecutionPlan("ownership", values, new ExecutionBlock([])),
                (record, values) => record with { Shapes = values },
                record => record.Shapes,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.RowShape)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.RowShape)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(ExecutionAggregateCall) && property == "Arguments")
        {
            AssertOwnership<ExecutionAggregateCall, global::Musoq.Evaluator.IR.Execution.ExecutionExpression>(
                values => new ExecutionAggregateCall(
                    (global::Musoq.Evaluator.IR.Execution.ExecutionVariable)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionVariable)),
                    (global::Musoq.Evaluator.IR.Execution.ExecutionCallableRef)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionCallableRef)),
                    values,
                    (global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef)),
                    (global::Musoq.Evaluator.IR.Execution.AggregateAccumulatorField)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.AggregateAccumulatorField)),
                    (string)factory.Create(typeof(string))
                ),
                (record, values) => record with { Arguments = values },
                record => record.Arguments,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.ExecutionExpression)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionExpression)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(ExecutionCaseWhen) && property == "Branches")
        {
            AssertOwnership<ExecutionCaseWhen, global::Musoq.Evaluator.IR.Execution.ExecutionCaseWhenBranch>(
                values => new ExecutionCaseWhen(
                    values,
                    (global::Musoq.Evaluator.IR.Execution.ExecutionExpression)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionExpression)),
                    (global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef))
                ),
                (record, values) => record with { Branches = values },
                record => record.Branches,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.ExecutionCaseWhenBranch)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionCaseWhenBranch)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(ExecutionCoalesce) && property == "Expressions")
        {
            AssertOwnership<ExecutionCoalesce, global::Musoq.Evaluator.IR.Execution.ExecutionExpression>(
                values => new ExecutionCoalesce(
                    values,
                    (global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef))
                ),
                (record, values) => record with { Expressions = values },
                record => record.Expressions,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.ExecutionExpression)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionExpression)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(ExecutionContextArray) && property == "Segments")
        {
            AssertOwnership<ExecutionContextArray, global::Musoq.Evaluator.IR.Execution.ExecutionContextSegment>(
                values => new ExecutionContextArray(values),
                (record, values) => record with { Segments = values },
                record => record.Segments,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.ExecutionContextSegment)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionContextSegment)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(ExecutionInCheck) && property == "Values")
        {
            AssertOwnership<ExecutionInCheck, global::Musoq.Evaluator.IR.Execution.ExecutionExpression>(
                values => new ExecutionInCheck(
                    (global::Musoq.Evaluator.IR.Execution.ExecutionExpression)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionExpression)),
                    values,
                    (global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef)),
                    (global::Musoq.Evaluator.IR.Execution.ExecutionConstantInSet)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionConstantInSet)),
                    (bool)factory.Create(typeof(bool))
                ),
                (record, values) => record with { Values = values },
                record => record.Values,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.ExecutionExpression)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionExpression)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(ExecutionMethodCall) && property == "Arguments")
        {
            AssertOwnership<ExecutionMethodCall, global::Musoq.Evaluator.IR.Execution.ExecutionExpression>(
                values => new ExecutionMethodCall(
                    (global::Musoq.Evaluator.IR.Execution.ExecutionCallableRef)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionCallableRef)),
                    values,
                    (string)factory.Create(typeof(string)),
                    (global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef)),
                    (global::Musoq.Evaluator.IR.Execution.ExecutionExpression)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionExpression)),
                    (global::Musoq.Evaluator.IR.Execution.ExecutionVariable)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionVariable)),
                    (global::Musoq.Evaluator.IR.Execution.ExecutionVariable)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionVariable))
                ),
                (record, values) => record with { Arguments = values },
                record => record.Arguments,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.ExecutionExpression)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionExpression)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(ExecutionValueTupleKey) && property == "Parts")
        {
            AssertOwnership<ExecutionValueTupleKey, global::Musoq.Evaluator.IR.Execution.ExecutionExpression>(
                values => new ExecutionValueTupleKey(
                    values,
                    (global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef))
                ),
                (record, values) => record with { Parts = values },
                record => record.Parts,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.ExecutionExpression)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionExpression)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(GeneratedRecordShape) && property == "Fields")
        {
            AssertOwnership<GeneratedRecordShape, global::Musoq.Evaluator.IR.Execution.FieldBinding>(
                values => new GeneratedRecordShape(
                    (string)factory.Create(typeof(string)),
                    values,
                    (bool)factory.Create(typeof(bool))
                ),
                (record, values) => record with { Fields = values },
                record => record.Fields,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.FieldBinding)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.FieldBinding)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(GeneratedRowShape) && property == "Fields")
        {
            AssertOwnership<GeneratedRowShape, global::Musoq.Evaluator.IR.Execution.FieldBinding>(
                values => new GeneratedRowShape((string)factory.Create(typeof(string)), values),
                (record, values) => record with { Fields = values },
                record => record.Fields,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.FieldBinding)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.FieldBinding)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(GeneratedRowShape) && property == "Contexts")
        {
            AssertOwnership<GeneratedRowShape, global::Musoq.Evaluator.IR.Execution.FieldBinding>(
                values => new GeneratedRowShape(
                    (string)factory.Create(typeof(string)),
                    (global::System.Collections.Generic.IReadOnlyList<global::Musoq.Evaluator.IR.Execution.FieldBinding>)
                        factory.Create(
                            typeof(global::System.Collections.Generic.IReadOnlyList<global::Musoq.Evaluator.IR.Execution.FieldBinding>)
                        ),
                    values,
                    (bool)factory.Create(typeof(bool)),
                    (bool)factory.Create(typeof(bool))
                ),
                (record, values) => record with { Contexts = values },
                record => record.Contexts,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.FieldBinding)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.FieldBinding)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(HashPayloadShape) && property == "Fields")
        {
            AssertOwnership<HashPayloadShape, global::Musoq.Evaluator.IR.Execution.FieldBinding>(
                values => new HashPayloadShape((string)factory.Create(typeof(string)), values),
                (record, values) => record with { Fields = values },
                record => record.Fields,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.FieldBinding)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.FieldBinding)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(HashPayloadShape) && property == "Contexts")
        {
            AssertOwnership<HashPayloadShape, global::Musoq.Evaluator.IR.Execution.FieldBinding>(
                values => new HashPayloadShape(
                    (string)factory.Create(typeof(string)),
                    (global::System.Collections.Generic.IReadOnlyList<global::Musoq.Evaluator.IR.Execution.FieldBinding>)
                        factory.Create(
                            typeof(global::System.Collections.Generic.IReadOnlyList<global::Musoq.Evaluator.IR.Execution.FieldBinding>)
                        ),
                    values
                ),
                (record, values) => record with { Contexts = values },
                record => record.Contexts,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.FieldBinding)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.FieldBinding)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(SourceEntityShape) && property == "Fields")
        {
            AssertOwnership<SourceEntityShape, global::Musoq.Evaluator.IR.Execution.FieldBinding>(
                values => new SourceEntityShape(
                    (string)factory.Create(typeof(string)),
                    (global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.ExecutionTypeRef)),
                    values,
                    (string)factory.Create(typeof(string))
                ),
                (record, values) => record with { Fields = values },
                record => record.Fields,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.FieldBinding)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.FieldBinding)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(TableRowShape) && property == "Fields")
        {
            AssertOwnership<TableRowShape, global::Musoq.Evaluator.IR.Execution.FieldBinding>(
                values => new TableRowShape((string)factory.Create(typeof(string)), values),
                (record, values) => record with { Fields = values },
                record => record.Fields,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.FieldBinding)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.FieldBinding)),
                kind,
                clone
            );
            return;
        }
        if (type == typeof(TableRowShape) && property == "Contexts")
        {
            AssertOwnership<TableRowShape, global::Musoq.Evaluator.IR.Execution.FieldBinding>(
                values => new TableRowShape(
                    (string)factory.Create(typeof(string)),
                    (global::System.Collections.Generic.IReadOnlyList<global::Musoq.Evaluator.IR.Execution.FieldBinding>)
                        factory.Create(
                            typeof(global::System.Collections.Generic.IReadOnlyList<global::Musoq.Evaluator.IR.Execution.FieldBinding>)
                        ),
                    values,
                    (string)factory.Create(typeof(string))
                ),
                (record, values) => record with { Contexts = values },
                record => record.Contexts,
                () =>
                    (global::Musoq.Evaluator.IR.Execution.FieldBinding)
                        factory.Create(typeof(global::Musoq.Evaluator.IR.Execution.FieldBinding)),
                kind,
                clone
            );
            return;
        }
        throw new ArgumentException("Unregistered ownership fixture");
    }

    private static void AssertOwnership<TRecord, TElement>(
        Func<IReadOnlyList<TElement>, TRecord> construct,
        Func<TRecord, IReadOnlyList<TElement>, TRecord> clone,
        Func<TRecord, IReadOnlyList<TElement>> read,
        Func<TElement> createElement,
        string kind,
        bool useClone
    )
    {
        var first = createElement();
        TElement[] array = [first, createElement()];
        var list = new List<TElement>(array);
        IReadOnlyList<TElement> input = kind switch
        {
            "Array" => array,
            "List" => list,
            "ReadOnly" => list.AsReadOnly(),
            _ => throw new ArgumentException("Unknown ownership storage", nameof(kind)),
        };
        var record = construct(input);
        var owned = read(useClone ? clone(record, input) : record);
        var replacement =
            typeof(TElement) == typeof(ExecutionNode) ? (TElement)(object)new ExecutionContinue() : createElement();
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreNotEqual(first, replacement);
        if (kind == "Array")
            array[0] = replacement;
        else
            list[0] = replacement;
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.HasCount(2, owned);
        Microsoft.VisualStudio.TestTools.UnitTesting.Assert.AreEqual(first, owned[0]);
    }
}
