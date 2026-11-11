using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Converter.Exceptions;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

internal static class GeneratedExecutionIlInspection
{
    private static readonly OpCode[] OneByteOpCodes = new OpCode[0x100];
    private static readonly OpCode[] TwoByteOpCodes = new OpCode[0x100];

    static GeneratedExecutionIlInspection()
    {
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opCode)
                continue;

            var value = unchecked((ushort)opCode.Value);
            if (value < 0x100)
                OneByteOpCodes[value] = opCode;
            else if ((value & 0xff00) == 0xfe00)
                TwoByteOpCodes[value & 0xff] = opCode;
        }
    }

    private static readonly ConditionalWeakTable<CompiledQuery, Type> GeneratedTypes = new();

    internal static void RecordCompilation(BuildResult result, GeneratedAssemblyCapture capture)
    {
        if (!result.Succeeded)
            return;
        RecordCompilation(result.CompiledQuery, result.BuildItems!, capture);
    }

    private static void RecordCompilation(CompiledQuery query, BuildItems items, GeneratedAssemblyCapture capture)
    {
        var type = ResolveGeneratedType(items, capture);
        if (type is not null)
            GeneratedTypes.Add(query, type);
    }

    // IL inspection necessarily reads emitted method metadata and bytecode. The
    // reflection scope remains the exact generated type from the build receipt;
    // this lookup consumes typed artifacts without discovering private members.
    private static Type? ResolveGeneratedType(BuildItems items, GeneratedAssemblyCapture capture)
    {
        if (items.ExecutableArtifact is ClrLoadedExecutableArtifact loaded)
            return loaded.RunnableType;
        if (items.ContainsKey(BuildItemKeys.RenderingArtifact))
            return capture.RequireType(items.RenderingArtifacts.AccessToClassPath);
        if (items.ContainsKey(BuildItemKeys.AccessToClassPath))
            return capture.RequireType(items.AccessToClassPath);
        if (items.ExecutableArtifact is ClrAssemblyExecutableArtifact assembly)
            return capture.RequireType(assembly.RunnableTypeName);
        return null;
    }

    internal static CompiledQuery CompileForExecution(
        string script,
        string assemblyName,
        ISchemaProvider schemaProvider,
        ILoggerResolver loggerResolver,
        CompilationOptions? compilationOptions = null
    )
    {
        using var capture = new GeneratedAssemblyCapture();
        var result = InstanceCreator.CompileWithDiagnostics(
            script,
            assemblyName,
            schemaProvider,
            loggerResolver,
            compilationOptions
        );
        if (!result.Succeeded)
            throw result.CaughtException is { } cause
                ? new MusoqQueryException(result.ToEnvelopes(), cause)
                : new MusoqQueryException(result.ToEnvelopes());
        RecordCompilation(result, capture);
        return result.CompiledQuery;
    }

    internal static CompiledQuery CompileForExecution(
        string script,
        string assemblyName,
        ISchemaProvider provider,
        ILoggerResolver logger,
        Func<BuildChain> createChain,
        Action<BuildItems> modify
    )
    {
        using var capture = new GeneratedAssemblyCapture();
        BuildItems? capturedItems = null;
        var query = InstanceCreator.CompileForExecution(
            script,
            assemblyName,
            provider,
            logger,
            createChain,
            items =>
            {
                capturedItems = items;
                modify(items);
            }
        );
        RecordCompilation(
            query,
            capturedItems ?? throw new InvalidOperationException("Build artifacts were not captured."),
            capture
        );
        return query;
    }

    internal static IEnumerable<MethodInfo> GetGeneratedComputeMethods(CompiledQuery query)
    {
        var runnableType = GeneratedTypes.GetValue(
            query,
            static _ =>
                throw new InvalidOperationException("No generated assembly receipt was retained for this query.")
        );
        var methods = runnableType
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static method => method.Name.StartsWith("Compute", StringComparison.Ordinal))
            .Concat(
                runnableType
                    .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(static type => type.Name.StartsWith("<Compute", StringComparison.Ordinal))
                    .SelectMany(static type =>
                        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    )
                    .Where(static method => method.Name == "MoveNext")
            )
            .ToArray();
        Assert.IsNotEmpty(methods, "IL inspection must include actual compute or streaming iterator methods.");
        var reflectionCalls = methods.SelectMany(GetReflectionExecutionCalls).ToArray();
        Assert.IsEmpty(
            reflectionCalls,
            "Generated execution must not invoke runtime reflection: " + string.Join(", ", reflectionCalls)
        );
        return methods;
    }

    private static IEnumerable<string> GetReflectionExecutionCalls(MethodInfo method)
    {
        var bytes = method.GetMethodBody()?.GetILAsByteArray();
        if (bytes is null)
            yield break;
        for (var position = 0; position < bytes.Length; )
        {
            var operation = ReadOpCode(bytes, ref position);
            if (operation.OperandType == OperandType.InlineMethod)
            {
                var called = method.Module.ResolveMethod(
                    BitConverter.ToInt32(bytes, position),
                    method.DeclaringType?.GetGenericArguments(),
                    method.GetGenericArguments()
                );
                if (called is not null && IsReflectionExecutionOperation(called))
                    yield return method.Name + " -> " + called;
            }
            position += GetOperandSize(operation.OperandType, bytes, position);
        }
    }

    private static bool IsReflectionExecutionOperation(MethodBase method)
    {
        var owner = method.DeclaringType;
        if (owner is null)
            return false;
        if (typeof(MethodBase).IsAssignableFrom(owner))
            return method.Name == "Invoke";
        if (typeof(Delegate).IsAssignableFrom(owner))
            return method.Name == "DynamicInvoke";
        if (owner == typeof(Activator))
            return method.Name == "CreateInstance";
        return (typeof(PropertyInfo).IsAssignableFrom(owner) || typeof(FieldInfo).IsAssignableFrom(owner))
            && method.Name is "GetValue" or "SetValue";
    }

    internal static bool ContainsBoxOpcode(MethodInfo method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il == null || il.Length == 0)
            return false;

        for (var index = 0; index < il.Length; )
        {
            var opCode = ReadOpCode(il, ref index);
            if (opCode == OpCodes.Box)
                return true;

            index += GetOperandSize(opCode.OperandType, il, index);
        }

        return false;
    }

    private static OpCode ReadOpCode(byte[] il, ref int index)
    {
        var value = il[index++];
        if (value != 0xfe)
            return OneByteOpCodes[value];

        return TwoByteOpCodes[il[index++]];
    }

    private static int GetOperandSize(OperandType operandType, byte[] il, int operandIndex)
    {
        return operandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineBrTarget
            or OperandType.InlineField
            or OperandType.InlineI
            or OperandType.InlineMethod
            or OperandType.InlineSig
            or OperandType.InlineString
            or OperandType.InlineTok
            or OperandType.InlineType => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.ShortInlineR => 4,
            OperandType.InlineSwitch => 4 + (BitConverter.ToInt32(il, operandIndex) * 4),
            _ => throw new ArgumentOutOfRangeException(nameof(operandType), operandType, null),
        };
    }
}
