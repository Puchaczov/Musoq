using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private string ResolveInterpreterTypeName(PhysicalInterpretSourceNode interpret)
    {
        if (_schemaRegistry != null &&
            _schemaRegistry.TryGetSchema(interpret.SchemaName, out var registration) &&
            registration != null)
        {
            if (!string.IsNullOrWhiteSpace(registration.GeneratedTypeName))
                return registration.GeneratedTypeName;

            if (registration.GeneratedType != null)
                return EvaluationHelper.GetCastableType(registration.GeneratedType);
        }

        if (interpret.ResultType != typeof(object) && !IsRowSourceType(interpret.ResultType))
            return EvaluationHelper.GetCastableType(interpret.ResultType);

        throw new NotSupportedException(
            $"Execution IR interpret-source lowering cannot resolve interpreter type for schema '{interpret.SchemaName}'.");
    }
}
