using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public class BitsOperationsTests : PluginsTestBase
{
    public static IEnumerable<object?[]> ShiftLeftCases()
    {
        yield return Case("ShiftLeft_Byte_ShouldReturnCorrectResult", library => library.ShiftLeft((byte?)4, 2), (byte)16);
        yield return Case("ShiftLeft_Byte_WhenNull_ShouldReturnNull", library => library.ShiftLeft((byte?)null, 2), null);
        yield return Case("ShiftLeft_Short_ShouldReturnCorrectResult", library => library.ShiftLeft((short?)4, 2), (short)16);
        yield return Case("ShiftLeft_Short_WhenNull_ShouldReturnNull", library => library.ShiftLeft((short?)null, 2), null);
        yield return Case("ShiftLeft_Int_ShouldReturnCorrectResult", library => library.ShiftLeft((int?)4, 2), 16);
        yield return Case("ShiftLeft_Int_WhenNull_ShouldReturnNull", library => library.ShiftLeft((int?)null, 2), null);
        yield return Case("ShiftLeft_Long_ShouldReturnCorrectResult", library => library.ShiftLeft(4L, 2), 16L);
        yield return Case("ShiftLeft_Long_WhenNull_ShouldReturnNull", library => library.ShiftLeft((long?)null, 2), null);
    }

    [TestMethod]
    [DynamicData(nameof(ShiftLeftCases))]
    public void ShiftLeft_Cases_ReturnExpected(string name, Func<LibraryBase, object?> execute, object? expected)
    {
        Assert.AreEqual(expected, execute(Library), name);
    }

    public static IEnumerable<object?[]> ShiftRightCases()
    {
        yield return Case("ShiftRight_Byte_ShouldReturnCorrectResult", library => library.ShiftRight((byte?)16, 2), (byte)4);
        yield return Case("ShiftRight_Byte_WhenNull_ShouldReturnNull", library => library.ShiftRight((byte?)null, 2), null);
        yield return Case("ShiftRight_Int_ShouldReturnCorrectResult", library => library.ShiftRight((int?)16, 2), 4);
        yield return Case("ShiftRight_Int_WhenNull_ShouldReturnNull", library => library.ShiftRight((int?)null, 2), null);
    }

    [TestMethod]
    [DynamicData(nameof(ShiftRightCases))]
    public void ShiftRight_Cases_ReturnExpected(string name, Func<LibraryBase, object?> execute, object? expected)
    {
        Assert.AreEqual(expected, execute(Library), name);
    }

    public static IEnumerable<object?[]> NotCases()
    {
        yield return Case("Not_Byte_ShouldReturnCorrectResult", library => library.Not((byte?)0b00001111), (byte)0b11110000);
        yield return Case("Not_Byte_WhenNull_ShouldReturnNull", library => library.Not((byte?)null), null);
        yield return Case("Not_Int_ShouldReturnCorrectResult", library => library.Not((int?)0b00001111), ~15);
        yield return Case("Not_Int_WhenNull_ShouldReturnNull", library => library.Not((int?)null), null);
    }

    [TestMethod]
    [DynamicData(nameof(NotCases))]
    public void Not_Cases_ReturnExpected(string name, Func<LibraryBase, object?> execute, object? expected)
    {
        Assert.AreEqual(expected, execute(Library), name);
    }

    public static IEnumerable<object?[]> AndCases()
    {
        yield return Case("And_Byte_ShouldReturnCorrectResult", library => library.And((byte?)0b11110000, (byte?)0b00001111), (byte)0);
        yield return Case("And_Byte_WhenOneNull_ShouldReturnNull", library => library.And((byte?)240, (byte?)null), null);
        yield return Case("And_Int_ShouldReturnCorrectResult", library => library.And((int?)0b11110000, (int?)0b11111111), 240);
        yield return Case("And_Int_WhenBothNull_ShouldReturnNull", library => library.And((int?)null, (int?)null), null);
    }

    [TestMethod]
    [DynamicData(nameof(AndCases))]
    public void And_Cases_ReturnExpected(string name, Func<LibraryBase, object?> execute, object? expected)
    {
        Assert.AreEqual(expected, execute(Library), name);
    }

    public static IEnumerable<object?[]> OrCases()
    {
        yield return Case("Or_Byte_ShouldReturnCorrectResult", library => library.Or((byte?)0b11110000, (byte?)0b00001111), (byte)255);
        yield return Case("Or_Byte_WhenOneNull_ShouldReturnNull", library => library.Or((byte?)null, (byte?)15), null);
        yield return Case("Or_Int_ShouldReturnCorrectResult", library => library.Or((int?)0b11110000, (int?)0b00001111), 255);
    }

    [TestMethod]
    [DynamicData(nameof(OrCases))]
    public void Or_Cases_ReturnExpected(string name, Func<LibraryBase, object?> execute, object? expected)
    {
        Assert.AreEqual(expected, execute(Library), name);
    }

    public static IEnumerable<object?[]> XorCases()
    {
        yield return Case("Xor_Byte_ShouldReturnCorrectResult", library => library.Xor((byte?)0b11110000, (byte?)0b00001111), (byte)255);
        yield return Case("Xor_Byte_WhenSameValues_ShouldReturnZero", library => library.Xor((byte?)0b11110000, (byte?)0b11110000), (byte)0);
        yield return Case("Xor_Byte_WhenOneNull_ShouldReturnNull", library => library.Xor((byte?)240, (byte?)null), null);
        yield return Case("Xor_Int_ShouldReturnCorrectResult", library => library.Xor((int?)0b11110000, (int?)0b00001111), 255);
    }

    [TestMethod]
    [DynamicData(nameof(XorCases))]
    public void Xor_Cases_ReturnExpected(string name, Func<LibraryBase, object?> execute, object? expected)
    {
        Assert.AreEqual(expected, execute(Library), name);
    }

    private static object?[] Case(string name, Func<LibraryBase, object?> execute, object? expected)
    {
        return [name, execute, expected];
    }
}
