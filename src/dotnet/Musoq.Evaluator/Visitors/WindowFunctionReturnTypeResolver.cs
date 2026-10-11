using System.Reflection;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Visitors;

internal static class WindowFunctionReturnTypeResolver
{
    internal static Type Resolve(string normalizedName, Node[] args, MethodInfo? declaration)
    {
        if (declaration?.GetCustomAttribute<WindowFunctionAttribute>()?.KernelType is not null)
            return declaration.ReturnType;
        throw new SourcePlanContractException($"Window '{normalizedName}' requires a registered typed SQL declaration and kernel.", TextSpan.Empty);
    }
}
