using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Targets.Abstractions;

namespace Musoq.Evaluator.IR.Execution.Portability;

internal static class ExecutionPortableSymbolFactory
{
    public static ExecutionPortableTypeDescriptor FromType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (type.IsByRef)
        {
            var element = FromType(type.GetElementType()!);
            return new ExecutionPortableTypeDescriptor(
                ExecutionPortableTypeKind.ByRef,
                $"byref<{element.StableName}>",
                $"{element.DisplayName}&")
            {
                Portability = ExecutionPortableSymbolPortability.ClrOnly,
                PortabilityReason = "By-ref CLR type requires in-process CLR calling semantics.",
                Arguments = [element]
            };
        }

        if (type.IsGenericParameter)
        {
            return new ExecutionPortableTypeDescriptor(
                ExecutionPortableTypeKind.GenericParameter,
                $"generic-parameter:{type.GenericParameterPosition}",
                type.Name)
            {
                Portability = ExecutionPortableSymbolPortability.Portable,
                PortabilityReason = "Generic parameter is target-neutral until bound by a concrete symbol."
            };
        }

        if (ExecutionPortableSymbolCatalog.TryGetPrimitiveName(type, out var primitiveName))
        {
            return new ExecutionPortableTypeDescriptor(
                ExecutionPortableTypeKind.Primitive,
                $"primitive:{primitiveName}",
                primitiveName)
            {
                Portability = ExecutionPortableSymbolPortability.Portable,
                PortabilityReason = "Explicit primitive type catalog entry."
            };
        }

