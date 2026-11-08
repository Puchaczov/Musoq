using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CountDistinctAllOverloadsTests : BasicEntityTestBase
{
    [TestMethod]
    public void CountDistinct_AllSupportedOverloads_ShouldBindAndCountUngroupedValues()
    {
        const string query = """
            select
                Count(distinct StringValue) as StringCount,
                Count(distinct DecimalValue) as DecimalCount,
                Count(distinct DateTimeOffsetValue) as DateTimeOffsetCount,
                Count(distinct DateTimeValue) as DateTimeCount,
                Count(distinct ByteValue) as ByteCount,
                Count(distinct SByteValue) as SByteCount,
                Count(distinct ShortValue) as ShortCount,
                Count(distinct UShortValue) as UShortCount,
                Count(distinct IntValue) as IntCount,
                Count(distinct UIntValue) as UIntCount,
                Count(distinct LongValue) as LongCount,
                Count(distinct ULongValue) as ULongCount,
                Count(distinct FloatValue) as FloatCount,
                Count(distinct DoubleValue) as DoubleCount,
                Count(distinct BoolValue) as BoolCount
            from #A.entities()
            """;

        var table = Run(query, CreateRows());

        AssertColumns(table, false, "StringCount", "DecimalCount", "DateTimeOffsetCount", "DateTimeCount", "ByteCount", "SByteCount", "ShortCount", "UShortCount", "IntCount", "UIntCount", "LongCount", "ULongCount", "FloatCount", "DoubleCount", "BoolCount");
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            [2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L]);
    }

    [TestMethod]
    public void CountDistinct_AllSupportedOverloads_ShouldCountWithinEachGroup()
    {
        const string query = """
            select
                Bucket,
                Count(distinct StringValue) as StringCount,
                Count(distinct DecimalValue) as DecimalCount,
                Count(distinct DateTimeOffsetValue) as DateTimeOffsetCount,
                Count(distinct DateTimeValue) as DateTimeCount,
                Count(distinct ByteValue) as ByteCount,
                Count(distinct SByteValue) as SByteCount,
                Count(distinct ShortValue) as ShortCount,
                Count(distinct UShortValue) as UShortCount,
                Count(distinct IntValue) as IntCount,
                Count(distinct UIntValue) as UIntCount,
                Count(distinct LongValue) as LongCount,
                Count(distinct ULongValue) as ULongCount,
                Count(distinct FloatValue) as FloatCount,
                Count(distinct DoubleValue) as DoubleCount,
                Count(distinct BoolValue) as BoolCount
            from #A.entities()
            group by Bucket
            """;

        var table = Run(query, CreateRows());

        AssertColumns(table, true, "Bucket", "StringCount", "DecimalCount", "DateTimeOffsetCount", "DateTimeCount", "ByteCount", "SByteCount", "ShortCount", "UShortCount", "IntCount", "UIntCount", "LongCount", "ULongCount", "FloatCount", "DoubleCount", "BoolCount");
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["A", 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L, 2L],
            ["B", 1L, 1L, 1L, 1L, 1L, 1L, 1L, 1L, 1L, 1L, 1L, 1L, 1L, 1L, 1L]);
    }

    private Table Run(string query, IEnumerable<CountDistinctEntity> rows)
    {
        var rowList = rows as IReadOnlyList<CountDistinctEntity> ?? new List<CountDistinctEntity>(rows);
        var rowSource = new EntitySource<CountDistinctEntity>(
            [rowList],
            GenericEntityTable<CountDistinctEntity>.NameToIndexMap,
            GenericEntityTable<CountDistinctEntity>.IndexToObjectAccessMap);
        var schema = new GenericSchema<GenericLibrary>(
            new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
            {
                { "entities", (new GenericEntityTable<CountDistinctEntity>(), rowSource) }
            });
        var schemaProvider = new GenericSchemaProvider(new Dictionary<string, ISchema>
        {
            { "#A", schema }
        });

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
        return vm.Run(CancellationToken.None);
    }

    private static CountDistinctEntity[] CreateRows()
    {
        var firstDateTimeOffset = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var secondDateTimeOffset = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var firstDateTime = new DateTime(2024, 1, 1);
        var secondDateTime = new DateTime(2024, 1, 2);

        return
        [
            Create("A", "alpha", 1m, firstDateTimeOffset, firstDateTime, 1, 1, 1, 1, 1, 1, 1, 1, 1f, 1d, true),
            Create("A", "alpha", 1m, firstDateTimeOffset, firstDateTime, 1, 1, 1, 1, 1, 1, 1, 1, 1f, 1d, true),
            Create("A", "beta", 2m, secondDateTimeOffset, secondDateTime, 2, 2, 2, 2, 2, 2, 2, 2, 2f, 2d, false),
            Create("A", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null),
            Create("B", "alpha", 1m, firstDateTimeOffset, firstDateTime, 1, 1, 1, 1, 1, 1, 1, 1, 1f, 1d, true),
            Create("B", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null)
        ];
    }

    private static CountDistinctEntity Create(
        string bucket,
        string? stringValue,
        decimal? decimalValue,
        DateTimeOffset? dateTimeOffsetValue,
        DateTime? dateTimeValue,
        byte? byteValue,
        sbyte? sByteValue,
        short? shortValue,
        ushort? uShortValue,
        int? intValue,
        uint? uIntValue,
        long? longValue,
        ulong? uLongValue,
        float? floatValue,
        double? doubleValue,
        bool? boolValue)
    {
        return new CountDistinctEntity
        {
            Bucket = bucket,
            StringValue = stringValue,
            DecimalValue = decimalValue,
            DateTimeOffsetValue = dateTimeOffsetValue,
            DateTimeValue = dateTimeValue,
            ByteValue = byteValue,
            SByteValue = sByteValue,
            ShortValue = shortValue,
            UShortValue = uShortValue,
            IntValue = intValue,
            UIntValue = uIntValue,
            LongValue = longValue,
            ULongValue = uLongValue,
            FloatValue = floatValue,
            DoubleValue = doubleValue,
            BoolValue = boolValue
        };
    }

    private static void AssertColumns(Table table, bool firstColumnIsGroup, params string[] names)
    {
        var columns = new List<Column>(table.Columns);
        Assert.HasCount(names.Length, columns);
        for (var index = 0; index < names.Length; index++)
        {
            Assert.AreEqual(names[index], columns[index].ColumnName);
            var expectedType = firstColumnIsGroup && index == 0 ? typeof(string) : typeof(long);
            Assert.AreEqual(expectedType, columns[index].ColumnType, names[index]);
        }
    }

    public sealed class CountDistinctEntity
    {
        public string Bucket { get; init; } = string.Empty;
        public string? StringValue { get; init; }
        public decimal? DecimalValue { get; init; }
        public DateTimeOffset? DateTimeOffsetValue { get; init; }
        public DateTime? DateTimeValue { get; init; }
        public byte? ByteValue { get; init; }
        public sbyte? SByteValue { get; init; }
        public short? ShortValue { get; init; }
        public ushort? UShortValue { get; init; }
        public int? IntValue { get; init; }
        public uint? UIntValue { get; init; }
        public long? LongValue { get; init; }
        public ulong? ULongValue { get; init; }
        public float? FloatValue { get; init; }
        public double? DoubleValue { get; init; }
        public bool? BoolValue { get; init; }
    }
}
