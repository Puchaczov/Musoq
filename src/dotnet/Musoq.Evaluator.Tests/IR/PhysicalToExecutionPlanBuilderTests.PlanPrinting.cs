using System;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

public sealed partial class PhysicalToExecutionPlanBuilderTests
{
    private static string PrintPlanWithoutPhaseBoundaries(ExecutionPlan plan)
    {
        var printed = ExecutionPlanPrinter.Print(plan);
        var newline = printed.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        return string.Join(
            newline,
            printed
                .Split(["\r\n", "\n"], StringSplitOptions.None)
                .Where(static line => !line.TrimStart().StartsWith("PhaseBoundary [", StringComparison.Ordinal)));
    }
}
