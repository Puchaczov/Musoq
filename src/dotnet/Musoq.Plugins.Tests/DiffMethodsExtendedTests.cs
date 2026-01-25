using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for Diff methods to improve branch coverage.
///     Tests Diff, DiffSegments with various modes and edge cases.
/// </summary>
[TestClass]
public class DiffMethodsExtendedTests : LibraryBaseBaseTests
{
    #region Diff Tests

    [TestMethod]
    public void Diff_BothNull_ReturnsNull()
    {
        Assert.IsNull(Library.Diff(null, null));
    }

    [TestMethod]
    public void Diff_FirstNull_ReturnsInserted()
    {
        Assert.AreEqual("[+hello]", Library.Diff(null, "hello"));
    }

    [TestMethod]
    public void Diff_SecondNull_ReturnsDeleted()
    {
        Assert.AreEqual("[-hello]", Library.Diff("hello", null));
    }

    [TestMethod]
    public void Diff_BothEmpty_ReturnsEmpty()
    {
        Assert.AreEqual(string.Empty, Library.Diff(string.Empty, string.Empty));
    }

    [TestMethod]
    public void Diff_FirstEmpty_ReturnsInserted()
    {
        Assert.AreEqual("[+hello]", Library.Diff(string.Empty, "hello"));
    }

    [TestMethod]
    public void Diff_SecondEmpty_ReturnsDeleted()
    {
        Assert.AreEqual("[-hello]", Library.Diff("hello", string.Empty));
    }

    [TestMethod]
    public void Diff_Identical_FullMode_ReturnsOriginal()
    {
        Assert.AreEqual("hello", Library.Diff("hello", "hello"));
    }

    [TestMethod]
    public void Diff_Identical_CompactMode_ReturnsUnchangedMarker()
    {
        Assert.AreEqual("[=5]", Library.Diff("hello", "hello", "compact"));
    }

    [TestMethod]
    public void Diff_DefaultMode_UsesFull()
    {
        var result = Library.Diff("hello", "hello");
        Assert.AreEqual("hello", result);
    }

    [TestMethod]
    public void Diff_NullMode_UsesFull()
    {
        var result = Library.Diff("hello", "hello", null);
        Assert.AreEqual("hello", result);
    }

    [TestMethod]
    public void Diff_EmptyMode_ReturnsNull()
    {
        Assert.IsNull(Library.Diff("hello", "world", string.Empty));
    }

    [TestMethod]
    public void Diff_InvalidMode_ReturnsNull()
    {
        Assert.IsNull(Library.Diff("hello", "world", "invalid"));
    }

    [TestMethod]
    public void Diff_FullThresholdMode_Valid()
    {
        var result = Library.Diff("hello world", "hello world", "full:5");
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void Diff_FullThresholdMode_InvalidNumber_ReturnsNull()
    {
        Assert.IsNull(Library.Diff("hello", "world", "full:abc"));
    }

    [TestMethod]
    public void Diff_FullThresholdMode_NegativeNumber_ReturnsNull()
    {
        Assert.IsNull(Library.Diff("hello", "world", "full:-1"));
    }

    [TestMethod]
    public void Diff_FullThresholdMode_ZeroThreshold_ReturnsNull()
    {
        Assert.IsNull(Library.Diff("hello", "world", "full:0"));
    }

    [TestMethod]
    public void Diff_SimpleChange_ShowsDiff()
    {
        var result = Library.Diff("hello", "hallo");
        Assert.IsNotNull(result);

        Assert.IsTrue(result.Contains("[-") || result.Contains("[+"));
    }

    [TestMethod]
    public void Diff_Addition_ShowsInserted()
    {
        var result = Library.Diff("hello", "hello world");
        Assert.IsNotNull(result);
        Assert.Contains("[+", result);
    }

    [TestMethod]
    public void Diff_Deletion_ShowsDeleted()
    {
        var result = Library.Diff("hello world", "hello");
        Assert.IsNotNull(result);
        Assert.Contains("[-", result);
    }

    [TestMethod]
    public void Diff_CompactMode_CollapseUnchanged()
    {
        var result = Library.Diff("hello", "hallo", "compact");
        Assert.IsNotNull(result);
    }

    #endregion

    #region DiffSegments Tests

    [TestMethod]
    public void DiffSegments_BothNull_ReturnsEmpty()
    {
        var result = Library.DiffSegments(null, null);
        Assert.AreEqual(0, result.Count());
    }

    [TestMethod]
    public void DiffSegments_FirstNull_ReturnsInserted()
    {
        var result = Library.DiffSegments(null, "hello").ToList();
        Assert.HasCount(1, result);
        Assert.AreEqual("Inserted", result[0].Kind);
        Assert.AreEqual("hello", result[0].Text);
        Assert.AreEqual(0, result[0].Position);
        Assert.AreEqual(5, result[0].Length);
    }

    [TestMethod]
    public void DiffSegments_SecondNull_ReturnsDeleted()
    {
        var result = Library.DiffSegments("hello", null).ToList();
        Assert.HasCount(1, result);
        Assert.AreEqual("Deleted", result[0].Kind);
        Assert.AreEqual("hello", result[0].Text);
    }

    [TestMethod]
    public void DiffSegments_BothEmpty_ReturnsEmpty()
    {
        var result = Library.DiffSegments(string.Empty, string.Empty);
        Assert.AreEqual(0, result.Count());
    }

    [TestMethod]
    public void DiffSegments_FirstEmpty_ReturnsInserted()
    {
        var result = Library.DiffSegments(string.Empty, "hello").ToList();
        Assert.HasCount(1, result);
        Assert.AreEqual("Inserted", result[0].Kind);
    }

    [TestMethod]
    public void DiffSegments_SecondEmpty_ReturnsDeleted()
    {
        var result = Library.DiffSegments("hello", string.Empty).ToList();
        Assert.HasCount(1, result);
        Assert.AreEqual("Deleted", result[0].Kind);
    }

    [TestMethod]
    public void DiffSegments_Identical_ReturnsUnchanged()
    {
        var result = Library.DiffSegments("hello", "hello").ToList();
        Assert.HasCount(1, result);
        Assert.AreEqual("Unchanged", result[0].Kind);
        Assert.AreEqual("hello", result[0].Text);
    }

    [TestMethod]
    public void DiffSegments_SimpleChange_ReturnsMultipleSegments()
    {
        var result = Library.DiffSegments("hello", "hallo").ToList();
        Assert.IsGreaterThanOrEqualTo(2, result.Count);
    }

    [TestMethod]
    public void DiffSegments_Addition_HasInsertedSegment()
    {
        var result = Library.DiffSegments("ab", "abc").ToList();
        Assert.IsTrue(result.Any(s => s.Kind == "Inserted"));
    }

    [TestMethod]
    public void DiffSegments_Deletion_HasDeletedSegment()
    {
        var result = Library.DiffSegments("abc", "ab").ToList();
        Assert.IsTrue(result.Any(s => s.Kind == "Deleted"));
    }

    #endregion
}
