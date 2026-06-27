using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static CastExpressionSyntax CreatePluginFactoryInvocation(MethodInfo factoryMethod, TypeSyntax interfaceType)
    {
        var reflectedType = factoryMethod.ReflectedType
            ?? throw new InvalidOperationException($"Window function factory {factoryMethod.Name} does not declare a reflected type.");
        var libraryType = reflectedType.FullName!.Replace("+", ".", StringComparison.Ordinal);

        return SyntaxFactory.CastExpression(
            interfaceType,
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParenthesizedExpression(
                            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(libraryType))
                                .WithArgumentList(SyntaxFactory.ArgumentList())),
                        SyntaxFactory.IdentifierName(factoryMethod.Name)))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }

    private static TypeSyntax CreatePluginWindowFunctionType(Type inputType, Type resultType)
    {
        return SyntaxFactory.ParseTypeName(
            $"Musoq.Plugins.IWindowFunction<{EvaluationHelper.GetCastableType(inputType)}, {EvaluationHelper.GetCastableType(resultType)}>");
    }

    private static bool TryGetTypedPluginWindowCallTypes(
        ExecutionComputePluginWindow plugin,
        out Type inputType,
        out Type resultType)
    {
        inputType = typeof(object);
        resultType = typeof(object);

        if (!plugin.Results.Type.IsArray)
            return false;

        resultType = plugin.Results.Type.GetElementType() ?? typeof(object);
        if (resultType == typeof(object))
            return false;

        if (!TryGetPluginWindowTypes(plugin.FactoryMethod, out inputType, out var factoryResultType))
            return false;

        if (inputType == typeof(object) || factoryResultType == typeof(object))
            return false;

        return CanPassValueToTypedPluginInput(plugin.Value.ReturnType, inputType) &&
               factoryResultType == resultType;
    }

    private static bool CanPassValueToTypedPluginInput(Type valueType, Type inputType)
    {
        if (inputType == valueType)
            return true;

        if (!inputType.IsValueType && inputType.IsAssignableFrom(valueType))
            return true;

        return Nullable.GetUnderlyingType(inputType) == valueType;
    }

    private static bool TryGetPluginWindowTypes(MethodInfo factoryMethod, out Type inputType, out Type resultType)
    {
        var windowFunctionType = factoryMethod.ReturnType.IsGenericType &&
                                 factoryMethod.ReturnType.GetGenericTypeDefinition() == typeof(IWindowFunction<,>)
            ? factoryMethod.ReturnType
            : factoryMethod.ReturnType
                .GetInterfaces()
                .FirstOrDefault(static type =>
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IWindowFunction<,>));

        if (windowFunctionType == null)
        {
            inputType = typeof(object);
            resultType = typeof(object);
            return false;
        }

        var arguments = windowFunctionType.GetGenericArguments();
        inputType = arguments[0];
        resultType = arguments[1];
        return true;
    }
}
