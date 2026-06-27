using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static string? GetTypedPluginWindowDispatchUnsupportedReason(
        WindowRegistration registration,
        PluginWindowArgumentsBuildResult arguments)
    {
        if (IsBuiltInValueAccessWindowFunction(registration.FunctionName) ||
            IsNtileWindowFunction(registration.FunctionName))
        {
            return null;
        }

        if (TryGetBuiltInPluginWindowArgumentCount(registration.FunctionName) != null)
        {
            return $"Execution IR window lowering requires a generated typed kernel for {registration.FunctionName}; object plugin helper fallback is disabled.";
        }

        if (registration.Function == null)
            return CreateTypedPluginWindowDiagnostic(registration, "the factory method was not resolved.");

        if (!CanLowerTypedPluginWindowFrame(registration))
        {
            return CreateTypedPluginWindowDiagnostic(
                registration,
                "custom plugin windows support only whole-partition or running streaming frames.");
        }

        if (!WindowRegistrationLoweringHelpers.TryGetPluginWindowTypes(registration.Function, out var inputType, out var resultType))
        {
            return CreateTypedPluginWindowDiagnostic(
                registration,
                "the factory return type does not expose IWindowFunction<TInput,TResult>.");
        }

        if (inputType == typeof(object) || resultType == typeof(object))
        {
            return CreateTypedPluginWindowDiagnostic(
                registration,
                $"found object-shaped IWindowFunction<{FormatType(inputType)},{FormatType(resultType)}>.");
        }

        if (!CanPassValueToTypedPluginInput(arguments.Value.ReturnType, inputType))
        {
            return CreateTypedPluginWindowDiagnostic(
                registration,
                $"the typed input {FormatType(inputType)} does not match the value argument type {FormatType(arguments.Value.ReturnType)}.");
        }

        if (resultType != registration.ReturnType)
        {
            return CreateTypedPluginWindowDiagnostic(
                registration,
                $"the typed result {FormatType(resultType)} does not match the window result type {FormatType(registration.ReturnType)}.");
        }

        if (arguments.RowScopedArguments.Any(static rowScoped => rowScoped))
        {
            return CreateTypedPluginWindowDiagnostic(
                registration,
                "row-scoped extra arguments would require per-row object argument buffers.");
        }

        if (arguments.Arguments.Count > 7)
        {
            return CreateTypedPluginWindowDiagnostic(
                registration,
                $"typed plugin argument dispatch supports up to 7 extra arguments. Found {arguments.Arguments.Count.ToString(CultureInfo.InvariantCulture)}.");
        }

        var objectArgument = arguments.Arguments.FirstOrDefault(static argument => argument.ReturnType == typeof(object));
        if (objectArgument != null)
        {
            return CreateTypedPluginWindowDiagnostic(
                registration,
                "extra argument type object is not supported for no-boxing window execution.");
        }

        if (arguments.Arguments.Count > 0)
        {
            var argumentTypes = arguments.Arguments.Select(static argument => argument.ReturnType).ToArray();
            if (!TryCreateTypedWindowFunctionArgumentsInterfaceType(argumentTypes, out var argumentInterfaceType))
                return CreateTypedPluginWindowDiagnostic(registration, "typed argument interface arity is not supported.");

            if (!argumentInterfaceType.IsAssignableFrom(registration.Function.ReturnType))
            {
                return CreateTypedPluginWindowDiagnostic(
                    registration,
                    $"the factory return type must expose {FormatType(argumentInterfaceType)} for extra arguments.");
            }
        }

        return null;
    }

    private static bool CanLowerTypedPluginWindowFrame(WindowRegistration registration)
    {
        if (registration.Frame == null)
            return true;

        var frame = CreateWindowFrame(registration.Frame) ??
                    throw new InvalidOperationException("Window frame metadata was expected after frame validation.");

        return IsUnboundedPrecedingToCurrentRow(frame) ||
               IsUnboundedPrecedingToUnboundedFollowing(frame);
    }

    private static bool CanPassValueToTypedPluginInput(Type valueType, Type inputType)
    {
        if (inputType == valueType)
            return true;

        if (!inputType.IsValueType && inputType.IsAssignableFrom(valueType))
            return true;

        return Nullable.GetUnderlyingType(inputType) == valueType;
    }

    private static string CreateTypedPluginWindowDiagnostic(WindowRegistration registration, string detail)
    {
        return $"Execution IR plugin window lowering requires {registration.FunctionName} to expose typed no-boxing input/result/argument contracts: IWindowFunction<TInput,TResult> with concrete non-object types and IWindowFunctionArguments<T...> for extra arguments; {detail}";
    }

    private static bool TryCreateTypedWindowFunctionArgumentsInterfaceType(
        IReadOnlyList<Type> argumentTypes,
        out Type interfaceType)
    {
        interfaceType = argumentTypes.Count switch
        {
            1 => typeof(IWindowFunctionArguments<>).MakeGenericType(argumentTypes.ToArray()),
            2 => typeof(IWindowFunctionArguments<,>).MakeGenericType(argumentTypes.ToArray()),
            3 => typeof(IWindowFunctionArguments<,,>).MakeGenericType(argumentTypes.ToArray()),
            4 => typeof(IWindowFunctionArguments<,,,>).MakeGenericType(argumentTypes.ToArray()),
            5 => typeof(IWindowFunctionArguments<,,,,>).MakeGenericType(argumentTypes.ToArray()),
            6 => typeof(IWindowFunctionArguments<,,,,,>).MakeGenericType(argumentTypes.ToArray()),
            7 => typeof(IWindowFunctionArguments<,,,,,,>).MakeGenericType(argumentTypes.ToArray()),
            _ => typeof(object)
        };

        return interfaceType != typeof(object);
    }

    private static string FormatType(Type type)
    {
        return EvaluationHelper.GetCastableType(type);
    }
}
