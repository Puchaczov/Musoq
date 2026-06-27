using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class MathExtendedTests
{
    [TestMethod]
    [DynamicData(nameof(AdditionalDoubleCases))]
    public void AdditionalDouble_Cases_ReturnExpected(string name, Func<LibraryBase, double?> execute, double expected)
    {
        var result = execute(Library);

        Assert.IsNotNull(result, name);
        Assert.AreEqual(expected, result.Value, 0.0001, name);
    }

    [TestMethod]
    [DynamicData(nameof(AdditionalDecimalCases))]
    public void AdditionalDecimal_Cases_ReturnExpected(string name, Func<LibraryBase, decimal?> execute, decimal expected)
    {
        var result = execute(Library);

        Assert.IsNotNull(result, name);
        Assert.AreEqual(expected, result.Value, name);
    }

    public static IEnumerable<object?[]> AdditionalDoubleCases()
    {
        yield return DoubleCase("Log_Base2", _ => LibraryBase.Log(2m, 8m), 3.0);
        yield return DoubleCase("Exp_Zero", _ => LibraryBase.Exp(0.0), 1.0);
        yield return DoubleCase("Ln_One", _ => LibraryBase.Ln(1.0), 0.0);
        yield return DoubleCase("Log10_One", _ => LibraryBase.Log10(1.0), 0.0);
        yield return DoubleCase("Log2_One", _ => LibraryBase.Log2(1.0), 0.0);
        yield return DoubleCase("Sin_PiOver2", _ => LibraryBase.Sin(Math.PI / 2), 1.0);
        yield return DoubleCase("Cos_Pi", _ => LibraryBase.Cos(Math.PI), -1.0);
        yield return DoubleCase("Tan_PiOver4", _ => LibraryBase.Tan(Math.PI / 4), 1.0);
        yield return DoubleCase("Pow_ZeroExponent", library => library.Pow(5.0, 0.0), 1.0);
        yield return DoubleCase("Pow_FractionalExponent", library => library.Pow(4.0, 0.5), 2.0);
        yield return DoubleCase("Sqrt_Zero", library => library.Sqrt(0m), 0.0);
    }

    public static IEnumerable<object?[]> AdditionalDecimalCases()
    {
        yield return DecimalCase("Exp_ZeroDecimal", _ => LibraryBase.Exp(0m), 1m);
        yield return DecimalCase("Ln_OneDecimal", _ => LibraryBase.Ln(1m), 0m);
    }

    private static object?[] DoubleCase(string name, Func<LibraryBase, double?> execute, double expected)
    {
        return [name, execute, expected];
    }

    private static object?[] DecimalCase(string name, Func<LibraryBase, decimal?> execute, decimal expected)
    {
        return [name, execute, expected];
    }
}