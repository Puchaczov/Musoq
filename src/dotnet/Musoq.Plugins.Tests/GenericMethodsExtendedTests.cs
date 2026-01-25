using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for generic utility methods to improve branch coverage.
///     Tests NullIf, IfNull, DefaultIfNull, IsNull, IsNotNull, NthFromEndOrDefault, Coalesce, etc.
/// </summary>
[TestClass]
public class GenericMethodsExtendedTests : LibraryBaseBaseTests
{
    #region NullIf Tests

    [TestMethod]
    public void NullIf_BothNull_ReturnsDefault()
    {
        Assert.IsNull(Library.NullIf<string>(null, null));
    }

    [TestMethod]
    public void NullIf_ValueNullCompareNotNull_ReturnsNull()
    {
        Assert.IsNull(Library.NullIf<string>(null, "test"));
    }

    [TestMethod]
    public void NullIf_ValueNotNullCompareNull_ReturnsValue()
    {
        Assert.AreEqual("test", Library.NullIf("test", null));
    }

    [TestMethod]
    public void NullIf_Equal_ReturnsDefault()
    {
        Assert.IsNull(Library.NullIf("test", "test"));
    }

    [TestMethod]
    public void NullIf_NotEqual_ReturnsValue()
    {
        Assert.AreEqual("hello", Library.NullIf("hello", "world"));
    }

    [TestMethod]
    public void NullIf_IntegerEqual_ReturnsDefault()
    {
        Assert.AreEqual(default, Library.NullIf<int?>(42, 42));
    }

    [TestMethod]
    public void NullIf_IntegerNotEqual_ReturnsValue()
    {
        Assert.AreEqual(42, Library.NullIf<int?>(42, 100));
    }

    #endregion

    #region IfNull Tests

    [TestMethod]
    public void IfNull_ValueNull_ReturnsDefault()
    {
        Assert.AreEqual("default", Library.IfNull<string>(null, "default"));
    }

    [TestMethod]
    public void IfNull_ValueNotNull_ReturnsValue()
    {
        Assert.AreEqual("hello", Library.IfNull("hello", "default"));
    }

    [TestMethod]
    public void IfNull_BothNull_ReturnsNull()
    {
        Assert.IsNull(Library.IfNull<string>(null, null));
    }

    [TestMethod]
    public void IfNull_IntegerNull_ReturnsDefault()
    {
        Assert.AreEqual(42, Library.IfNull<int?>(null, 42));
    }

    [TestMethod]
    public void IfNull_IntegerNotNull_ReturnsValue()
    {
        Assert.AreEqual(100, Library.IfNull<int?>(100, 42));
    }

    #endregion

    #region DefaultIfNull Tests

    [TestMethod]
    public void DefaultIfNull_Null_ReturnsDefault()
    {
        Assert.IsNull(Library.DefaultIfNull<string>(null));
    }

    [TestMethod]
    public void DefaultIfNull_NotNull_ReturnsValue()
    {
        Assert.AreEqual("hello", Library.DefaultIfNull("hello"));
    }

    [TestMethod]
    public void DefaultIfNull_IntegerNull_ReturnsDefaultInt()
    {
        Assert.IsNull(Library.DefaultIfNull<int?>(null));
    }

    [TestMethod]
    public void DefaultIfNull_IntegerNotNull_ReturnsValue()
    {
        Assert.AreEqual(42, Library.DefaultIfNull<int?>(42));
    }

    #endregion

    #region IsNull Tests

