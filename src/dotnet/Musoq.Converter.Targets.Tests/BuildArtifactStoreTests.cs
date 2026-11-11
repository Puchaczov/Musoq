using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class BuildArtifactStoreTests
{
    [TestMethod]
    public void GetRequired_WhenMissing_ShouldReportSlotKey()
    {
        var store = new BuildArtifactStore(new Dictionary<string, object>());
        var slot = new BuildArtifactSlot<string>("REQUIRED_VALUE");

        var exception = Assert.Throws<KeyNotFoundException>(() => store.GetRequired(slot));

        Assert.Contains("REQUIRED_VALUE", exception.Message);
    }

    [TestMethod]
    public void SetRequiredAndGetRequired_ShouldUseSharedBackingDictionary()
    {
        var backing = new Dictionary<string, object>();
        var store = new BuildArtifactStore(backing);
        var slot = new BuildArtifactSlot<string>("VALUE");

        store.SetRequired(slot, "stored");

        Assert.AreEqual("stored", backing["VALUE"]);
        Assert.AreEqual("stored", store.GetRequired(slot));
    }

    [TestMethod]
    public void GetRequired_WhenRawBackingValueHasWrongType_ShouldKeepInvalidCastBehavior()
    {
        var store = new BuildArtifactStore(new Dictionary<string, object>
        {
            ["VALUE"] = 123
        });
        var slot = new BuildArtifactSlot<string>("VALUE");

        Assert.Throws<InvalidCastException>(() => store.GetRequired(slot));
    }

    [TestMethod]
    public void OptionalArtifacts_WhenSetToNull_ShouldRemoveBackingValue()
    {
        var backing = new Dictionary<string, object>();
        var store = new BuildArtifactStore(backing);
        var slot = new BuildArtifactSlot<string>("OPTIONAL_VALUE");

        store.SetOptional(slot, "stored");
        store.SetOptional(slot, null);

        Assert.IsFalse(backing.ContainsKey("OPTIONAL_VALUE"));
        Assert.IsNull(store.GetOptional(slot));
    }

    [TestMethod]
    public void FlagsAndLists_WhenMissing_ShouldReturnDefaults()
    {
        var store = new BuildArtifactStore(new Dictionary<string, object>());

        Assert.IsTrue(store.GetFlag(new BuildArtifactSlot<bool>("FLAG"), defaultWhenMissing: true));
        Assert.IsEmpty(store.GetListOrEmpty(new BuildArtifactSlot<IReadOnlyList<string>>("LIST")));
    }
}
