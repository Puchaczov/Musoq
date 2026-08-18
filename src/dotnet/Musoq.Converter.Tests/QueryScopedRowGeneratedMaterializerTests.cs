using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class QueryScopedRowGeneratedMaterializerTests
{
    private static readonly OpCode[] OneByteOpCodes = new OpCode[0x100];
    private static readonly OpCode[] TwoByteOpCodes = new OpCode[0x100];
    private readonly TestsLoggerResolver _loggerResolver = new();

    static QueryScopedRowGeneratedMaterializerTests()
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
    public void GeneratedMaterializer_WhenInspectedAsSyntax_ShouldUseConstrainedGenericReaderCallsOnly()
    {
        var inspection = InstanceCreator.CompileForInspection(
            "select p.Name, p.Value from #queryrows.items() p",
            "query-row-materializer-syntax",
            new QueryScopedRowsSchemaProvider(),
            _loggerResolver);
        Assert.IsFalse(inspection.Diagnostics.Any(static diagnostic => diagnostic.IsError));

        var root = CSharpSyntaxTree.ParseText(inspection.GeneratedCSharpCode).GetRoot();
        var materializer = root.DescendantNodes()
            .OfType<StructDeclarationSyntax>()
            .Single(static declaration => declaration.Identifier.ValueText.StartsWith(
                "QueryRowMaterializer_",
                StringComparison.Ordinal));
        var materialize = materializer.Members
            .OfType<MethodDeclarationSyntax>()
            .Single(static method => method.Identifier.ValueText == "Materialize");
        var reader = materialize.ParameterList.Parameters.Single();
        var readerCalls = materialize.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(static invocation => invocation.Expression is MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "reader" },
                Name: GenericNameSyntax { Identifier.ValueText: "Read" }
            })
            .Select(static invocation => invocation.NormalizeWhitespace().ToFullString())
            .ToArray();

        StringAssert.Contains(materializer.BaseList!.ToString(), "IQueryRowMaterializer<QueryRow_");
        Assert.IsTrue(reader.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.ScopedKeyword)));
        Assert.IsTrue(reader.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.RefKeyword)));
        StringAssert.Contains(materialize.ConstraintClauses.Single().ToString(), "IQuerySourceFieldReader");
        StringAssert.Contains(materialize.ConstraintClauses.Single().ToString(), "allows ref struct");
        CollectionAssert.AreEqual(
            new[] { "reader.Read<string>(0)", "reader.Read<int>(1)" },
            readerCalls);

        var materializerCode = materializer.NormalizeWhitespace().ToFullString();
        Assert.IsFalse(materializerCode.Contains("object[]", StringComparison.Ordinal));
        Assert.IsFalse(materializerCode.Contains("System.Reflection", StringComparison.Ordinal));
        Assert.IsFalse(materializerCode.Contains("Delegate", StringComparison.Ordinal));
        Assert.IsFalse(materializerCode.Contains("DynamicInvoke", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GeneratedMaterializer_WhenInspectedAsIl_ShouldConstrainEveryInterfaceCallWithoutBoxing()
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            "select p.Name, p.Value from #queryrows.items() p where p.Value > -987654321",
            "query-row-materializer-il",
            new QueryScopedRowsSchemaProvider(),
            _loggerResolver);
        Assert.IsTrue(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));

        try
        {
            var runtimeType = GetGeneratedRuntimeType(result.CompiledQuery!);
            var materializer = runtimeType.Assembly.GetTypes().Single(static type =>
                type.Name.StartsWith("QueryRowMaterializer_", StringComparison.Ordinal));
            var method = materializer.GetMethod(
                "Materialize",
                BindingFlags.Public | BindingFlags.Static) ??
                         throw new AssertFailedException("The generated materializer method was not found.");
            var opCodes = ReadOpCodes(method);

            Assert.IsFalse(opCodes.Contains(OpCodes.Box));
            Assert.IsTrue(opCodes.Contains(OpCodes.Constrained));
            for (var index = 0; index < opCodes.Count; index++)
            {
                if (opCodes[index] != OpCodes.Callvirt)
                    continue;

                Assert.IsTrue(
                    index > 0 && opCodes[index - 1] == OpCodes.Constrained,
                    "Every interface call in the generated materializer must use constrained generic dispatch.");
            }
        }
        finally
        {
            result.CompiledQuery?.Dispose();
        }
    }

    private static Type GetGeneratedRuntimeType(CompiledQuery query)
    {
        var runnableField = typeof(CompiledQuery).GetField(
            "_runnable",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
                            throw new AssertFailedException("The compiled query runnable was not found.");
        var current = runnableField.GetValue(query) ??
                      throw new AssertFailedException("The compiled query runnable was not initialized.");
        while (FindProperty(current.GetType(), "Inner")?.GetValue(current) is { } inner)
            current = inner;

        return current.GetType();
    }

    private static PropertyInfo? FindProperty(Type type, string name)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is { } property)
                return property;
        }

        return null;
    }

    private static IReadOnlyList<OpCode> ReadOpCodes(MethodInfo method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray() ??
                 throw new AssertFailedException("The generated materializer has no IL body.");
        var result = new List<OpCode>();
        for (var index = 0; index < il.Length;)
        {
            var opCode = ReadOpCode(il, ref index);
            result.Add(opCode);
            index += GetOperandSize(opCode.OperandType, il, index);
        }

        return result;
    }

    private static OpCode ReadOpCode(byte[] il, ref int index)
    {
        var value = il[index++];
        return value != 0xfe
            ? OneByteOpCodes[value]
            : TwoByteOpCodes[il[index++]];
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
