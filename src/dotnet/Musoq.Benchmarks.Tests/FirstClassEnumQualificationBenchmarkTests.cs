using System.Reflection;
using System.Reflection.Emit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class FirstClassEnumQualificationBenchmarkTests
{
    private static readonly OpCode[] OneByteOpCodes = new OpCode[0x100];
    private static readonly OpCode[] TwoByteOpCodes = new OpCode[0x100];

    static FirstClassEnumQualificationBenchmarkTests()
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

    public static IEnumerable<object[]> Scenarios => Enum
        .GetValues<FirstClassEnumScenario>()
        .Select(static scenario => new object[] { scenario });

    [TestMethod]
    [DynamicData(nameof(Scenarios))]
    public void PairedQueries_ShouldReturnCarrierEquivalentResults(
        FirstClassEnumScenario scenario)
    {
        using var pair = FirstClassEnumBenchmarkSupport.CompilePair(scenario, 256);

        Assert.AreEqual(pair.ExecuteCarrier(), pair.ExecuteEnum());
    }

    [TestMethod]
    public void EnumProjection_ShouldExposeOnlyPrimitiveValuesAndPortableMetadata()
    {
        using var pair = FirstClassEnumBenchmarkSupport.CompilePair(
            FirstClassEnumScenario.Projection,
            64);
        using var table = pair.EnumQuery.Run();

        Assert.AreEqual(typeof(short?), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(uint?), table.Columns.ElementAt(1).ColumnType);
        Assert.IsNotNull(table.Columns.ElementAt(0).EnumType);
        Assert.IsNotNull(table.Columns.ElementAt(1).EnumType);
        Assert.IsFalse(table.SelectMany(static row => row.Values).Any(static value => value is Enum));
    }

    [TestMethod]
    [DynamicData(nameof(Scenarios))]
    public void GeneratedEnumCode_ShouldKeepDescriptorConstructionAndRuntimeApisOutOfLoops(
        FirstClassEnumScenario scenario)
    {
        var generated = FirstClassEnumBenchmarkSupport.InspectEnum(scenario).GeneratedCSharpCode;
        string[] forbiddenRuntimeApis =
        [
            "Enum.Parse",
            "Enum.ToObject",
            "Convert.ChangeType",
            "System.Reflection",
            "DynamicInvoke"
        ];

        var presentRuntimeApis = forbiddenRuntimeApis
            .Where(forbidden => generated.Contains(forbidden, StringComparison.Ordinal))
            .ToArray();

        Assert.IsEmpty(
            presentRuntimeApis,
            $"Generated enum code contains forbidden runtime APIs: {string.Join(", ", presentRuntimeApis)}");

        var root = CSharpSyntaxTree.ParseText(generated).GetRoot();
        var descriptorCreationsInLoops = root.DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Where(static creation => creation.Type.ToString().Contains(
                "EnumTypeDescriptor",
                StringComparison.Ordinal))
            .Where(static creation => creation.Ancestors().Any(IsLoop))
            .Select(static creation => creation.ToString())
            .ToArray();

        Assert.IsEmpty(
            descriptorCreationsInLoops,
            $"Enum descriptors must be compiled-query metadata, not per-row work: " +
            string.Join(Environment.NewLine, descriptorCreationsInLoops));
    }

    [TestMethod]
    public void GeneratedEnumShapeLoop_ShouldContainNoBoxInstruction()
    {
        using var query = FirstClassEnumBenchmarkSupport.CompileEnumShapeProbe(
            FirstClassEnumScenario.Helpers);
        var runtimeType = GetGeneratedRuntimeType(query);
        var shapeMethods = runtimeType
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static method => method.Name.Contains("ComputeShapeRows", StringComparison.Ordinal))
            .Concat(runtimeType
                .GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Where(static type => type.Name.Contains("ComputeShapeRows", StringComparison.Ordinal))
                .SelectMany(static type => type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)))
            .Where(static method => method.GetMethodBody() != null)
            .ToArray();

        Assert.IsNotEmpty(shapeMethods, "The enum shape-loop probe found no generated shape methods.");
        var boxed = shapeMethods
            .Where(static method => ReadOpCodes(method).Contains(OpCodes.Box))
            .Select(static method => $"{method.DeclaringType?.FullName}.{method.Name}")
            .ToArray();

        Assert.IsEmpty(
            boxed,
            $"Generated enum shape loops must not box: {string.Join(", ", boxed)}");
    }

    [TestMethod]
    public void GeneratedInPredicate_ShouldMatchCarrierRuntimeShape()
    {
        var carrier = FirstClassEnumBenchmarkSupport
            .InspectCarrier(FirstClassEnumScenario.In)
            .GeneratedCSharpCode;
        var logicalEnum = FirstClassEnumBenchmarkSupport
            .InspectEnum(FirstClassEnumScenario.In)
            .GeneratedCSharpCode;

        static string[] Comparisons(string source) => CSharpSyntaxTree
            .ParseText(source)
            .GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(static invocation => invocation.Expression.ToString().Contains(
                "SqlCompare",
                StringComparison.Ordinal))
            .Select(static invocation => invocation.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var carrierComparisons = Comparisons(carrier);
        var enumComparisons = Comparisons(logicalEnum);
        Assert.IsNotEmpty(carrierComparisons);
        CollectionAssert.AreEqual(carrierComparisons, enumComparisons);
    }

    private static bool IsLoop(SyntaxNode node)
    {
        return node is ForStatementSyntax or ForEachStatementSyntax or
            WhileStatementSyntax or DoStatementSyntax;
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
            if (current.GetProperty(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is { } property)
            {
                return property;
            }
        }

        return null;
    }

    private static IReadOnlyList<OpCode> ReadOpCodes(MethodInfo method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray() ??
                 throw new AssertFailedException($"Generated method '{method}' has no IL body.");
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
            OperandType.InlineSwitch => 4 + BitConverter.ToInt32(il, operandIndex) * 4,
            _ => throw new ArgumentOutOfRangeException(nameof(operandType), operandType, null)
        };
    }
}
