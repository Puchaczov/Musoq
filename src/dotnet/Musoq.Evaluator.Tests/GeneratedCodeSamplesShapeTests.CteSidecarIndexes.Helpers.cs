using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static string ExtractGeneratedClass(string generatedCode, string className)
    {
        var marker = $"private sealed class {className}";
        var start = generatedCode.IndexOf(marker, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, generatedCode);

        var next = generatedCode.IndexOf("private sealed class ", start + marker.Length, StringComparison.Ordinal);
        return next < 0 ? generatedCode[start..] : generatedCode[start..next];
    }

    private static CompilationOptions CreateParallelCteSidecarOptions()
    {
        return new CompilationOptions(
            parallelizationMode: ParallelizationMode.Full,
            useHashJoin: true,
            useSortMergeJoin: false,
            useCteParallelization: true,
            useCteSidecarIndexes: true);
    }

    private static void AssertGeneratedClassIsLeanInternalCarrier(string generatedCode, string className)
    {
        var generatedClass = ExtractGeneratedClass(generatedCode, className);

        Assert.IsFalse(generatedClass.Contains(": Row", StringComparison.Ordinal), generatedClass);
        Assert.IsFalse(generatedClass.Contains("override object this[", StringComparison.Ordinal), generatedClass);
        Assert.IsFalse(generatedClass.Contains("AssignValue", StringComparison.Ordinal), generatedClass);
        Assert.IsFalse(generatedClass.Contains("Contexts =>", StringComparison.Ordinal), generatedClass);
    }
}