        if (type == typeof(object))
        {
            return new ExecutionPortableTypeDescriptor(
                ExecutionPortableTypeKind.HostOpaque,
                "host-opaque:dynamic-object",
                "object")
            {
                Portability = ExecutionPortableSymbolPortability.HostImport,
                PortabilityReason = "Object requires host-provided tagged dynamic value semantics."
            };
        }

        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null)
        {
            var argument = FromType(nullable);
            return new ExecutionPortableTypeDescriptor(
                ExecutionPortableTypeKind.Nullable,
                $"nullable<{argument.StableName}>",
                $"{argument.DisplayName}?")
            {
                Portability = argument.Portability,
                PortabilityReason = $"Nullable wrapper inherits underlying portability: {argument.PortabilityReason}",
                Arguments = [argument]
            };
        }

        if (type.IsArray)
        {
            var element = FromType(type.GetElementType()!);
            var rank = type.GetArrayRank();
            return new ExecutionPortableTypeDescriptor(
                ExecutionPortableTypeKind.Array,
                $"array:{rank}<{element.StableName}>",
                rank == 1 ? $"{element.DisplayName}[]" : $"{element.DisplayName}[{new string(',', rank - 1)}]")
            {
                Portability = element.Portability,
                PortabilityReason = $"Array wrapper inherits element portability: {element.PortabilityReason}",
                Arguments = [element],
                ArrayRank = rank
            };
        }

        if (type.IsGenericType)
        {
            var arguments = type.GetGenericArguments().Select(FromType).ToArray();
            var definition = type.GetGenericTypeDefinition();
            if (ExecutionPortableSymbolCatalog.TryGetPortableContainer(definition, out var container))
            {
                var portability = CombinePortability(arguments, ExecutionPortableSymbolPortability.Portable);
                return new ExecutionPortableTypeDescriptor(
                    container.TypeKind,
                    $"{container.StableName}<{string.Join(",", arguments.Select(static argument => argument.StableName))}>",
                    $"{container.StableName}<{string.Join(", ", arguments.Select(static argument => argument.DisplayName))}>")
                {
                    Portability = portability,
                    PortabilityReason = portability == ExecutionPortableSymbolPortability.Portable
                        ? $"Explicit portable {container.Contract.Kind} container contract."
                        : $"{container.Contract.Kind} container inherits non-portable argument requirements: {FormatNonPortableArgumentReasons(arguments)}",
                    Arguments = arguments,
                    Container = container.Contract
                };
            }

            if (ExecutionPortableSymbolCatalog.TryGetHostImportTypeReason(definition, out var hostImportReason))
            {
                return new ExecutionPortableTypeDescriptor(
                    ExecutionPortableTypeKind.HostOpaque,
                    $"host-opaque:{CreateVersionFreeClrIdentity(definition)}<{string.Join(",", arguments.Select(static argument => argument.StableName))}>",
                    $"{definition.FullName ?? definition.Name}<{string.Join(", ", arguments.Select(static argument => argument.DisplayName))}>")
                {
                    Portability = CombinePortability(arguments, ExecutionPortableSymbolPortability.HostImport),
                    PortabilityReason = hostImportReason,
                    Arguments = arguments
                };
            }

            return new ExecutionPortableTypeDescriptor(
                ExecutionPortableTypeKind.ClrOnly,
                $"clr:{CreateVersionFreeClrIdentity(definition)}<{string.Join(",", arguments.Select(static argument => argument.StableName))}>",
                $"{definition.FullName ?? definition.Name}<{string.Join(", ", arguments.Select(static argument => argument.DisplayName))}>")
            {
                Portability = ExecutionPortableSymbolPortability.ClrOnly,
                PortabilityReason = $"No portable container or host contract exists for CLR generic type '{definition.FullName ?? definition.Name}'.",
                Arguments = arguments
            };
        }

        var clrPortability = ExecutionPortableSymbolCatalog.TryGetHostImportTypeReason(type, out var hostOpaqueReason)
            ? ExecutionPortableSymbolPortability.HostImport
            : ExecutionPortableSymbolPortability.ClrOnly;
        return new ExecutionPortableTypeDescriptor(
            clrPortability == ExecutionPortableSymbolPortability.HostImport
                ? ExecutionPortableTypeKind.HostOpaque
                : ExecutionPortableTypeKind.ClrOnly,
            $"{(clrPortability == ExecutionPortableSymbolPortability.HostImport ? "host-opaque" : "clr")}:{CreateVersionFreeClrIdentity(type)}",
            type.FullName ?? type.Name)
        {
            Portability = clrPortability,
            PortabilityReason = clrPortability == ExecutionPortableSymbolPortability.HostImport
                ? hostOpaqueReason
                : $"No portable catalog entry for CLR type '{type.FullName ?? type.Name}'."
        };
    }

    public static ExecutionPortableTypeDescriptor GeneratedRow(
        string displayName,
        IEnumerable<ExecutionPortableRowFieldDescriptor> fields)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Generated row display name cannot be null or whitespace.", nameof(displayName));

        ArgumentNullException.ThrowIfNull(fields);
        var fieldArray = fields.ToArray();
        var signature = string.Join(
            "|",
            fieldArray.Select((field, index) =>
                $"{index}:{field.Name}:{field.Type.StableName}:{field.Nullability}"));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature)));

        return new ExecutionPortableTypeDescriptor(
            ExecutionPortableTypeKind.GeneratedRow,
            $"generated-row:sha256:{hash}",
            displayName)
        {
            Portability = ExecutionPortableSymbolPortability.Portable,
            PortabilityReason = "Generated row shape is represented by portable ordered field metadata.",
            Fields = fieldArray
        };
    }

    public static ExecutionPortableCallableDescriptor FromMethod(MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(method);

        var declaringType = method.DeclaringType != null
            ? FromType(method.DeclaringType)
            : new ExecutionPortableTypeDescriptor(
                ExecutionPortableTypeKind.ClrOnly,
                "clr:<unknown>",
                "<unknown>")
            {
                Portability = ExecutionPortableSymbolPortability.ClrOnly,
                PortabilityReason = "Method has no declaring type and cannot be mapped to a portable callable."
            };
        var parameters = method.GetParameters()
            .Select(static parameter => FromType(parameter.ParameterType))
            .ToArray();
        var returnType = FromType(method.ReturnType);
        var genericArity = method.IsGenericMethod ? method.GetGenericArguments().Length : 0;
        var stableName =
            $"method:{declaringType.StableName}.{method.Name}#g{genericArity}({string.Join(",", parameters.Select(static parameter => parameter.StableName))}):{returnType.StableName}";
        var callableKind = ExecutionPortableSymbolCatalog.ClassifyCallable(method, out var callableReason);
        var portability = callableKind == ExecutionPortableCallableKind.ClrMethod
            ? ExecutionPortableSymbolPortability.ClrOnly
            : ExecutionPortableSymbolPortability.HostImport;

        return new ExecutionPortableCallableDescriptor(
            callableKind,
            stableName,
            $"{declaringType.DisplayName}.{method.Name}")
        {
            Portability = portability,
            PortabilityReason = callableReason,
            MethodName = method.Name,
            DeclaringType = declaringType,
            ReturnType = returnType,
            ParameterTypes = parameters,
            IsStatic = method.IsStatic,
            GenericArity = genericArity,
            IntrinsicKind = ExecutionPortableSymbolCatalog.ClassifyIntrinsicCallable(method),
            InvocationMode = method.IsDefined(typeof(ExtensionAttribute), inherit: false)
                ? ExecutionCallableInvocationMode.Extension
                : method.IsStatic
                    ? ExecutionCallableInvocationMode.Static
                    : ExecutionCallableInvocationMode.Instance,
            IsStable = ExpressionStabilityAnalyzer.IsStableMethod(method)
        };
    }

    private static string CreateVersionFreeClrIdentity(Type type)
    {
        return $"{type.FullName ?? type.Name}@{type.Assembly.GetName().Name ?? "<unknown>"}";
    }

    private static ExecutionPortableSymbolPortability CombinePortability(
        IReadOnlyList<ExecutionPortableTypeDescriptor> symbols,
        ExecutionPortableSymbolPortability defaultPortability)
    {
        if (symbols.Any(static symbol => symbol.Portability == ExecutionPortableSymbolPortability.ClrOnly))
            return ExecutionPortableSymbolPortability.ClrOnly;

        return symbols.Any(static symbol => symbol.Portability == ExecutionPortableSymbolPortability.HostImport)
            ? ExecutionPortableSymbolPortability.HostImport
            : defaultPortability;
    }

    private static string FormatNonPortableArgumentReasons(
        IEnumerable<ExecutionPortableTypeDescriptor> arguments)
    {
        return string.Join(
            "; ",
            arguments
                .Where(static argument => argument.Portability != ExecutionPortableSymbolPortability.Portable)
                .Select(static argument => $"{argument.DisplayName}: {argument.PortabilityReason}"));
    }
}
