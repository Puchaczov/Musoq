using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Visitors;

internal static class SourceArgumentMethodValidation
{
    internal static void Validate(AccessMethodNode node, MethodInfo method)
    {
        if (!IsAliasIndependent(method))
            throw new AliasMissingException(
                "A datasource argument cannot inject a row from the source being opened. Move the call to APPLY after its source is bound.",
                node.SpanOrEmpty());
        if (method.GetCustomAttribute<AggregateFunctionAttribute>() != null)
            throw new CannotResolveMethodException("Aggregate calls cannot be used to open a datasource.",
                DiagnosticCode.MQ3011_AggregateNotAllowed, node.SpanOrEmpty());
    }

    internal static bool IsAliasIndependent(MethodInfo method)
    {
        return !method.GetParameters()
            .SelectMany(static parameter => parameter.GetCustomAttributes(true))
            .Any(static attribute => attribute.GetType().Name is
                nameof(InjectSpecificSourceAttribute) or "InjectSourceAttribute");
    }

}
