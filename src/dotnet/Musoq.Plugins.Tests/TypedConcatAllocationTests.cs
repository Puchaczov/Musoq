using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class TypedConcatAllocationTests
{
    [TestMethod]
    public void FixedArityNumericConcat_ShouldAllocateOnlyFormattingAndTheResultString()
    {
        var library = new LibraryBase();
        int? first = 42;
        int? second = 7;
        for (var index = 0; index < 10_000; index++)
        {
            _ = string.Concat(first.ToString(), second.ToString());
            _ = library.Concat<int?, int?>(first, second);
        }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
            _ = string.Concat(first.ToString(), second.ToString());
        var baseline = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
            _ = library.Concat<int?, int?>(first, second);
        var actual = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.AreEqual(baseline, actual);
        Assert.AreEqual("427", library.Concat<int?, int?>(first, second));
        Assert.IsNull(library.Concat<int?, int?>(first, null));
    }
}
