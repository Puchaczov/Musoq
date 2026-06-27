using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class WindowFunctionNoBoxingTests : BasicEntityTestBase
{
    private static readonly OpCode[] OneByteOpCodes = new OpCode[0x100];
    private static readonly OpCode[] TwoByteOpCodes = new OpCode[0x100];

    static WindowFunctionNoBoxingTests()
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

    [TestMethod]
    public void GeneratedWindowComputeMethods_WhenTypedWindowQueryCompiled_ShouldNotEmitBoxOpcode()
    {
        var query = @"
            select Name,
                   RowNumber() over (partition by City order by Name) as RowNo,
                   RunningProduct(Population) over (partition by City order by Name) as Product,
                   Sum(Population) over (partition by City order by Name) as RunningSum
            from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("NYC", "Alice", 2),
            new BasicEntity("NYC", "Bob", 3),
            new BasicEntity("LA", "Diana", 4));
        var vm = CreateAndRunVirtualMachine(query, sources);

        var boxedMethods = GetGeneratedComputeMethods(vm)
            .Where(ContainsBoxOpcode)
            .Select(static method => method.Name)
            .ToArray();

        Assert.IsEmpty(
            boxedMethods,
            $"Generated window compute methods should not emit IL box opcodes: {string.Join(", ", boxedMethods)}");
    }

    [TestMethod]
    public void GeneratedWindowComputeMethods_WhenIntegerKeyWindowQueryCompiled_ShouldNotEmitBoxOpcode()
    {
        var query = @"
            select Name,
                   RowNumber() over (partition by Id order by Id) as RowNo,
                   Sum(Population) over (partition by Id order by Id) as RunningSum
            from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("NYC", "Alice", 2) { Id = 1 },
            new BasicEntity("NYC", "Bob", 3) { Id = 1 },
            new BasicEntity("LA", "Diana", 4) { Id = 2 });
        var vm = CreateAndRunVirtualMachine(query, sources);

        var boxedMethods = GetGeneratedComputeMethods(vm)
            .Where(ContainsBoxOpcode)
            .Select(static method => method.Name)
            .ToArray();

        Assert.IsEmpty(
            boxedMethods,
            $"Generated integer-key window compute methods should not emit IL box opcodes: {string.Join(", ", boxedMethods)}");
    }

    [TestMethod]
    public void GeneratedWindowComputeMethods_WhenNullableKeyWindowQueryCompiled_ShouldNotEmitBoxOpcode()
    {
        var query = @"
            select Name,
                   RowNumber() over (partition by NullableValue order by NullableValue) as RowNo,
                   Sum(Population) over (partition by NullableValue order by NullableValue) as RunningSum
            from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("NYC", "Alice", 2) { NullableValue = 1 },
            new BasicEntity("NYC", "Bob", 3) { NullableValue = 1 },
            new BasicEntity("LA", "Diana", 4) { NullableValue = null });
        var vm = CreateAndRunVirtualMachine(query, sources);

        var boxedMethods = GetGeneratedComputeMethods(vm)
            .Where(ContainsBoxOpcode)
            .Select(static method => method.Name)
            .ToArray();

        Assert.IsEmpty(
            boxedMethods,
            $"Generated nullable-key window compute methods should not emit IL box opcodes: {string.Join(", ", boxedMethods)}");
    }

    [TestMethod]
    public void GeneratedWindowComputeMethods_WhenMultiColumnMixedDirectionWindowQueryCompiled_ShouldNotEmitBoxOpcode()
    {
        var query = @"
            select Name,
                   RowNumber() over (partition by City order by City asc, Id desc) as RowNo,
                   Sum(Population) over (partition by City order by City asc, Id desc) as RunningSum
            from #A.Entities()";
        var sources = CreateSingleSource(
            new BasicEntity("NYC", "Alice", 2) { Id = 5 },
            new BasicEntity("NYC", "Bob", 3) { Id = 3 },
            new BasicEntity("LA", "Diana", 4) { Id = 9 });
        var vm = CreateAndRunVirtualMachine(query, sources);

        var boxedMethods = GetGeneratedComputeMethods(vm)
            .Where(ContainsBoxOpcode)
            .Select(static method => method.Name)
            .ToArray();

        Assert.IsEmpty(
            boxedMethods,
            $"Generated multi-column mixed-direction window compute methods should not emit IL box opcodes: {string.Join(", ", boxedMethods)}");
    }

    private static IEnumerable<MethodInfo> GetGeneratedComputeMethods(CompiledQuery query)
    {
        var runnableField = typeof(CompiledQuery).GetField("_runnable", BindingFlags.Instance | BindingFlags.NonPublic) ??
                            throw new InvalidOperationException("Compiled query runnable field was not found.");
        var runnable = runnableField.GetValue(query) ??
                       throw new InvalidOperationException("Compiled query runnable was not initialized.");

        return runnable
            .GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(static method => method.Name.StartsWith("ComputeTable_", StringComparison.Ordinal));
    }

    private static bool ContainsBoxOpcode(MethodInfo method)
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
