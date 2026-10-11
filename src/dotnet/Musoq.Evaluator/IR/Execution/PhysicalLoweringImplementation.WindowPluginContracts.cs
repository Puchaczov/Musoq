using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static string? GetTypedPluginWindowDispatchUnsupportedReason(WindowRegistration registration)
    {
        if (registration.Function?.GetCustomAttribute<WindowFunctionAttribute>() is { KernelType: not null, ContextType: not null })
            return null;
        return $"Window '{registration.FunctionName}' requires a registered typed kernel operation; legacy factory execution is unsupported.";
    }
}
