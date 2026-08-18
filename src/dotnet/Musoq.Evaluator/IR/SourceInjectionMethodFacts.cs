using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR;

internal static class SourceInjectionMethodFacts
{
    public static ParameterInfo? FindInjectedSourceParameter(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        return method.GetParameters()
            .FirstOrDefault(static parameter => parameter.GetCustomAttributes(true)
                .OfType<InjectTypeAttribute>()
                .Any(static attribute => IsSourceInjectionAttribute(attribute)));
    }

    private static bool IsSourceInjectionAttribute(InjectTypeAttribute attribute)
    {
        return attribute.GetType().Name is nameof(InjectSpecificSourceAttribute) or "InjectSourceAttribute";
    }
}
