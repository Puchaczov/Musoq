using System;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class SpecExplorationTableCoupleTests
{
    [TestMethod]
    [FeatureEvidence("table-couple-type-matrix", FeatureEvidenceKind.RuntimePositive)]
    public void Spec_TableCouple_AllSupportedTypes_ShouldPreserveSchemaValuesAndNulls()
    {
        const string query =
            "table AllTypes {" +
            " ByteCol: byte, SByteCol: sbyte, ShortCol: short, IntCol: int, LongCol: long," +
            " UShortCol: ushort, UIntCol: uint, ULongCol: ulong, FloatCol: float, DoubleCol: double," +
            " DecimalCol: decimal, MoneyCol: money, BoolCol: bool, BooleanCol: boolean, BitCol: bit," +
            " CharCol: char, StringCol: string, DateTimeCol: datetime," +
            " DateTimeOffsetCol: datetimeoffset, TimeSpanCol: timespan, GuidCol: guid, ObjectCol: object" +
            " };" +
            "couple #test.whatever with table AllTypes as Source;" +
            "select ByteCol, SByteCol, ShortCol, IntCol, LongCol, UShortCol, UIntCol, ULongCol," +
            " FloatCol, DoubleCol, DecimalCol, MoneyCol, BoolCol, BooleanCol, BitCol, CharCol," +
            " StringCol, DateTimeCol, DateTimeOffsetCol, TimeSpanCol, GuidCol, ObjectCol from Source()";

        var identifier = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var dateTime = new DateTime(2020, 1, 2, 3, 4, 5);
        var dateTimeOffset = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(1));
        var duration = new TimeSpan(1, 2, 3);

        dynamic populated = new ExpandoObject();
        populated.ByteCol = (byte)1;
        populated.SByteCol = (sbyte)-2;
        populated.ShortCol = (short)-3;
        populated.IntCol = 4;
        populated.LongCol = 5L;
        populated.UShortCol = (ushort)6;
        populated.UIntCol = 7U;
        populated.ULongCol = 8UL;
        populated.FloatCol = 1.5f;
        populated.DoubleCol = 2.5d;
        populated.DecimalCol = 3.5m;
        populated.MoneyCol = 4.5m;
        populated.BoolCol = true;
        populated.BooleanCol = false;
        populated.BitCol = true;
        populated.CharCol = 'Z';
        populated.StringCol = "text";
        populated.DateTimeCol = dateTime;
        populated.DateTimeOffsetCol = dateTimeOffset;
        populated.TimeSpanCol = duration;
        populated.GuidCol = identifier;
        populated.ObjectCol = "object-value";

        dynamic nulls = new ExpandoObject();
        nulls.ByteCol = null;
        nulls.SByteCol = null;
        nulls.ShortCol = null;
        nulls.IntCol = null;
        nulls.LongCol = null;
        nulls.UShortCol = null;
        nulls.UIntCol = null;
        nulls.ULongCol = null;
        nulls.FloatCol = null;
        nulls.DoubleCol = null;
        nulls.DecimalCol = null;
        nulls.MoneyCol = null;
        nulls.BoolCol = null;
        nulls.BooleanCol = null;
        nulls.BitCol = null;
        nulls.CharCol = null;
        nulls.StringCol = null;
        nulls.DateTimeCol = null;
        nulls.DateTimeOffsetCol = null;
        nulls.TimeSpanCol = null;
        nulls.GuidCol = null;
        nulls.ObjectCol = null;

        var vm = CreateAndRunVirtualMachine(query, [populated, populated, nulls]);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("ByteCol", typeof(byte?)),
            ("SByteCol", typeof(sbyte?)),
            ("ShortCol", typeof(short?)),
            ("IntCol", typeof(int?)),
            ("LongCol", typeof(long?)),
            ("UShortCol", typeof(ushort?)),
            ("UIntCol", typeof(uint?)),
            ("ULongCol", typeof(ulong?)),
            ("FloatCol", typeof(float?)),
            ("DoubleCol", typeof(double?)),
            ("DecimalCol", typeof(decimal?)),
            ("MoneyCol", typeof(decimal?)),
            ("BoolCol", typeof(bool?)),
            ("BooleanCol", typeof(bool?)),
            ("BitCol", typeof(bool?)),
            ("CharCol", typeof(char?)),
            ("StringCol", typeof(string)),
            ("DateTimeCol", typeof(DateTime?)),
            ("DateTimeOffsetCol", typeof(DateTimeOffset?)),
            ("TimeSpanCol", typeof(TimeSpan?)),
            ("GuidCol", typeof(Guid?)),
            ("ObjectCol", typeof(object)));

        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [
                (byte)1, (sbyte)-2, (short)-3, 4, 5L, (ushort)6, 7U, 8UL,
                1.5f, 2.5d, 3.5m, 4.5m, true, false, true, 'Z', "text",
                dateTime, dateTimeOffset, duration, identifier, "object-value"
            ],
            [
                (byte)1, (sbyte)-2, (short)-3, 4, 5L, (ushort)6, 7U, 8UL,
                1.5f, 2.5d, 3.5m, 4.5m, true, false, true, 'Z', "text",
                dateTime, dateTimeOffset, duration, identifier, "object-value"
            ],
            [
                null, null, null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, null
            ]);
    }

    [TestMethod]
    public void Spec_TableCouple_FullyQualifiedAndAliasTypes_ShouldPreserveExactTypes()
    {
        const string query =
            "table TypedAliases {" +
            " Amount: MONEY, FlagA: BOOLEAN, FlagB: BIT," +
            " Id: System.Int32, Stamp: System.DateTimeOffset, Label: System.String" +
            " };" +
            "couple #test.whatever with table TypedAliases as Source;" +
            "select * from Source()";

        var stamp = new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero);
        dynamic item = new ExpandoObject();
        item.Amount = 12.5m;
        item.FlagA = true;
        item.FlagB = false;
        item.Id = 42;
        item.Stamp = stamp;
        item.Label = "qualified";

        var vm = CreateAndRunVirtualMachine(query, [item]);
        var table = vm.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Amount", typeof(decimal?)),
            ("FlagA", typeof(bool?)),
            ("FlagB", typeof(bool?)),
            ("Id", typeof(int?)),
            ("Stamp", typeof(DateTimeOffset?)),
            ("Label", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [12.5m, true, false, 42, stamp, "qualified"]);
    }
}
