using System.Collections.Generic;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionVariable CreateMaterializedRowsBufferVariable(
        string name,
        GeneratedRowShape? generatedRowShape)
    {
        return string.IsNullOrWhiteSpace(generatedRowShape?.TypeName)
            ? new ExecutionVariable(name, typeof(object))
            : new ExecutionVariable(name, typeof(IReadOnlyList<Row>), generatedRowShape.TypeName);
    }
}
