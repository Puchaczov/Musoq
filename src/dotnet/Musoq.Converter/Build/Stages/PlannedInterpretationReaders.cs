using System.Linq;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Visitors;

namespace Musoq.Converter.Build;

internal static class PlannedInterpretationReaders
{
    internal static string? Generate(
        SchemaRegistry? registry,
        InterpretationExecutionLimits limits,
        PhysicalNode? physical
    )
    {
        if (registry is not { Count: > 0 } || physical is null)
            return null;
        var readers = BinaryFieldReaderPlan.Collect(physical).Distinct().ToArray();
        if (readers.Length == 0)
            return null;
        var generator = new InterpreterCodeGenerator(registry, limits);
        return generator.GenerateAll(InterpretationCSharpNames.Namespace, readers);
    }
}
