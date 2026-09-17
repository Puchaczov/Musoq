using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class EnumScalarContractTests
{
    [TestMethod]
    public void ScalarValue_ShouldRoundTripEverySupportedBackingType()
    {
        Assert.AreEqual(byte.MaxValue, EnumScalarValue.FromByte(byte.MaxValue).AsByte());
        Assert.AreEqual(sbyte.MinValue, EnumScalarValue.FromSByte(sbyte.MinValue).AsSByte());
        Assert.AreEqual(short.MinValue, EnumScalarValue.FromInt16(short.MinValue).AsInt16());
        Assert.AreEqual(ushort.MaxValue, EnumScalarValue.FromUInt16(ushort.MaxValue).AsUInt16());
        Assert.AreEqual(int.MinValue, EnumScalarValue.FromInt32(int.MinValue).AsInt32());
        Assert.AreEqual(uint.MaxValue, EnumScalarValue.FromUInt32(uint.MaxValue).AsUInt32());
        Assert.AreEqual(long.MinValue, EnumScalarValue.FromInt64(long.MinValue).AsInt64());
        Assert.AreEqual(ulong.MaxValue, EnumScalarValue.FromUInt64(ulong.MaxValue).AsUInt64());
    }

    [TestMethod]
    public void ScalarValue_FromRawShouldPreserveSignedCarrierBits()
    {
        Assert.AreEqual((sbyte)-1, EnumScalarValue.FromRaw(EnumUnderlyingKind.SByte, byte.MaxValue).AsSByte());
        Assert.AreEqual((short)-1, EnumScalarValue.FromRaw(EnumUnderlyingKind.Int16, ushort.MaxValue).AsInt16());
        Assert.AreEqual(-1, EnumScalarValue.FromRaw(EnumUnderlyingKind.Int32, uint.MaxValue).AsInt32());
        Assert.AreEqual(-1L, EnumScalarValue.FromRaw(EnumUnderlyingKind.Int64, ulong.MaxValue).AsInt64());
    }

    [TestMethod]
    public void ScalarValue_FromRawShouldRejectBitsOutsideCarrierWidth()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EnumScalarValue.FromRaw(EnumUnderlyingKind.Byte, byte.MaxValue + 1UL));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EnumScalarValue.FromRaw(EnumUnderlyingKind.Int16, ushort.MaxValue + 1UL));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EnumScalarValue.FromRaw(EnumUnderlyingKind.UInt32, (ulong)uint.MaxValue + 1UL));
    }

    [TestMethod]
    public void Descriptor_ShouldPreserveDeclarationOrderAliasesAndUnknownValues()
    {
        var descriptor = CreateStatusDescriptor();

        CollectionAssert.AreEqual(
            new[] { "Queued", "Waiting", "Running" },
            descriptor.Members.Select(static member => member.Name).ToArray());
        Assert.AreEqual("Queued", descriptor.Aliases["Waiting"]);
        Assert.IsTrue(descriptor.TryGetValue("Waiting", out var aliasValue));
        Assert.AreEqual(10, aliasValue.AsInt32());
        Assert.IsFalse(descriptor.TryGetValue("waiting", out _));
        Assert.IsTrue(descriptor.TryGetCanonicalName(aliasValue, out var canonicalName));
        Assert.AreEqual("Queued", canonicalName);

        var unknown = EnumScalarValue.FromInt32(999);
        Assert.IsFalse(descriptor.IsDefined(unknown));
        Assert.IsFalse(descriptor.TryGetCanonicalName(unknown, out _));
    }

    [TestMethod]
    public void Descriptor_FingerprintShouldBeDeterministicAndIdentitySensitive()
    {
        var first = CreateStatusDescriptor();
        var equivalent = CreateStatusDescriptor();
        var renamed = new EnumTypeDescriptor(
            "OtherStatus",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.Int32,
            false,
            first.Members);
        var reordered = new EnumTypeDescriptor(
            first.DisplayName,
            first.Origin,
            first.UnderlyingKind,
            first.IsFlags,
            first.Members.Reverse().ToArray());

        Assert.AreEqual(first.Fingerprint, equivalent.Fingerprint);
        Assert.AreEqual(first, equivalent);
        Assert.AreNotEqual(first.Fingerprint, renamed.Fingerprint);
        Assert.AreNotEqual(first.Fingerprint, reordered.Fingerprint);
        Assert.AreEqual(64, first.Fingerprint.Length);
    }

    [TestMethod]
    public void Descriptor_ShouldRejectCaseOnlyMemberDuplicates()
    {
        var members = new[]
        {
            new EnumMemberDescriptor("Ready", EnumScalarValue.FromByte(1)),
            new EnumMemberDescriptor("ready", EnumScalarValue.FromByte(2))
        };

        Assert.Throws<ArgumentException>(() => new EnumTypeDescriptor(
            "State",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.Byte,
            false,
            members));
    }

    [TestMethod]
    public void Descriptor_PublicSurfaceShouldNotExposeClrType()
    {
        var typeProperties = typeof(EnumTypeDescriptor)
            .GetProperties()
            .Where(static property => property.PropertyType == typeof(Type))
            .Select(static property => property.Name)
            .ToArray();

        Assert.IsEmpty(typeProperties);
    }

    [TestMethod]
    public void NativeDescriptor_ShouldCaptureFlagsAliasesAndExactClrIdentity()
    {
        var descriptor = EnumTypeDescriptor.FromClrEnum(typeof(NativeAccess));

        Assert.AreEqual(typeof(NativeAccess).FullName, descriptor.DisplayName);
        Assert.AreEqual(EnumTypeOrigin.NativeClr, descriptor.Origin);
        Assert.AreEqual(EnumUnderlyingKind.UInt32, descriptor.UnderlyingKind);
        Assert.IsTrue(descriptor.IsFlags);
        Assert.AreEqual("Read", descriptor.Aliases["Open"]);
    }

    [TestMethod]
    public void NativeDescriptor_ShouldRejectCaseOnlyClrMemberDuplicates()
    {
        Assert.Throws<ArgumentException>(() => EnumTypeDescriptor.FromClrEnum(typeof(CaseCollision)));
    }

    private static EnumTypeDescriptor CreateStatusDescriptor()
    {
        return new EnumTypeDescriptor(
            "JobStatus",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.Int32,
            false,
            [
                new EnumMemberDescriptor("Queued", EnumScalarValue.FromInt32(10)),
                new EnumMemberDescriptor("Waiting", EnumScalarValue.FromInt32(10)),
                new EnumMemberDescriptor("Running", EnumScalarValue.FromInt32(20))
            ]);
    }

    [Flags]
    public enum NativeAccess : uint
    {
        None = 0,
        Read = 1,
        Open = Read,
        Write = 2
    }

    public enum CaseCollision : byte
    {
        Ready = 1,
        ready = 2
    }
}
