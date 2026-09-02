using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{

    private static bool CanRenderMethodCall(ExecutionMethodCall methodCall)
    {
        if (methodCall.EnumIntrinsic != null)
            return EnumIntrinsicExpressionRenderer.CanRender(methodCall);

        var method = methodCall.Method.RequireClrMethod();
        var declaringType = method.DeclaringType;
        if (declaringType == null || !method.IsPublic || !CanReferenceType(declaringType) || !CanReferenceType(methodCall.ReturnType))
            return false;

        if (method.ContainsGenericParameters)
            return false;

        if (methodCall.Target != null &&
            (!CanReferenceType(methodCall.Target.Type) || !declaringType.IsAssignableFrom(methodCall.Target.Type.RequireClrType())))
        {
            return false;
        }

        if (RequiresAssignedMethodTarget(methodCall))
            return false;

        if (methodCall.Cache != null && !CanReferenceType(methodCall.Cache.Type))
            return false;

        var argumentIndex = 0;
        var parameters = method.GetParameters();
        foreach (var parameter in parameters)
        {
            var injectAttribute = parameter.GetCustomAttributes(true)
                .OfType<InjectTypeAttribute>()
                .FirstOrDefault();

            if (injectAttribute != null)
            {
                if (!CanRenderInjectedMethodArgument(methodCall, parameter, injectAttribute))
                    return false;

                continue;
            }

            if (parameter.GetCustomAttribute<ParamArrayAttribute>() != null)
            {
                while (argumentIndex < methodCall.Arguments.Count)
                {
                    if (!CanRenderExpression(methodCall.Arguments[argumentIndex++]))
                        return false;
                }

                return true;
            }

            if (argumentIndex >= methodCall.Arguments.Count)
            {
                if (parameter.IsOptional && CanRenderOptionalParameterDefault(parameter))
                    continue;

                return false;
            }

            if (!CanRenderExpression(methodCall.Arguments[argumentIndex++]))
                return false;
        }

        return argumentIndex == methodCall.Arguments.Count;
    }

    private static bool CanRenderInjectedMethodArgument(
        ExecutionMethodCall methodCall,
        ParameterInfo parameter,
        InjectTypeAttribute injectAttribute)
    {
        if (injectAttribute is InjectQueryStatsAttribute)
            return true;

        if (injectAttribute.GetType().Name is nameof(InjectSpecificSourceAttribute) or "InjectSourceAttribute")
        {
            if (methodCall.InjectedSource != null)
                return CanRenderExpression(methodCall.InjectedSource) && CanReferenceType(parameter.ParameterType);

            return !string.IsNullOrWhiteSpace(methodCall.Alias) && CanReferenceType(parameter.ParameterType);
        }

        return false;
    }

    private static bool CanRenderOptionalParameterDefault(ParameterInfo parameter)
    {
        return !parameter.HasDefaultValue || CanRenderLiteral(parameter.DefaultValue);
    }
}
