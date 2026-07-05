using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private List<ArgumentSyntax> CreateMethodInvocationArguments(
        ExecutionMethodCall methodCall,
        ExecutionRenderContext context)
    {
        var renderedArguments = new List<ArgumentSyntax>();
        var argumentIndex = 0;

        foreach (var parameter in methodCall.Method.GetParameters())
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

                throw new NotSupportedException($"Method {methodCall.Method.Name} is missing argument {parameter.Name}.");
            }

            renderedArguments.Add(SyntaxFactory.Argument(RenderExpression(methodCall.Arguments[argumentIndex++], context)));
        }

        if (argumentIndex != methodCall.Arguments.Count)
            throw new NotSupportedException($"Method {methodCall.Method.Name} received {methodCall.Arguments.Count} IR arguments, but used {argumentIndex}.");

        return renderedArguments;
    }
}