    [TestMethod]
    public void IsNull_Null_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsNull<string>(null));
    }

    [TestMethod]
    public void IsNull_NotNull_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNull("hello"));
    }

    [TestMethod]
    public void IsNull_IntegerNull_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsNull<int?>(null));
    }

    [TestMethod]
    public void IsNull_IntegerNotNull_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNull<int?>(42));
    }

    #endregion

    #region IsNotNull Tests

    [TestMethod]
    public void IsNotNull_Null_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNotNull<string>(null));
    }

    [TestMethod]
    public void IsNotNull_NotNull_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsNotNull("hello"));
    }

    [TestMethod]
    public void IsNotNull_IntegerNull_ReturnsFalse()
    {
        Assert.IsFalse(Library.IsNotNull<int?>(null));
    }

    [TestMethod]
    public void IsNotNull_IntegerNotNull_ReturnsTrue()
    {
        Assert.IsTrue(Library.IsNotNull<int?>(42));
    }

    #endregion

    #region NthFromEndOrDefault Tests

    [TestMethod]
    public void NthFromEndOrDefault_Null_ReturnsDefault()
    {
        Assert.IsNull(Library.NthFromEndOrDefault<string>(null, 0));
    }

    [TestMethod]
    public void NthFromEndOrDefault_List_Index0_ReturnsLast()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        Assert.AreEqual(5, Library.NthFromEndOrDefault(list, 0));
    }

    [TestMethod]
    public void NthFromEndOrDefault_List_Index1_ReturnsSecondFromEnd()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        Assert.AreEqual(4, Library.NthFromEndOrDefault(list, 1));
    }

    [TestMethod]
    public void NthFromEndOrDefault_List_IndexOutOfRange_ReturnsDefault()
    {
        var list = new List<int> { 1, 2, 3 };
        Assert.AreEqual(default, Library.NthFromEndOrDefault(list, 10));
    }

    [TestMethod]
    public void NthFromEndOrDefault_Array_Index0_ReturnsLast()
    {
        var array = new[] { 1, 2, 3, 4, 5 };
        Assert.AreEqual(5, Library.NthFromEndOrDefault(array, 0));
    }

    [TestMethod]
    public void NthFromEndOrDefault_Array_Index1_ReturnsSecondFromEnd()
    {
        var array = new[] { 1, 2, 3, 4, 5 };
        Assert.AreEqual(4, Library.NthFromEndOrDefault(array, 1));
    }

    [TestMethod]
    public void NthFromEndOrDefault_Array_IndexOutOfRange_ReturnsDefault()
    {
        var array = new[] { 1, 2, 3 };
        Assert.AreEqual(default, Library.NthFromEndOrDefault(array, 10));
    }

    [TestMethod]
    public void NthFromEndOrDefault_Enumerable_Index0_ReturnsLast()
    {
        var enumerable = Enumerable.Range(1, 5);
        Assert.AreEqual(5, Library.NthFromEndOrDefault(enumerable, 0));
    }

    [TestMethod]
    public void NthFromEndOrDefault_Enumerable_Index1_ReturnsSecondFromEnd()
    {
        var enumerable = Enumerable.Range(1, 5);
        Assert.AreEqual(4, Library.NthFromEndOrDefault(enumerable, 1));
    }

    [TestMethod]
    public void NthFromEndOrDefault_Enumerable_IndexOutOfRange_ReturnsDefault()
    {
        var enumerable = Enumerable.Range(1, 3);
        Assert.AreEqual(default, Library.NthFromEndOrDefault(enumerable, 10));
    }

    [TestMethod]
    public void NthFromEndOrDefault_EmptyList_ReturnsDefault()
    {
        var list = new List<int>();
        Assert.AreEqual(default, Library.NthFromEndOrDefault(list, 0));
    }

    [TestMethod]
    public void NthFromEndOrDefault_SingleElementList_Index0_ReturnsElement()
    {
        var list = new List<int> { 42 };
        Assert.AreEqual(42, Library.NthFromEndOrDefault(list, 0));
    }

    #endregion

    #region NthOrDefault Tests

    [TestMethod]
    public void NthOrDefault_Null_ReturnsDefault()
    {
        Assert.IsNull(Library.NthOrDefault<string>(null, 0));
    }

    [TestMethod]
    public void NthOrDefault_ValidIndex_ReturnsElement()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        Assert.AreEqual(3, Library.NthOrDefault(list, 2));
    }

    [TestMethod]
    public void NthOrDefault_IndexOutOfRange_ReturnsDefault()
    {
        var list = new List<int> { 1, 2, 3 };
        Assert.AreEqual(default, Library.NthOrDefault(list, 10));
    }

    [TestMethod]
    public void NthOrDefault_NegativeIndex_ReturnsDefault()
    {
        var list = new List<int> { 1, 2, 3 };
        Assert.AreEqual(default, Library.NthOrDefault(list, -1));
    }

    #endregion

    #region Coalesce Generic Tests

    [TestMethod]
    public void Coalesce_AllNull_ReturnsDefault()
    {
        Assert.IsNull(Library.Coalesce<string>(null, null, null));
    }

    [TestMethod]
    public void Coalesce_FirstNotNull_ReturnsFirst()
    {
        Assert.AreEqual("first", Library.Coalesce("first", "second", "third"));
    }

    [TestMethod]
    public void Coalesce_FirstNull_ReturnsSecond()
    {
        Assert.AreEqual("second", Library.Coalesce<string>(null, "second", "third"));
    }

    [TestMethod]
    public void Coalesce_FirstTwoNull_ReturnsThird()
    {
        Assert.AreEqual("third", Library.Coalesce<string>(null, null, "third"));
    }

    [TestMethod]
    public void Coalesce_SingleElement_ReturnsElement()
    {
        Assert.AreEqual("only", Library.Coalesce("only"));
    }

    [TestMethod]
    public void Coalesce_SingleNullElement_ReturnsDefault()
    {
        Assert.IsNull(Library.Coalesce<string>(new string?[] { null }));
    }

    [TestMethod]
    public void Coalesce_IntegerFirstNotNull_ReturnsFirst()
    {
        Assert.AreEqual(42, Library.Coalesce<int?>(42, 100, 200));
    }

    [TestMethod]
    public void Coalesce_IntegerFirstNull_ReturnsSecond()
    {
        Assert.AreEqual(100, Library.Coalesce<int?>(null, 100, 200));
    }

    #endregion

    #region Coalesce Type-specific Tests

    [TestMethod]
    public void Coalesce_Byte_FirstNotNull_ReturnsFirst()
    {
        Assert.AreEqual((byte)1, Library.Coalesce((byte?)1, (byte?)2));
    }

    [TestMethod]
    public void Coalesce_Byte_FirstNull_ReturnsSecond()
    {
        Assert.AreEqual((byte)2, Library.Coalesce(null, (byte?)2));
    }

    [TestMethod]
    public void Coalesce_SByte_FirstNotNull_ReturnsFirst()
    {
        Assert.AreEqual((sbyte)1, Library.Coalesce((sbyte?)1, (sbyte?)2));
    }

    [TestMethod]
    public void Coalesce_SByte_FirstNull_ReturnsSecond()
    {
        Assert.AreEqual((sbyte)2, Library.Coalesce(null, (sbyte?)2));
    }

    [TestMethod]
    public void Coalesce_Short_FirstNotNull_ReturnsFirst()
    {
        Assert.AreEqual((short)1, Library.Coalesce((short?)1, (short?)2));
    }

    [TestMethod]
    public void Coalesce_Short_FirstNull_ReturnsSecond()
    {
        Assert.AreEqual((short)2, Library.Coalesce(null, (short?)2));
    }

    [TestMethod]
    public void Coalesce_UShort_FirstNotNull_ReturnsFirst()
    {
        Assert.AreEqual((ushort)1, Library.Coalesce((ushort?)1, (ushort?)2));
    }

    [TestMethod]
    public void Coalesce_UShort_FirstNull_ReturnsSecond()
    {
        Assert.AreEqual((ushort)2, Library.Coalesce(null, (ushort?)2));
    }

    [TestMethod]
    public void Coalesce_Int_FirstNotNull_ReturnsFirst()
    {
        Assert.AreEqual(1, Library.Coalesce(1, (int?)2));
    }

    [TestMethod]
    public void Coalesce_Int_FirstNull_ReturnsSecond()
    {
        Assert.AreEqual(2, Library.Coalesce(null, (int?)2));
    }

    [TestMethod]
    public void Coalesce_UInt_FirstNotNull_ReturnsFirst()
    {
        Assert.AreEqual(1m, Library.Coalesce((uint?)1, (uint?)2));
    }

    [TestMethod]
    public void Coalesce_UInt_FirstNull_ReturnsSecond()
    {
        Assert.AreEqual(2m, Library.Coalesce(null, (uint?)2));
    }

    [TestMethod]
    public void Coalesce_Long_FirstNotNull_ReturnsFirst()
    {
        Assert.AreEqual(1m, Library.Coalesce(1, (long?)2));
    }

    [TestMethod]
    public void Coalesce_Long_FirstNull_ReturnsSecond()
    {
        Assert.AreEqual(2m, Library.Coalesce(null, (long?)2));
    }

    [TestMethod]
    public void Coalesce_ULong_FirstNotNull_ReturnsFirst()
    {
        Assert.AreEqual(1m, Library.Coalesce((ulong?)1, (ulong?)2));
    }

    [TestMethod]
    public void Coalesce_ULong_FirstNull_ReturnsSecond()
    {
        Assert.AreEqual(2m, Library.Coalesce(null, (ulong?)2));
    }

    [TestMethod]
    public void Coalesce_Decimal_FirstNotNull_ReturnsFirst()
    {
        Assert.AreEqual(1m, Library.Coalesce(1m, (decimal?)2m));
    }

    [TestMethod]
    public void Coalesce_Decimal_FirstNull_ReturnsSecond()
    {
        Assert.AreEqual(2m, Library.Coalesce(null, 2m));
    }

    #endregion

    #region If Tests

    [TestMethod]
    public void If_True_ReturnsA()
    {
        Assert.AreEqual("yes", Library.If(true, "yes", "no"));
    }

    [TestMethod]
    public void If_False_ReturnsB()
    {
        Assert.AreEqual("no", Library.If(false, "yes", "no"));
    }

    [TestMethod]
    public void If_TrueInteger_ReturnsA()
    {
        Assert.AreEqual(1, Library.If(true, 1, 2));
    }

    [TestMethod]
    public void If_FalseInteger_ReturnsB()
    {
        Assert.AreEqual(2, Library.If(false, 1, 2));
    }

    #endregion

    #region Choose Tests

    [TestMethod]
    public void Choose_ValidIndex_ReturnsValue()
    {
        Assert.AreEqual("b", Library.Choose(1, "a", "b", "c"));
    }

    [TestMethod]
    public void Choose_FirstIndex_ReturnsFirst()
    {
        Assert.AreEqual("a", Library.Choose(0, "a", "b", "c"));
    }

    [TestMethod]
    public void Choose_LastIndex_ReturnsLast()
    {
        Assert.AreEqual("c", Library.Choose(2, "a", "b", "c"));
    }

    [TestMethod]
    public void Choose_IndexOutOfRange_ReturnsDefault()
    {
        Assert.IsNull(Library.Choose(5, "a", "b", "c"));
    }

    [TestMethod]
    public void Choose_NegativeIndex_ThrowsException()
    {
        var exceptionThrown = false;
        try
        {
            Library.Choose(-1, "a", "b", "c");
        }
        catch (IndexOutOfRangeException)
        {
            exceptionThrown = true;
        }

        Assert.IsTrue(exceptionThrown, "IndexOutOfRangeException should be thrown");
    }

    [TestMethod]
    public void Choose_IntegerValidIndex_ReturnsValue()
    {
        Assert.AreEqual(20, Library.Choose(1, 10, 20, 30));
    }

    #endregion

    #region Match Tests

    [TestMethod]
    public void Match_NullRegex_ReturnsNull()
    {
        Assert.IsNull(Library.Match(null, "test"));
    }

    [TestMethod]
    public void Match_NullContent_ReturnsNull()
    {
        Assert.IsNull(Library.Match(@"\d+", null));
    }

    [TestMethod]
    public void Match_Matches_ReturnsTrue()
    {
        Assert.IsTrue(Library.Match(@"\d+", "test123"));
    }

    [TestMethod]
    public void Match_NoMatch_ReturnsFalse()
    {
        Assert.IsFalse(Library.Match(@"\d+", "test"));
    }

    #endregion

    #region FirstOrDefault Tests

    [TestMethod]
    public void FirstOrDefault_Null_ReturnsDefault()
    {
        Assert.IsNull(Library.FirstOrDefault<string>(null));
    }

    [TestMethod]
    public void FirstOrDefault_EmptyList_ReturnsDefault()
    {
        Assert.IsNull(Library.FirstOrDefault(new List<string>()));
    }

    [TestMethod]
    public void FirstOrDefault_HasElements_ReturnsFirst()
    {
        Assert.AreEqual("a", Library.FirstOrDefault(new List<string> { "a", "b", "c" }));
    }

    #endregion

    #region LastOrDefault Tests

    [TestMethod]
    public void LastOrDefault_Null_ReturnsDefault()
    {
        Assert.IsNull(Library.LastOrDefault<string>(null));
    }

    [TestMethod]
    public void LastOrDefault_EmptyList_ReturnsDefault()
    {
        Assert.IsNull(Library.LastOrDefault(new List<string>()));
    }

    [TestMethod]
    public void LastOrDefault_HasElements_ReturnsLast()
    {
        Assert.AreEqual("c", Library.LastOrDefault(new List<string> { "a", "b", "c" }));
    }

    #endregion

    #region Skip Tests

    [TestMethod]
    public void Skip_Null_ReturnsNull()
    {
        Assert.IsNull(Library.Skip<int>(null, 2));
    }

    [TestMethod]
    public void Skip_ValidCount_SkipsElements()
    {
        var result = Library.Skip(new[] { 1, 2, 3, 4, 5 }, 2);
        Assert.IsNotNull(result);
        var array = result.ToArray();
        Assert.HasCount(3, array);
        Assert.AreEqual(3, array[0]);
    }

    #endregion

    #region Take Tests

    [TestMethod]
    public void Take_Null_ReturnsNull()
    {
        Assert.IsNull(Library.Take<int>(null, 2));
    }

    [TestMethod]
    public void Take_ValidCount_TakesElements()
    {
        var result = Library.Take(new[] { 1, 2, 3, 4, 5 }, 2);
        Assert.IsNotNull(result);
        var array = result.ToArray();
        Assert.HasCount(2, array);
        Assert.AreEqual(1, array[0]);
        Assert.AreEqual(2, array[1]);
    }

    #endregion

    #region SkipAndTake Tests

    [TestMethod]
    public void SkipAndTake_Null_ReturnsNull()
    {
        Assert.IsNull(Library.SkipAndTake<int>(null, 1, 2));
    }

    [TestMethod]
    public void SkipAndTake_ValidCounts_SkipsAndTakes()
    {
        var result = Library.SkipAndTake(new[] { 1, 2, 3, 4, 5 }, 1, 2);
        Assert.IsNotNull(result);
        var array = result.ToArray();
        Assert.HasCount(2, array);
        Assert.AreEqual(2, array[0]);
        Assert.AreEqual(3, array[1]);
    }

    #endregion

    #region Distinct Tests

    [TestMethod]
    public void Distinct_Null_ReturnsNull()
    {
        Assert.IsNull(Library.Distinct<int>(null));
    }

    [TestMethod]
    public void Distinct_WithDuplicates_ReturnsDistinct()
    {
        var result = Library.Distinct(new[] { 1, 2, 2, 3, 3, 3 });
        Assert.IsNotNull(result);
        var array = result.ToArray();
        Assert.HasCount(3, array);
    }

    #endregion

    #region EnumerableToArray Tests

    [TestMethod]
    public void EnumerableToArray_Null_ReturnsNull()
    {
        Assert.IsNull(Library.EnumerableToArray<int>(null));
    }

    [TestMethod]
    public void EnumerableToArray_Enumerable_ReturnsArray()
    {
        var result = Library.EnumerableToArray(Enumerable.Range(1, 3));
        Assert.IsNotNull(result);
        Assert.HasCount(3, result);
    }

    #endregion

    #region MergeArrays Tests

    [TestMethod]
    public void MergeArrays_Null_ReturnsNull()
    {
        Assert.IsNull(Library.MergeArrays<int>(null));
    }

    [TestMethod]
    public void MergeArrays_MultipleArrays_MergesAll()
    {
        var result = Library.MergeArrays(new[] { 1, 2 }, new[] { 3, 4 }, new[] { 5 });
        Assert.IsNotNull(result);
        Assert.HasCount(5, result);
        Assert.AreEqual(1, result[0]);
        Assert.AreEqual(5, result[4]);
    }

    #endregion

    #region Length Tests

    [TestMethod]
    public void Length_NullEnumerable_ReturnsNull()
    {
        Assert.IsNull(Library.Length<int>(null));
    }

    [TestMethod]
    public void Length_Enumerable_ReturnsCount()
    {
        Assert.AreEqual(5, Library.Length(Enumerable.Range(1, 5)));
    }

    [TestMethod]
    public void Length_NullArray_ReturnsNull()
    {
        int[]? nullArray = null;
        Assert.IsNull(Library.Length(nullArray));
    }

    [TestMethod]
    public void Length_Array_ReturnsLength()
    {
        Assert.AreEqual(3, Library.Length(new[] { 1, 2, 3 }));
    }

    #endregion

    #region GetElementAtOrDefault Tests

    [TestMethod]
    public void GetElementAtOrDefault_Null_ReturnsDefault()
    {
        Assert.IsNull(Library.GetElementAtOrDefault<string>(null, 0));
    }

    [TestMethod]
    public void GetElementAtOrDefault_NullIndex_ReturnsDefault()
    {
        Assert.IsNull(Library.GetElementAtOrDefault(new[] { "a", "b" }, null));
    }

    [TestMethod]
    public void GetElementAtOrDefault_ValidIndex_ReturnsElement()
    {
        Assert.AreEqual("b", Library.GetElementAtOrDefault(new[] { "a", "b", "c" }, 1));
    }

    [TestMethod]
    public void GetElementAtOrDefault_IndexOutOfRange_ReturnsDefault()
    {
        Assert.IsNull(Library.GetElementAtOrDefault(new[] { "a", "b" }, 10));
    }

    #endregion

    #region LongestCommonSequence Tests

    [TestMethod]
    public void LongestCommonSequence_NullSource_ReturnsNull()
    {
        Assert.IsNull(Library.LongestCommonSequence(null, new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void LongestCommonSequence_NullPattern_ReturnsNull()
    {
        Assert.IsNull(Library.LongestCommonSequence(new[] { 1, 2, 3 }, null));
    }

    [TestMethod]
    public void LongestCommonSequence_NoCommon_ReturnsEmpty()
    {
        var result = Library.LongestCommonSequence(new[] { 1, 2, 3 }, new[] { 4, 5, 6 });
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
    }

    [TestMethod]
    public void LongestCommonSequence_HasCommon_ReturnsLongest()
    {
        var result = Library.LongestCommonSequence(new[] { 1, 2, 3, 4, 5 }, new[] { 2, 3, 4 });
        Assert.IsNotNull(result);
        var array = result.ToArray();
        Assert.HasCount(3, array);
        Assert.AreEqual(2, array[0]);
        Assert.AreEqual(3, array[1]);
        Assert.AreEqual(4, array[2]);
    }

    [TestMethod]
    public void LongestCommonSequence_IdenticalSequences_ReturnsAll()
    {
        var result = Library.LongestCommonSequence(new[] { 1, 2, 3 }, new[] { 1, 2, 3 });
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Count());
    }

    #endregion
}
