using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Plugins.Attributes;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<ArgumentSyntax> CreateMethodInvocationArguments(
        ExecutionMethodCall methodCall,
        ExecutionRenderContext context)
    {
        var renderedArguments = new List<ArgumentSyntax>();
        var argumentIndex = 0;

        foreach (var parameter in methodCall.Method.RequireClrMethod().GetParameters())
        {
            var injectAttribute = parameter.GetCustomAttributes(true)
                .OfType<InjectTypeAttribute>()
                .FirstOrDefault();

            if (injectAttribute != null)
            {
                renderedArguments.Add(SyntaxFactory.Argument(CreateInjectedMethodArgument(methodCall, parameter, injectAttribute)));
                continue;
            }

            if (parameter.GetCustomAttribute<ParamArrayAttribute>() != null)
            {
                while (argumentIndex < methodCall.Arguments.Count)
                    renderedArguments.Add(SyntaxFactory.Argument(RenderExpression(methodCall.Arguments[argumentIndex++], context)));

                continue;
            }

            if (argumentIndex >= methodCall.Arguments.Count)
            {
                if (parameter.IsOptional)
                {
                    renderedArguments.Add(SyntaxFactory.Argument(CreateOptionalParameterDefaultExpression(parameter)));
                    continue;
                }

                throw new NotSupportedException($"Method {methodCall.Method.MethodName} is missing argument {parameter.Name}.");
            }

            var argument = methodCall.Arguments[argumentIndex++];
            var renderedArgument = parameter.ParameterType == typeof(bool) && argument.RequiresNullableBoolean()
                ? this.RenderBooleanCondition(argument, context)
                : RenderExpression(argument, context);
            if (argument is ExecutionLiteral { Value.Kind: ExecutionConstantKind.Null })
            {
                var parameterType = parameter.ParameterType.IsByRef
                    ? parameter.ParameterType.GetElementType()!
                    : parameter.ParameterType;
                if (!parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null)
                {
                    renderedArgument = SyntaxFactory.CastExpression(
                        CreateTypeSyntax(parameterType),
                        renderedArgument);
                }
            }

            renderedArguments.Add(SyntaxFactory.Argument(renderedArgument));
        }

        if (argumentIndex != methodCall.Arguments.Count)
            throw new NotSupportedException($"Method {methodCall.Method.MethodName} received {methodCall.Arguments.Count} IR arguments, but used {argumentIndex}.");

        return renderedArguments;
    }
}
