using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class EvaluationHelperNestedValueTests
{
    [TestMethod]
    public void NestedClrProperties_ShouldResolveAndPreserveNulls()
    {
        var value = new OuterEntity(new InnerEntity("resolved"));

        Assert.AreEqual("resolved", EvaluationHelper.GetNestedValue(value, "Child.Value"));
        Assert.IsNull(EvaluationHelper.GetNestedValue(new OuterEntity(null), "Child.Value"));
    }

    [TestMethod]
    public void Dictionaries_ShouldResolveNestedValues()
    {
        var value = new Dictionary<string, object>
        {
            ["Child"] = new Dictionary<string, object> { ["Value"] = "dictionary" }
        };

        Assert.AreEqual("dictionary", EvaluationHelper.GetNestedValue(value, "Child.Value"));
    }

    [TestMethod]
    public void DynamicObjects_ShouldResolveNestedValues()
    {
        var value = new TestDynamicObject(
            new Dictionary<string, object?>
            {
                ["Child"] = new TestDynamicObject(new Dictionary<string, object?> { ["Value"] = "dynamic" })
            });

        Assert.AreEqual("dynamic", EvaluationHelper.GetNestedValue(value, "Child.Value"));
    }

    [TestMethod]
    public void Indexers_ShouldResolveNumericAndStringPaths()
    {
        var value = new IndexedEntity(
            [new InnerEntity("numeric")],
            new Dictionary<string, InnerEntity> { ["child"] = new("string") });

        Assert.AreEqual("numeric", EvaluationHelper.GetNestedValue(value, "[0].Value"));
        Assert.AreEqual("string", EvaluationHelper.GetNestedValue(value, "Values['child'].Value"));
    }

    [TestMethod]
    public void MissingMembers_ShouldKeepTheExistingExceptionContract()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => EvaluationHelper.GetNestedValue(new OuterEntity(null), "Missing"));

        StringAssert.Contains(exception.Message, "Missing");
    }

    [TestMethod]
    public void ThrowingGetters_ShouldPropagateTheGetterException()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => EvaluationHelper.GetNestedValue(new ThrowingEntity(), "Value"));

        Assert.AreEqual("getter", exception.Message);
    }

    private sealed class OuterEntity(InnerEntity? child)
    {
        public InnerEntity? Child { get; } = child;
    }

    private sealed class InnerEntity(string value)
    {
        public string Value { get; } = value;
    }

    private sealed class ThrowingEntity
    {
        public string Value => throw new InvalidOperationException("getter");
    }

    private sealed class IndexedEntity(
        IReadOnlyList<InnerEntity> numericValues,
        IReadOnlyDictionary<string, InnerEntity> namedValues)
    {
        public IReadOnlyDictionary<string, InnerEntity> Values { get; } = namedValues;

        public InnerEntity this[int index] => numericValues[index];
    }

    private sealed class TestDynamicObject(IReadOnlyDictionary<string, object?> values) : DynamicObject
    {
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            return values.TryGetValue(binder.Name, out result);
        }
    }
}
