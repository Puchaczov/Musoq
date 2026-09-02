using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class EnumColumnContractTests
{
    [TestMethod]
    public void SchemaColumn_NativeEnumShouldSupportEveryClrBackingType()
    {
        (Type EnumType, Type CarrierType, EnumUnderlyingKind Kind)[] cases =
        [
            (typeof(ByteStatus), typeof(byte), EnumUnderlyingKind.Byte),
            (typeof(SByteStatus), typeof(sbyte), EnumUnderlyingKind.SByte),
            (typeof(Int16Status), typeof(short), EnumUnderlyingKind.Int16),
            (typeof(UInt16Status), typeof(ushort), EnumUnderlyingKind.UInt16),
            (typeof(Int32Status), typeof(int), EnumUnderlyingKind.Int32),
            (typeof(UInt32Status), typeof(uint), EnumUnderlyingKind.UInt32),
            (typeof(Int64Status), typeof(long), EnumUnderlyingKind.Int64),
            (typeof(UInt64Status), typeof(ulong), EnumUnderlyingKind.UInt64)
        ];

        foreach (var item in cases)
        {
            var column = new SchemaColumn("Status", 0, item.EnumType);

            Assert.AreEqual(item.CarrierType, column.ColumnType);
            Assert.AreEqual(item.EnumType, column.SourceReadType);
            Assert.AreEqual(item.Kind, column.EnumType!.UnderlyingKind);
        }
    }

    [TestMethod]
    public void SchemaColumn_NativeEnumShouldNormalizeToPrimitiveCarrier()
    {
        var column = new SchemaColumn("Status", 0, typeof(NativeStatus));

        Assert.AreEqual(typeof(short), column.ColumnType);
        Assert.AreEqual(typeof(NativeStatus), column.SourceReadType);
        Assert.IsNotNull(column.EnumType);
        Assert.AreEqual(EnumUnderlyingKind.Int16, column.EnumType.UnderlyingKind);
        Assert.AreEqual(typeof(NativeStatus).FullName, column.EnumType.DisplayName);
    }

    [TestMethod]
    public void SchemaColumn_NullableNativeEnumShouldNormalizeToNullableCarrier()
    {
        var column = new SchemaColumn("Status", 0, typeof(NativeStatus?));

        Assert.AreEqual(typeof(short?), column.ColumnType);
        Assert.AreEqual(typeof(NativeStatus?), column.SourceReadType);
        Assert.IsNotNull(column.EnumType);
    }

    [TestMethod]
    public void SchemaColumn_QueryLocalEnumShouldUsePrimitiveSourceReadType()
    {
        var descriptor = CreateQueryLocalDescriptor();
        var column = new SchemaColumn("Status", 0, typeof(short), typeof(short), descriptor);

        Assert.AreEqual(typeof(short), column.ColumnType);
        Assert.AreEqual(typeof(short), column.SourceReadType);
        Assert.AreSame(descriptor, column.EnumType);
    }

    [TestMethod]
    public void SchemaColumn_OrdinaryColumnShouldRejectDifferentSourceReadType()
    {
        Assert.Throws<ArgumentException>(() =>
            new SchemaColumn("Value", 0, typeof(int), typeof(long), null));
    }

    [TestMethod]
    public void SchemaColumn_NativeSourceShouldRejectDescriptorFromDifferentClrEnum()
    {
        var wrongDescriptor = EnumTypeDescriptor.FromClrEnum(typeof(OtherStatus));

        Assert.Throws<ArgumentException>(() =>
            new SchemaColumn("Status", 0, typeof(short), typeof(NativeStatus), wrongDescriptor));
    }

    [TestMethod]
    public void SchemaColumn_NativeEnumShouldRejectIntendedGeneratedTypeName()
    {
        Assert.Throws<ArgumentException>(() =>
            new SchemaColumn("Status", 0, typeof(NativeStatus), "Generated.Status"));
    }

    [TestMethod]
    public void QueryRowField_ShouldCarryEnumLogicalAndSourceMetadata()
    {
        var descriptor = EnumTypeDescriptor.FromClrEnum(typeof(NativeStatus));
        var field = new QueryRowField(
            0,
            4,
            "Status",
            typeof(short),
            typeof(NativeStatus),
            descriptor,
            false,
            null,
            ColumnStability.Stable);

        Assert.AreEqual(typeof(short), field.FieldType);
        Assert.AreEqual(typeof(NativeStatus), field.SourceReadType);
        Assert.AreSame(descriptor, field.EnumType);
    }

    [TestMethod]
    public void QueryRowShape_FingerprintShouldIncludeEnumIdentityAndSourceReadType()
    {
        var descriptor = EnumTypeDescriptor.FromClrEnum(typeof(NativeStatus));
        var nativeShape = CreateShape(typeof(NativeStatus), descriptor);
        var carrierShape = CreateShape(typeof(short), descriptor);
        var otherIdentityShape = CreateShape(typeof(short), CreateQueryLocalDescriptor());

        Assert.AreNotEqual(nativeShape.Fingerprint, carrierShape.Fingerprint);
        Assert.AreNotEqual(carrierShape.Fingerprint, otherIdentityShape.Fingerprint);
    }

    [TestMethod]
    public void LogicalScalarReads_ShouldHaveStableIndependentCapabilityBit()
    {
        var logicalScalarReads = Enum.Parse<SourceTransferCapabilities>(nameof(SourceTransferCapabilities.LogicalScalarReads));
        var queryScopedRows = Enum.Parse<SourceTransferCapabilities>(nameof(SourceTransferCapabilities.QueryScopedRows));

        Assert.AreEqual((SourceTransferCapabilities)2, logicalScalarReads);
        Assert.AreEqual(
            (SourceTransferCapabilities)3,
            queryScopedRows | logicalScalarReads);
    }

    [TestMethod]
    public void RuntimeContract_ShouldAdvertiseBreakingSchemaGeneration()
    {
        Assert.AreEqual("2", ReadRuntimeContractConstant(nameof(RuntimeV2Contract.RuntimeContractVersion)));
        Assert.AreEqual("2", ReadRuntimeContractConstant(nameof(RuntimeV2Contract.SchemaContractVersion)));
        Assert.AreEqual(
            "runtime-v2=2;schema=2;parameters=1",
            ReadRuntimeContractConstant(nameof(RuntimeV2Contract.ContractSignature)));
    }

    private static QueryRowShape CreateShape(Type sourceReadType, EnumTypeDescriptor descriptor)
    {
        return new QueryRowShape([
            new QueryRowField(
                0,
                0,
                "Status",
                typeof(short),
                sourceReadType,
                descriptor,
                false,
                null,
                ColumnStability.Stable)
        ]);
    }

    private static EnumTypeDescriptor CreateQueryLocalDescriptor()
    {
        return new EnumTypeDescriptor(
            "JobStatus",
            EnumTypeOrigin.QueryLocal,
            EnumUnderlyingKind.Int16,
            false,
            [new EnumMemberDescriptor("Queued", EnumScalarValue.FromInt16(10))]);
    }

    private static string ReadRuntimeContractConstant(string name)
    {
        return (string)(typeof(RuntimeV2Contract).GetField(name)!.GetRawConstantValue() ?? string.Empty);
    }

    public enum NativeStatus : short
    {
        Queued = 10,
        Running = 20
    }

    public enum OtherStatus : short
    {
        Queued = 10,
        Running = 20
    }

    public enum ByteStatus : byte { Value = 1 }

    public enum SByteStatus : sbyte { Value = -1 }

    public enum Int16Status : short { Value = -1 }

    public enum UInt16Status : ushort { Value = 1 }

    public enum Int32Status : int { Value = -1 }

    public enum UInt32Status : uint { Value = 1 }

    public enum Int64Status : long { Value = -1 }

    public enum UInt64Status : ulong { Value = 1 }
}
