using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class QueryScopedRowContractsTests
{
    [TestMethod]
    public void QueryRowShape_ShouldFreezeFieldsAndProduceStableFingerprint()
    {
        var modifiers = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["culture"] = "en-US",
            ["trim"] = "true"
        };
        var shape = new QueryRowShape(
        [
            new QueryRowField(0, 4, "Name", typeof(string), true, modifiers),
            new QueryRowField(1, 9, "Count", typeof(int?), true)
        ]);
        modifiers["new"] = "value";

        var equivalent = new QueryRowShape(
        [
            new QueryRowField(0, 4, "Name", typeof(string), true,
                new Dictionary<string, string> { ["trim"] = "true", ["culture"] = "en-US" }),
            new QueryRowField(1, 9, "Count", typeof(int?), true)
        ]);

        Assert.AreEqual(2, shape.Fields.Count);
        Assert.AreEqual("en-US", shape.Fields[0].ReadModifiers["culture"]);
        Assert.AreEqual(shape.Fingerprint, equivalent.Fingerprint);
        Assert.IsFalse(shape.Fingerprint.Length == 0);
        Assert.Throws<KeyNotFoundException>(() => shape.Fields[0].ReadModifiers["new"]);
    }

    [TestMethod]
    public void QueryRowShape_ShouldRejectNonContiguousSlots()
    {
        var exception = Assert.Throws<ArgumentException>(() => new QueryRowShape(
        [new QueryRowField(1, 0, "Name", typeof(string), true)]));

        StringAssert.Contains(exception.Message, "contiguous");
    }

    [TestMethod]
    public void QueryRowField_ShouldRejectRefLikeTypes()
    {
        Assert.Throws<ArgumentException>(() =>
            new QueryRowField(0, 0, "Value", typeof(Span<int>), false));
    }

    [TestMethod]
    public void QueryRowField_ShouldAcceptVisibleArrayType()
    {
        Assert.IsTrue(QueryRowField.IsSupportedFieldType(typeof(int[])));
    }

    [TestMethod]
    public void QueryRowField_ShouldAcceptVisibleClosedGenericType()
    {
        Assert.IsTrue(QueryRowField.IsSupportedFieldType(typeof(IReadOnlyDictionary<string, int>)));
    }

    [TestMethod]
    public void QueryRowField_ShouldRejectVoidType()
    {
        Assert.IsFalse(QueryRowField.IsSupportedFieldType(typeof(void)));
    }

    [TestMethod]
    public unsafe void QueryRowField_ShouldRejectFunctionPointerType()
    {
        Assert.IsFalse(QueryRowField.IsSupportedFieldType(typeof(delegate*<void>)));
    }

    [TestMethod]
    public void QueryRowField_ShouldRejectPointerType()
    {
        Assert.IsFalse(QueryRowField.IsSupportedFieldType(typeof(int).MakePointerType()));
    }

    [TestMethod]
    public void QueryRowField_ShouldRejectByRefType()
    {
        Assert.IsFalse(QueryRowField.IsSupportedFieldType(typeof(int).MakeByRefType()));
    }

    [TestMethod]
    public void QueryRowField_ShouldRejectOpenGenericType()
    {
        Assert.IsFalse(QueryRowField.IsSupportedFieldType(typeof(List<>)));
    }

    [TestMethod]
    public void QueryRowField_ShouldRejectNonVisibleType()
    {
        Assert.IsFalse(QueryRowField.IsSupportedFieldType(typeof(PrivateFieldType)));
    }

    [TestMethod]
    public void SourceDescriptor_ShouldDefaultToLegacyTransfer()
    {
        var descriptor = SourceDescriptor.Empty(SourceIdentity.Empty);

        Assert.AreEqual(SourceTransferCapabilities.None, descriptor.TransferCapabilities);
    }

    [TestMethod]
    public void GenericMaterializer_ShouldAcceptRefStructReaderWithoutInterfaceStorage()
    {
        var reader = new TestReader(42);
        var value = TestMaterializer.Materialize(ref reader);

        Assert.AreEqual(42, value);
    }

    private ref struct TestReader(int value) : IQuerySourceFieldReader
    {
        public T Read<T>(int slot)
        {
            Assert.AreEqual(0, slot);
            return (T)(object)value;
        }
    }

    private readonly struct TestMaterializer : IQueryRowMaterializer<int>
    {
        public static int Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct => reader.Read<int>(0);
    }

    private sealed class PrivateFieldType;
}
