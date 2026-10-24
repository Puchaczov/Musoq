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

    internal static IEnumerable<object[]> Cases() => Properties.SelectMany(entry =>
        new[] { "Array", "List", "ReadOnly" }.Select(kind => new object[] { entry.Type, entry.Property, kind }));
}
