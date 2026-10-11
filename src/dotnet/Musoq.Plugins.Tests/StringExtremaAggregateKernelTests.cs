using System;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class StringExtremaAggregateKernelTests
{
    [TestMethod]
    [DataRow(null, null, null, null)]
    [DataRow(null, "a", "a", "a")]
    [DataRow("a", null, "a", "a")]
    [DataRow("", "a", "", "a")]
    [DataRow("", "", "", "")]
    [DataRow("a", "A", "A", "a")]
    [DataRow("z", "z", "z", "z")]
    [DataRow("\uE000", "\U00010000", "\U00010000", "\uE000")]
    public void Set_ShouldSelectOrdinalExtremaAndSkipNulls(string? first, string? second, string? minimum, string? maximum)
    {
        var min = new MinStringAggregateKernel.State();
        var max = new MaxStringAggregateKernel.State();

        MinStringAggregateKernel.Set(ref min, first);
        MinStringAggregateKernel.Set(ref min, second);
        MaxStringAggregateKernel.Set(ref max, first);
        MaxStringAggregateKernel.Set(ref max, second);

        Assert.AreEqual(minimum, MinStringAggregateKernel.Get(in min));
        Assert.AreEqual(maximum, MaxStringAggregateKernel.Get(in max));
    }

    [TestMethod]
    public void EmptyState_ShouldReturnNull()
    {
        var min = new MinStringAggregateKernel.State();
        var max = new MaxStringAggregateKernel.State();

        Assert.IsNull(MinStringAggregateKernel.Get(in min));
        Assert.IsNull(MaxStringAggregateKernel.Get(in max));
    }

    [TestMethod]
    [DataRow(null, null, null, null)]
    [DataRow(null, "a", "a", "a")]
    [DataRow("a", null, "a", "a")]
    [DataRow("z", "A", "A", "z")]
    [DataRow("A", "z", "A", "z")]
    [DataRow("", "a", "", "a")]
    [DataRow("z", "z", "z", "z")]
    [DataRow("\uE000", "\U00010000", "\U00010000", "\uE000")]
    public void Merge_ShouldSelectOrdinalExtremaWithoutChangingSource(string? first, string? second, string? minimum, string? maximum)
    {
        var minTarget = new MinStringAggregateKernel.State();
        var maxTarget = new MaxStringAggregateKernel.State();
        var minSource = new MinStringAggregateKernel.State();
        var maxSource = new MaxStringAggregateKernel.State();
        MinStringAggregateKernel.Set(ref minTarget, first);
        MaxStringAggregateKernel.Set(ref maxTarget, first);
        MinStringAggregateKernel.Set(ref minSource, second);
        MaxStringAggregateKernel.Set(ref maxSource, second);

        MinStringAggregateKernel.Merge(ref minTarget, in minSource);
        MaxStringAggregateKernel.Merge(ref maxTarget, in maxSource);

        Assert.AreEqual(minimum, MinStringAggregateKernel.Get(in minTarget));
        Assert.AreEqual(maximum, MaxStringAggregateKernel.Get(in maxTarget));
        Assert.AreEqual(second, MinStringAggregateKernel.Get(in minSource));
        Assert.AreEqual(second, MaxStringAggregateKernel.Get(in maxSource));
    }

    [TestMethod]
    [DataRow("en-US")]
    [DataRow("tr-TR")]
    public void StringExtrema_ShouldIgnoreCurrentCulture(string cultureName)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var min = new MinStringAggregateKernel.State();
            var max = new MaxStringAggregateKernel.State();
            MinStringAggregateKernel.Set(ref min, "I");
            MinStringAggregateKernel.Set(ref min, "i");
            MaxStringAggregateKernel.Set(ref max, "I");
            MaxStringAggregateKernel.Set(ref max, "i");

            Assert.AreEqual("I", MinStringAggregateKernel.Get(in min));
            Assert.AreEqual("i", MaxStringAggregateKernel.Get(in max));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    [DataRow("Min", typeof(MinStringAggregateKernel))]
    [DataRow("Max", typeof(MaxStringAggregateKernel))]
    public void Declaration_ShouldExposeMergeableNullableStringContract(string name, Type kernelType)
    {
        var declaration = typeof(LibraryBase).GetMethod(name, [typeof(string), typeof(int)])!;

        var descriptor = AggregateKernelDescriptor.Create(declaration);

        Assert.AreEqual(kernelType, descriptor.KernelType);
        Assert.AreEqual(typeof(string), descriptor.ResultType);
        Assert.AreEqual(AggregateEmptyResultBehavior.Null, descriptor.ResultDescriptor.EmptyResultBehavior);
        Assert.IsTrue(descriptor.SupportsMerge);
    }
}
