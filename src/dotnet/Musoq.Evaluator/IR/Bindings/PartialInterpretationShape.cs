using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Bindings;

internal static class PartialInterpretationShape
{
    internal static ColumnSchema[] CreateColumns(string schemaName) =>
    [
        new("ParsedFields", typeof(object), 0, $"Musoq.Generated.Interpreters.{schemaName}"),
        new("ErrorField", typeof(string), 1),
        new("ErrorMessage", typeof(string), 2),
        new("BytesConsumed", typeof(int), 3),
        new("CompletedFields", typeof(IReadOnlySet<string>), 4),
        new("IsSuccess", typeof(bool), 5),
        new("BitOffset", typeof(int), 6)
    ];
}
