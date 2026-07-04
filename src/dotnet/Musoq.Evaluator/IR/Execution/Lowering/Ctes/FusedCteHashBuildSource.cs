using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record FusedCteHashBuildSource(
    GeneratedRowShape RowShape,
    IReadOnlyList<RowShape> DefinitionShapes,
    RowShape ProducerShape,
    ExecutionVariable ProducerVariable,
    IReadOnlyList<ExecutionNode> ProducerSetup,
    ExecutionExpression ProducerRows,
    IReadOnlyList<ExecutionRowValue> RowValues,
    IReadOnlyList<ExecutionExpression> ContextValues,
    ExecutionContextLayout? ContextLayout,
    int SchemaSourceCount,
    HashPayloadShape? HashPayloadShape);
