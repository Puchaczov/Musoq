using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class TypedConcatTests
{
    [TestMethod]
    public void FixedArityNumericConcat_ShouldFormatValuesAndPropagateNull()
    {
        var library = new LibraryBase();
        int? first = 42;
        int? second = 7;

        Assert.AreEqual("427", library.Concat<int?, int?>(first, second));
        Assert.IsNull(library.Concat<int?, int?>(first, null));
    }
}
