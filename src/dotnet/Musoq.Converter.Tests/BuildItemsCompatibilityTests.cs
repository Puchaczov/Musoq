using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class BuildItemsCompatibilityTests
{
    [TestMethod]
    public void LegacyDictionaryCompatibility_ShouldKeepPublicDictionaryInheritance()
    {
        var items = new BuildItems();

        Assert.IsInstanceOfType<Dictionary<string, object>>(items);
        Assert.IsInstanceOfType<IDictionary<string, object>>(items);
    }

    [TestMethod]
    public void DictionaryCompatibility_WhenRawKeyIsWritten_ShouldStillBeVisibleThroughTypedAccessor()
    {
        var items = new BuildItems
        {
            [BuildItemKeys.AssemblyName] = "raw-assembly"
        };

        Assert.AreEqual("raw-assembly", items.AssemblyName);
    }

    [TestMethod]
    public void DictionaryCompatibility_WhenTypedAccessorIsWritten_ShouldStillUpdateRawDictionary()
    {
        var items = new BuildItems
        {
            AssemblyName = "typed-assembly"
        };

        Assert.AreEqual("typed-assembly", items[BuildItemKeys.AssemblyName]);
    }

    [TestMethod]
    public void DictionaryCompatibility_WhenRawValueHasWrongType_ShouldKeepCurrentInvalidCastBehavior()
    {
        var items = new BuildItems
        {
            [BuildItemKeys.AssemblyName] = 123
        };

        Assert.Throws<InvalidCastException>(() => _ = items.AssemblyName);
    }
}
