using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed partial class GenericTests : PluginsTestBase
{
    [TestMethod]
    [DynamicData(nameof(ByteMergeCases))]
    public void MergeArrays_ByteCases_ReturnExpected(byte[][] values, string expected)
    {
        Assert.AreEqual(expected, Encoding.UTF8.GetString(Library.MergeArrays(values)!));
    }

    public static IEnumerable<object?[]> ByteMergeCases()
    {
        yield return [new[] { "test1"u8.ToArray() }, "test1"];
        yield return [new[] { "test1"u8.ToArray(), "test2"u8.ToArray() }, "test1test2"];
        yield return [new[] { "test1"u8.ToArray(), "test2"u8.ToArray(), "test3"u8.ToArray() }, "test1test2test3"];
    }

    [TestMethod]
    [DynamicData(nameof(IntegerSequenceCases))]
    public void IntegerSequence_Cases_ReturnExpected(string name, Func<LibraryBase, IEnumerable<int>?> execute, int[] expected)
    {
        CollectionAssert.AreEqual(expected, execute(Library)!.ToArray(), name);
    }

    public static IEnumerable<object?[]> IntegerSequenceCases()
    {
        yield return SequenceCase("Skip_ValidCount", library => library.Skip([1, 2, 3, 4, 5], 2), [3, 4, 5]);
        yield return SequenceCase("Skip_ZeroCount", library => library.Skip([1, 2, 3], 0), [1, 2, 3]);
        yield return SequenceCase("Take_ValidCount", library => library.Take([1, 2, 3, 4, 5], 3), [1, 2, 3]);
        yield return SequenceCase("Take_ZeroCount", library => library.Take([1, 2, 3], 0), []);
        yield return SequenceCase("SkipAndTake_ValidCounts", library => library.SkipAndTake([1, 2, 3, 4, 5, 6, 7], 2, 3), [3, 4, 5]);
        yield return SequenceCase("EnumerableToArray_Enumerable", library => library.EnumerableToArray(Enumerable.Range(1, 3)), [1, 2, 3]);
        yield return SequenceCase("MergeArrays_MultipleArrays", library => library.MergeArrays([1, 2], [3, 4], [5]), [1, 2, 3, 4, 5]);
        yield return SequenceCase("Distinct_WithDuplicates", library => library.Distinct([1, 2, 2, 3, 3, 3, 4]), [1, 2, 3, 4]);
        yield return SequenceCase("Distinct_WithoutDuplicates", library => library.Distinct([1, 2, 3, 4]), [1, 2, 3, 4]);
        yield return SequenceCase("LongestCommonSequence_NoCommon", library => library.LongestCommonSequence([1, 2, 3],
            [4, 5, 6]), []);
        yield return SequenceCase("LongestCommonSequence_HasCommon", library => library.LongestCommonSequence([1, 2, 3, 4, 5
        ], [2, 3, 4]), [2, 3, 4]);
        yield return SequenceCase("LongestCommonSequence_Identical", library => library.LongestCommonSequence([1, 2, 3],
            [1, 2, 3]), [1, 2, 3]);
    }

    [TestMethod]
    [DynamicData(nameof(NullObjectCases))]
    public void NullObject_Cases_ReturnNull(string name, Func<LibraryBase, object?> execute)
    {
        Assert.IsNull(execute(Library), name);
    }

    public static IEnumerable<object?[]> NullObjectCases()
    {
        yield return NullCase("MergeArrays_ByteNull", library => library.MergeArrays((byte[][])null!));
        yield return NullCase("MergeArrays_IntegerNull", library => library.MergeArrays<int>(null!));
        yield return NullCase("Skip_Null", library => library.Skip<int>(null, 2));
        yield return NullCase("Take_Null", library => library.Take<int>(null, 2));
        yield return NullCase("SkipAndTake_Null", library => library.SkipAndTake<int>(null, 1, 2));
        yield return NullCase("EnumerableToArray_Null", library => library.EnumerableToArray<int>(null));
        yield return NullCase("Distinct_Null", library => library.Distinct<int>(null));
        yield return NullCase("LongestCommonSequence_NullSource", library => library.LongestCommonSequence(null, [1, 2, 3
        ]));
        yield return NullCase("LongestCommonSequence_NullPattern", library => library.LongestCommonSequence([1, 2, 3], null));
    }

    [TestMethod]
    [DynamicData(nameof(ScalarCases))]
    public void Scalar_Cases_ReturnExpected(string name, Func<LibraryBase, object?> execute, object? expected)
    {
        Assert.AreEqual(expected, execute(Library), name);
    }

    public static IEnumerable<object?[]> ScalarCases()
    {
        return LengthCases()
            .Concat(ElementSelectionCases())
            .Concat(NullUtilityCases())
            .Concat(ConditionalAndMatchCases())
            .Concat(GenericCoalesceCases());
    }

    private static IEnumerable<object?[]> LengthCases()
    {
        yield return ScalarCase("Length_Enumerable", library => library.Length(Enumerable.Range(1, 5)), 5);
        yield return ScalarCase("Length_NullEnumerable", library => { IEnumerable<int>? values = null; return library.Length(values); }, null);
        yield return ScalarCase("Length_Array", library => library.Length([1, 2, 3]), 3);
        yield return ScalarCase("Length_NullArray", library => { int[]? values = null; return library.Length(values); }, null);
    }

    private static IEnumerable<object?[]> ElementSelectionCases()
    {
        yield return ScalarCase("FirstOrDefault_HasElements", library => library.FirstOrDefault(["first", "second", "third"
        ]), "first");
        yield return ScalarCase("FirstOrDefault_Empty", library => library.FirstOrDefault(Array.Empty<string>()), null);
        yield return ScalarCase("FirstOrDefault_Null", library => library.FirstOrDefault<string>(null), null);
        yield return ScalarCase("LastOrDefault_HasElements", library => library.LastOrDefault(["first", "second", "third"
        ]), "third");
        yield return ScalarCase("LastOrDefault_Empty", library => library.LastOrDefault(Array.Empty<string>()), null);
        yield return ScalarCase("LastOrDefault_Null", library => library.LastOrDefault<string>(null), null);
        yield return ScalarCase("GetElementAtOrDefault_ValidIndex", library => library.GetElementAtOrDefault(["first", "second", "third"
        ], 1), "second");
        yield return ScalarCase("GetElementAtOrDefault_OutOfRange", library => library.GetElementAtOrDefault(["first", "second"
        ], 10), null);
        yield return ScalarCase("GetElementAtOrDefault_NullCollection", library => library.GetElementAtOrDefault<string>(null, 0), null);
        yield return ScalarCase("GetElementAtOrDefault_NullIndex", library => library.GetElementAtOrDefault(["first", "second"
        ], null), null);
        yield return ScalarCase("GetElementAt_ValidIndex", library => library.GetElementAt(["first", "second", "third"], 1), "second");
        yield return ScalarCase("GetElementAt_NullCollection", library => library.GetElementAt<string>(null, 0), null);
        yield return ScalarCase("GetElementAt_NullIndex", library => library.GetElementAt(["first", "second"], null), null);
        yield return ScalarCase("NthOrDefault_ValidIndex", library => library.NthOrDefault(["zero", "one", "two", "three"
        ], 2), "two");
        yield return ScalarCase("NthOrDefault_OutOfRange", library => library.NthOrDefault(["zero", "one", "two"], 10), null);
        yield return ScalarCase("NthOrDefault_Null", library => library.NthOrDefault<string>(null, 2), null);
        yield return ScalarCase("NthOrDefault_NegativeIndex", library => library.NthOrDefault(["zero", "one", "two"], -1), null);
        yield return ScalarCase("NthFromEndOrDefault_ListLast", library => library.NthFromEndOrDefault(new List<int> { 1, 2, 3, 4, 5 }, 0), 5);
        yield return ScalarCase("NthFromEndOrDefault_ListSecondFromEnd", library => library.NthFromEndOrDefault(new List<int> { 1, 2, 3, 4, 5 }, 1), 4);
        yield return ScalarCase("NthFromEndOrDefault_ListOutOfRange", library => library.NthFromEndOrDefault(new List<int> { 1, 2, 3 }, 10), 0);
        yield return ScalarCase("NthFromEndOrDefault_ArrayLast", library => library.NthFromEndOrDefault([1, 2, 3, 4, 5], 0), 5);
        yield return ScalarCase("NthFromEndOrDefault_ArraySecondFromEnd", library => library.NthFromEndOrDefault([1, 2, 3, 4, 5
        ], 1), 4);
        yield return ScalarCase("NthFromEndOrDefault_ArrayOutOfRange", library => library.NthFromEndOrDefault([1, 2, 3], 10), 0);
        yield return ScalarCase("NthFromEndOrDefault_EnumerableLast", library => library.NthFromEndOrDefault(Enumerable.Range(1, 5), 0), 5);
        yield return ScalarCase("NthFromEndOrDefault_EnumerableSecondFromEnd", library => library.NthFromEndOrDefault(Enumerable.Range(1, 5), 1), 4);
        yield return ScalarCase("NthFromEndOrDefault_EnumerableOutOfRange", library => library.NthFromEndOrDefault(Enumerable.Range(1, 3), 10), 0);
        yield return ScalarCase("NthFromEndOrDefault_EmptyList", library => library.NthFromEndOrDefault(new List<int>(), 0), 0);
        yield return ScalarCase("NthFromEndOrDefault_SingleElementList", library => library.NthFromEndOrDefault(new List<int> { 42 }, 0), 42);
    }

    private static IEnumerable<object?[]> NullUtilityCases()
    {
        yield return ScalarCase("NullIf_BothNull", library => library.NullIf<string>(null, null), null);
        yield return ScalarCase("NullIf_ValueNullCompareNotNull", library => library.NullIf<string>(null, "test"), null);
        yield return ScalarCase("NullIf_ValueNotNullCompareNull", library => library.NullIf("test", null), "test");
        yield return ScalarCase("NullIf_StringEqual", library => library.NullIf("test", "test"), null);
        yield return ScalarCase("NullIf_StringNotEqual", library => library.NullIf("hello", "world"), "hello");
        yield return ScalarCase("NullIf_IntegerEqual", library => library.NullIf(5, 5), 0);
        yield return ScalarCase("NullIf_IntegerNotEqual", library => library.NullIf(5, 10), 5);
        yield return ScalarCase("NullIf_NullableIntegerEqual", library => library.NullIf<int?>(42, 42), null);
        yield return ScalarCase("NullIf_NullableIntegerNotEqual", library => library.NullIf<int?>(42, 100), 42);
        yield return ScalarCase("IfNull_ValueNull", library => library.IfNull<string>(null, "default"), "default");
        yield return ScalarCase("IfNull_ValueNotNull", library => library.IfNull("hello", "default"), "hello");
        yield return ScalarCase("IfNull_BothNull", library => library.IfNull<string>(null, null), null);
        yield return ScalarCase("IfNull_IntegerNull", library => library.IfNull<int?>(null, 42), 42);
        yield return ScalarCase("IfNull_IntegerNotNull", library => library.IfNull<int?>(100, 42), 100);
        yield return ScalarCase("DefaultIfNull_Null", library => library.DefaultIfNull<string>(null), null);
        yield return ScalarCase("DefaultIfNull_NotNull", library => library.DefaultIfNull("hello"), "hello");
        yield return ScalarCase("DefaultIfNull_IntegerNull", library => library.DefaultIfNull<int?>(null), null);
        yield return ScalarCase("DefaultIfNull_IntegerNotNull", library => library.DefaultIfNull<int?>(42), 42);
        yield return ScalarCase("IsNull_Null", library => library.IsNull<string>(null), true);
        yield return ScalarCase("IsNull_NotNull", library => library.IsNull("hello"), false);
        yield return ScalarCase("IsNull_IntegerNull", library => library.IsNull<int?>(null), true);
        yield return ScalarCase("IsNull_IntegerNotNull", library => library.IsNull<int?>(42), false);
        yield return ScalarCase("IsNotNull_Null", library => library.IsNotNull<string>(null), false);
        yield return ScalarCase("IsNotNull_NotNull", library => library.IsNotNull("hello"), true);
        yield return ScalarCase("IsNotNull_IntegerNull", library => library.IsNotNull<int?>(null), false);
        yield return ScalarCase("IsNotNull_IntegerNotNull", library => library.IsNotNull<int?>(42), true);
    }

    private static IEnumerable<object?[]> ConditionalAndMatchCases()
    {
        yield return ScalarCase("If_TrueString", library => library.If(true, "yes", "no"), "yes");
        yield return ScalarCase("If_FalseString", library => library.If(false, "yes", "no"), "no");
        yield return ScalarCase("If_TrueInteger", library => library.If(true, 1, 2), 1);
        yield return ScalarCase("If_FalseInteger", library => library.If(false, 1, 2), 2);
        yield return ScalarCase("Choose_ValidIndex", library => library.Choose(1, "a", "b", "c"), "b");
        yield return ScalarCase("Choose_FirstIndex", library => library.Choose(0, "a", "b", "c"), "a");
        yield return ScalarCase("Choose_LastIndex", library => library.Choose(2, "a", "b", "c"), "c");
        yield return ScalarCase("Choose_IndexOutOfRange", library => library.Choose(5, "a", "b", "c"), null);
        yield return ScalarCase("Choose_IntegerValidIndex", library => library.Choose(1, 10, 20, 30), 20);
        yield return ScalarCase("Match_NullRegex", library => library.Match(null, "test"), null);
        yield return ScalarCase("Match_NullContent", library => library.Match(@"\d+", null), null);
        yield return ScalarCase("Match_Matches", library => library.Match(@"\d+", "test123"), true);
        yield return ScalarCase("Match_NoMatch", library => library.Match(@"\d+", "test"), false);
    }

    private static IEnumerable<object?[]> GenericCoalesceCases()
    {
        yield return ScalarCase("Coalesce_AllNull", library => library.Coalesce<string>(null!, null!, null!), null);
        yield return ScalarCase("Coalesce_FirstNotNull", library => library.Coalesce("first", "second", "third"), "first");
        yield return ScalarCase("Coalesce_FirstNull", library => library.Coalesce<string>(null!, "second", "third"), "second");
        yield return ScalarCase("Coalesce_FirstTwoNull", library => library.Coalesce<string>(null!, null!, "third"), "third");
        yield return ScalarCase("Coalesce_SingleElement", library => library.Coalesce("only"), "only");
        yield return ScalarCase("Coalesce_SingleNullElement", library => library.Coalesce(new string[] { null! }), null);
        yield return ScalarCase("Coalesce_IntegerFirstNotNull", library => library.Coalesce<int?>(42, 100, 200), 42);
        yield return ScalarCase("Coalesce_IntegerFirstNull", library => library.Coalesce<int?>(null, 100, 200), 100);
    }

    [TestMethod]
    public void Choose_NegativeIndex_ThrowsException()
    {
        Assert.Throws<IndexOutOfRangeException>(() => Library.Choose(-1, "a", "b", "c"));
    }

    [TestMethod]
    public void GetElementAt_IndexOutOfRange_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Library.GetElementAt(["a", "b"], 10));
    }

    private static object?[] SequenceCase(string name, Func<LibraryBase, IEnumerable<int>?> execute, int[] expected)
    {
        return [name, execute, expected];
    }

    private static object?[] NullCase(string name, Func<LibraryBase, object?> execute)
    {
        return [name, execute];
    }

    private static object?[] ScalarCase(string name, Func<LibraryBase, object?> execute, object? expected)
    {
        return [name, execute, expected];
    }

}