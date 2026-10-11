using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

    internal static IEnumerable<MethodInfo> GetGeneratedComputeMethods(CompiledQuery query)
    {
        var runnableField = typeof(CompiledQuery).GetField("_runnable", BindingFlags.Instance | BindingFlags.NonPublic) ??
                            throw new InvalidOperationException("Compiled query runnable field was not found.");
        var runnable = runnableField.GetValue(query) ??
                       throw new InvalidOperationException("Compiled query runnable was not initialized.");

        runnable = UnwrapGeneratedRunnable(runnable);
        var runnableType = runnable.GetType();
        var methods = runnableType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static method => method.Name.StartsWith("Compute", StringComparison.Ordinal))
            .Concat(runnableType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Where(static type => type.Name.StartsWith("<Compute", StringComparison.Ordinal))
                .SelectMany(static type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(static method => method.Name == "MoveNext"))
            .ToArray();
        Assert.IsNotEmpty(methods, "IL inspection must include actual compute or streaming iterator methods.");
        var reflectionCalls = methods.SelectMany(GetReflectionExecutionCalls).ToArray();
        Assert.IsEmpty(reflectionCalls, "Generated execution must not invoke runtime reflection: " + string.Join(", ", reflectionCalls));
        return methods;
    }

    private static object UnwrapGeneratedRunnable(object runnable)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        while (visited.Add(runnable))
        {
            if (runnable.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(static method => method.Name.StartsWith("Compute", StringComparison.Ordinal)))
                return runnable;
            var inner = GetRunnableFields(runnable.GetType())
                .Where(static field => typeof(IQueryRunnable).IsAssignableFrom(field.FieldType))
                .Select(field => field.GetValue(runnable)).OfType<ITableRunnable>().SingleOrDefault();
            if (inner is null)
                throw new InvalidOperationException($"No generated compute methods or runnable child on {runnable.GetType()}.");
            runnable = inner;
        }
        throw new InvalidOperationException("Runnable wrappers contain a cycle.");
    }

    private static IEnumerable<FieldInfo> GetRunnableFields(Type type)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                yield return field;
    }

    private static IEnumerable<string> GetReflectionExecutionCalls(MethodInfo method)
    {
        var bytes = method.GetMethodBody()?.GetILAsByteArray();
        if (bytes is null)
            yield break;
        for (var position = 0; position < bytes.Length;)
        {
            var operation = ReadOpCode(bytes, ref position);
            if (operation.OperandType == OperandType.InlineMethod)
            {
                var called = method.Module.ResolveMethod(BitConverter.ToInt32(bytes, position),
                    method.DeclaringType?.GetGenericArguments(), method.GetGenericArguments());
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
        return (typeof(PropertyInfo).IsAssignableFrom(owner) || typeof(FieldInfo).IsAssignableFrom(owner)) &&
               method.Name is "GetValue" or "SetValue";
    }

    internal static bool ContainsBoxOpcode(MethodInfo method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray();
        if (il == null || il.Length == 0)
            return false;

        for (var index = 0; index < il.Length;)
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
            OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI or OperandType.InlineMethod
                or OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType
                => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.ShortInlineR => 4,
            OperandType.InlineSwitch => 4 + (BitConverter.ToInt32(il, operandIndex) * 4),
            _ => throw new ArgumentOutOfRangeException(nameof(operandType), operandType, null)
        };
    }
}
