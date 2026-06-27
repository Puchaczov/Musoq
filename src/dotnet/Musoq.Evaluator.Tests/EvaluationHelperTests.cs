using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class EvaluationHelperTests
{
    [TestMethod]
    public void SelectTopOffsetRows_WhenKeysTie_ShouldPreserveOriginalRowOrder()
    {
        var low = new TestRow([0, "low"]);
        var firstTie = new TestRow([1, "first"]);
        var secondTie = new TestRow([1, "second"]);
        var high = new TestRow([2, "high"]);
        Row[] rows = [firstTie, high, secondTie, low];

        var selected = EvaluationHelper.SelectTopOffsetRows(
            rows,
            1,
            2,
            [new RowOrderKey(static row => (int)row[0], Descending: false)]);

        CollectionAssert.AreEqual(new Row[] { firstTie, secondTie }, selected.ToArray());
    }

    [TestMethod]
    public void AppendTopOffsetRowsDirect_WhenKeysTie_ShouldAppendRowsInDeterministicOrder()
    {
        var low = new TestRow([0, "low"]);
        var firstTie = new TestRow([1, "first"]);
        var secondTie = new TestRow([1, "second"]);
        var high = new TestRow([2, "high"]);
        Row[] rows = [firstTie, high, secondTie, low];
        var target = new Table(
            "result",
            [new Column("Score", typeof(int), 0), new Column("Name", typeof(string), 1)]);

        EvaluationHelper.AppendTopOffsetRowsDirect(
            rows,
            target,
            1,
            2,
            [new RowOrderKey(static row => (int)row[0], Descending: false)]);

        CollectionAssert.AreEqual(new Row[] { firstTie, secondTie }, target.Rows.ToArray());
    }

    [TestMethod]
    public void OrderRows_WithExplicitNullOrdering_ShouldPlaceNullsBeforeDescendingValues()
    {
        var nullRow = new TestRow([(string)null!, "null"]);
        var low = new TestRow(["A", "low"]);
        var high = new TestRow(["B", "high"]);
        Row[] rows = [low, nullRow, high];

        var ordered = EvaluationHelper.OrderRows(
            rows,
            [new RowOrderKey(static row => (string?)row[0]!, Descending: true, NullOrdering: 1)])
            .ToArray();

        CollectionAssert.AreEqual(new Row[] { nullRow, high, low }, ordered);
    }

    [TestMethod]
    public void EntitySource_Chunks_ShouldReturnRowChunkOverOriginalListBackedInput()
    {
        var entities = new[]
        {
            new TypedRowSourceEntity(1),
            new TypedRowSourceEntity(2)
        };
        var source = new EntitySource<TypedRowSourceEntity>(
            [entities],
            new Dictionary<string, int> { [nameof(TypedRowSourceEntity.Id)] = 0 },
            new Dictionary<int, Func<TypedRowSourceEntity, object?>>
            {
                [0] = static entity => entity.Id
            });

        var chunk = (RowChunk<TypedRowSourceEntity>)source.Chunks.Single();

        Assert.AreSame(entities, chunk.Source);
        Assert.AreEqual(0, chunk.Offset);
        Assert.AreEqual(2, chunk.Count);
    }

    [TestMethod]
    public void ConvertEnumerableOutputToChunks_WhenArrayIsLarge_ShouldSplitWithoutCopying()
    {
        var rows = Enumerable.Range(0, EvaluationHelper.DefaultSourceChunkSize + 1).ToArray();

        var chunks = EvaluationHelper.ConvertEnumerableOutputToChunks(rows).ToArray();
        rows[0] = -1;

        Assert.HasCount(2, chunks);
        Assert.IsInstanceOfType<RowChunk<int>>(chunks[0]);
        Assert.IsInstanceOfType<RowChunk<int>>(chunks[1]);
        Assert.AreSame(rows, ((RowChunk<int>)chunks[0]).Source);
        Assert.AreSame(rows, ((RowChunk<int>)chunks[1]).Source);
        Assert.AreEqual(EvaluationHelper.DefaultSourceChunkSize, chunks[0].Count);
        Assert.AreEqual(1, chunks[1].Count);
        Assert.AreEqual(-1, chunks[0][0]);
    }

    [TestMethod]
    public void ConvertEnumerableOutputToChunks_WhenEnumerableIsStreaming_ShouldBufferIntoArrayChunks()
    {
        var rows = Enumerable.Range(0, EvaluationHelper.DefaultSourceChunkSize + 1)
            .Where(static _ => true);

        var chunks = EvaluationHelper.ConvertEnumerableOutputToChunks(rows).ToArray();

        Assert.HasCount(2, chunks);
        Assert.IsTrue(chunks[0].GetType().IsArray);
        Assert.IsTrue(chunks[1].GetType().IsArray);
        Assert.AreEqual(EvaluationHelper.DefaultSourceChunkSize, chunks[0].Count);
        Assert.AreEqual(1, chunks[1].Count);
    }

    [TestMethod]
    public void ConvertEnumerableOutputToChunks_WhenReadOnlyListIsUnknown_ShouldBufferIntoArrayChunks()
    {
        var rows = new UnknownReadOnlyList<int>(Enumerable.Range(0, EvaluationHelper.DefaultSourceChunkSize + 1).ToArray());

        var chunks = EvaluationHelper.ConvertEnumerableOutputToChunks(rows).ToArray();

        Assert.HasCount(2, chunks);
        Assert.IsTrue(chunks[0].GetType().IsArray);
        Assert.IsTrue(chunks[1].GetType().IsArray);
        Assert.AreEqual(EvaluationHelper.DefaultSourceChunkSize, chunks[0].Count);
        Assert.AreEqual(1, chunks[1].Count);
    }

    [TestMethod]
    public void GetRowSourceChunks_WhenTypedChunkIsReflected_ShouldUseLazyObjectChunkView()
    {
        var entities = new[]
        {
            new TypedRowSourceEntity(1),
            new TypedRowSourceEntity(2)
        };
        var source = new EntitySource<TypedRowSourceEntity>(
            [entities],
            new Dictionary<string, int> { [nameof(TypedRowSourceEntity.Id)] = 0 },
            new Dictionary<int, Func<TypedRowSourceEntity, object?>>
            {
                [0] = static entity => entity.Id
            });
        var schema = new ReflectedChunkSchema(source);
        var executionContext = new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            [],
            new Dictionary<string, string>(),
            NullLogger.Instance);

        var chunks = EvaluationHelper.GetRowSourceChunks(
            schema,
            typeof(TypedRowSourceEntity),
            "items",
            executionContext,
            []).ToArray();

        Assert.HasCount(1, chunks);
        Assert.IsNotInstanceOfType(chunks[0], typeof(List<object>));
        Assert.AreSame(entities[0], chunks[0][0]);
        Assert.AreSame(entities[1], chunks[0][1]);
    }

    [TestMethod]
    [DataRow(BinaryOpKind.GreaterOrEqual, 50, "Exact50")]
    [DataRow(BinaryOpKind.GreaterThan, 50, "Low10")]
    [DataRow(BinaryOpKind.LessOrEqual, 50, "Exact50")]
    [DataRow(BinaryOpKind.LessThan, 50, "High90")]
    public void CreateAsOfIndex_WhenProbed_ShouldReturnClosestMatchingCandidate(
        BinaryOpKind comparisonKind,
        int probe,
        string expectedName)
    {
        var rows = new[]
        {
            new AsOfIndexTestRow("High90", "A", 90),
            new AsOfIndexTestRow("Low10", "A", 10),
            new AsOfIndexTestRow("Exact50", "A", 50),
            new AsOfIndexTestRow("Exact50Later", "A", 50)
        };
        var index = EvaluationHelper.CreateAsOfIndex(
            rows,
            null,
            static row => row.Score,
            comparisonKind);

        var match = index.Find(null, probe);

        Assert.IsNotNull(match);
        Assert.AreEqual(expectedName, match.Name);
    }

    [TestMethod]
    public void CreateAsOfIndex_WhenEqualityKeyIsProvided_ShouldPartitionCandidates()
    {
        var rows = new[]
        {
            new AsOfIndexTestRow("A90", "A", 90),
            new AsOfIndexTestRow("B80", "B", 80),
            new AsOfIndexTestRow("A20", "A", 20),
            new AsOfIndexTestRow("NoTeam", null, 99)
        };
        var index = EvaluationHelper.CreateAsOfIndex(
            rows,
            static row => row.Team,
            static row => row.Score,
            BinaryOpKind.GreaterOrEqual);

        Assert.AreEqual("A90", index.Find("A", 100)?.Name);
        Assert.AreEqual("B80", index.Find("B", 100)?.Name);
        Assert.IsNull(index.Find(null, 100));
        Assert.IsNull(index.Find("C", 100));
    }

    [TestMethod]
    public void CreateAsOfEqualityKey_WhenAnyPartIsNull_ShouldReturnNull()
    {
        Assert.IsNull(EvaluationHelper.CreateAsOfEqualityKey("A", null, 1));
        Assert.IsNotNull(EvaluationHelper.CreateAsOfEqualityKey("A", "B", 1));
    }

    [TestMethod]
    public void CreateComplexTypeDescriptionArrayTest()
    {
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("Test", typeof(TestClass)).ToArray();


        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test" && pair.Type == typeof(TestClass)));


        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.Test" && pair.Type == typeof(TestClass)));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Test.SubClass" && pair.Type == typeof(TestSubClass)));


        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SomeInt" && pair.Type == typeof(int)));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Test.SomeString" && pair.Type == typeof(string)));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Test.SomeObject" && pair.Type == typeof(object)));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Test.SubClass.SomeInt" && pair.Type == typeof(int)));


        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName.StartsWith("Test.SomeInt.")));
        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName.StartsWith("Test.SomeString.")));
    }

    [TestMethod]
    public void RemapPrimitiveTypesTest()
    {
        Assert.AreEqual("System.Byte", EvaluationHelper.RemapPrimitiveTypes("byte"));
        Assert.AreEqual("System.SByte", EvaluationHelper.RemapPrimitiveTypes("sbyte"));

        Assert.AreEqual("System.Int16", EvaluationHelper.RemapPrimitiveTypes("short"));
        Assert.AreEqual("System.Int32", EvaluationHelper.RemapPrimitiveTypes("int"));
        Assert.AreEqual("System.Int64", EvaluationHelper.RemapPrimitiveTypes("long"));

        Assert.AreEqual("System.UInt16", EvaluationHelper.RemapPrimitiveTypes("ushort"));
        Assert.AreEqual("System.UInt32", EvaluationHelper.RemapPrimitiveTypes("uint"));
        Assert.AreEqual("System.UInt64", EvaluationHelper.RemapPrimitiveTypes("ulong"));

        Assert.AreEqual("System.String", EvaluationHelper.RemapPrimitiveTypes("string"));

        Assert.AreEqual("System.Char", EvaluationHelper.RemapPrimitiveTypes("char"));

        Assert.AreEqual("System.Boolean", EvaluationHelper.RemapPrimitiveTypes("bool"));
        Assert.AreEqual("System.Boolean", EvaluationHelper.RemapPrimitiveTypes("boolean"));
        Assert.AreEqual("System.Boolean", EvaluationHelper.RemapPrimitiveTypes("bit"));

        Assert.AreEqual("System.Single", EvaluationHelper.RemapPrimitiveTypes("float"));
        Assert.AreEqual("System.Double", EvaluationHelper.RemapPrimitiveTypes("double"));

        Assert.AreEqual("System.Decimal", EvaluationHelper.RemapPrimitiveTypes("decimal"));
        Assert.AreEqual("System.Decimal", EvaluationHelper.RemapPrimitiveTypes("money"));

        Assert.AreEqual("System.Object", EvaluationHelper.RemapPrimitiveTypes("object"));

        Assert.AreEqual("System.DateTime", EvaluationHelper.RemapPrimitiveTypes("datetime"));
        Assert.AreEqual("System.DateTimeOffset", EvaluationHelper.RemapPrimitiveTypes("datetimeoffset"));
        Assert.AreEqual("System.TimeSpan", EvaluationHelper.RemapPrimitiveTypes("timespan"));

        Assert.AreEqual("System.Guid", EvaluationHelper.RemapPrimitiveTypes("guid"));

        Assert.AreEqual("System.SomeType", EvaluationHelper.RemapPrimitiveTypes("System.SomeType"));
    }

    [TestMethod]
    public void RemapPrimitiveTypeAsNullableTest()
    {
        Assert.AreEqual(typeof(byte?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Byte"));
        Assert.AreEqual(typeof(sbyte?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.SByte"));
        Assert.AreEqual(typeof(short?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Int16"));
        Assert.AreEqual(typeof(int?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Int32"));
        Assert.AreEqual(typeof(long?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Int64"));
        Assert.AreEqual(typeof(ushort?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.UInt16"));
        Assert.AreEqual(typeof(uint?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.UInt32"));
        Assert.AreEqual(typeof(ulong?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.UInt64"));
        Assert.AreEqual(typeof(string), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.String"));
        Assert.AreEqual(typeof(char?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Char"));
        Assert.AreEqual(typeof(bool?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Boolean"));
        Assert.AreEqual(typeof(float?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Single"));
        Assert.AreEqual(typeof(double?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Double"));
        Assert.AreEqual(typeof(decimal?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Decimal"));
        Assert.AreEqual(typeof(object), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Object"));
        Assert.AreEqual(typeof(DateTime?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.DateTime"));
        Assert.AreEqual(typeof(DateTimeOffset?),
            EvaluationHelper.RemapPrimitiveTypeAsNullable("System.DateTimeOffset"));
        Assert.AreEqual(typeof(TimeSpan?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.TimeSpan"));
        Assert.AreEqual(typeof(Guid?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Guid"));
    }

    [TestMethod]
    public void RemapPrimitiveTypeAsNullable_WhenTypeIsUnknown_ShouldReturnNull()
    {
        Assert.IsNull(EvaluationHelper.RemapPrimitiveTypeAsNullable("System.DoesNotExist"));
    }

    [TestMethod]
    public void RemapPrimitiveTypes_WhenTypeNameIsNull_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => EvaluationHelper.RemapPrimitiveTypes(null!));
    }

    [TestMethod]
    public void PrimitiveTypeResolver_ShouldResolveAliasesAndNullableDeclarations()
    {
        Assert.IsTrue(PrimitiveTypeResolver.TryResolveDeclarationType("int", out var intType));
        Assert.AreEqual(typeof(int), intType);

        Assert.IsTrue(PrimitiveTypeResolver.TryResolveDeclarationType("datetime?", out var nullableDateTimeType));
        Assert.AreEqual(typeof(DateTime?), nullableDateTimeType);

        Assert.AreEqual("System.Nullable`1[System.Int32]", PrimitiveTypeResolver.RemapPrimitiveTypeName("int?"));
    }

    [TestMethod]
    public void PrimitiveTypeResolver_WhenDeclarationTypeIsUnknownOrEmpty_ShouldReturnFalseAndNullType()
    {
        Assert.IsFalse(PrimitiveTypeResolver.TryResolveDeclarationType("System.DoesNotExist", out var unknownType));
        Assert.IsNull(unknownType);

        Assert.IsFalse(PrimitiveTypeResolver.TryResolveDeclarationType("", out var emptyType));
        Assert.IsNull(emptyType);

        Assert.IsFalse(PrimitiveTypeResolver.TryResolveDeclarationType(null!, out var nullType));
        Assert.IsNull(nullType);
    }

    [TestMethod]
    public void CreateComplexTypeDescription_WithPrimitiveTypeAtRoot_ShouldNotExplorePrimitiveProperties()
    {
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("IntValue", typeof(int)).ToArray();

        Assert.HasCount(1, typeDescriptions);
        Assert.AreEqual("IntValue", typeDescriptions[0].FieldName);
        Assert.AreEqual(typeof(int), typeDescriptions[0].Type);
    }

    [TestMethod]
    public void CreateComplexTypeDescription_WithStringAtRoot_ShouldNotExploreStringProperties()
    {
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("StringValue", typeof(string)).ToArray();

        Assert.HasCount(1, typeDescriptions);
        Assert.AreEqual("StringValue", typeDescriptions[0].FieldName);
        Assert.AreEqual(typeof(string), typeDescriptions[0].Type);
    }

    [TestMethod]
    public void CreateComplexTypeDescription_WithObjectAtRoot_ShouldIncludeRootColumn()
    {
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("ObjectValue", typeof(object)).ToArray();

        Assert.HasCount(1, typeDescriptions);
        Assert.AreEqual("ObjectValue", typeDescriptions[0].FieldName);
        Assert.AreEqual(typeof(object), typeDescriptions[0].Type);
    }

    [TestMethod]
    public void GetSpecificColumnDescription_WithArrayColumn_ShouldReturnElementTypeInfo()
    {
        var table = new BasicEntityTable();

        var result = EvaluationHelper.GetSpecificColumnDescription(table, "Array");

        Assert.IsGreaterThan(0, result.Count, "Should return at least one row for the 'Array' column");
        Assert.AreEqual(3, result.Columns.Count(), "Should have 3 columns: Name, Index, Type");
        Assert.AreEqual("Array", result[0][0], "First row should contain 'Array' column name");
    }

    [TestMethod]
    public void GetSpecificColumnDescription_WithNonExistentColumn_ShouldThrowException()
    {
        var table = new BasicEntityTable();

        var exception = Assert.Throws<UnknownColumnOrAliasException>(() =>
            EvaluationHelper.GetSpecificColumnDescription(table, "NonExistent"));

        Assert.Contains("NonExistent", exception.Message, "Exception message should contain the column name");
    }

    [TestMethod]
    public void GetSpecificColumnDescription_WithCaseInsensitiveMatch_ShouldReturnColumnInfo()
    {
        var table = new BasicEntityTable();

        var result = EvaluationHelper.GetSpecificColumnDescription(table, "array");

        Assert.IsGreaterThan(0, result.Count, "Should find column with case-insensitive match");
        Assert.AreEqual("Array", result[0][0], "Should return the actual column name (Array)");
    }

    [TestMethod]
    public void GetSpecificColumnDescription_WithNonArrayColumn_ShouldDescribeType()
    {
        var table = new BasicEntityTable();


        Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(() =>
            EvaluationHelper.GetSpecificColumnDescription(table, "Name"));
    }

    [TestMethod]
    public void CreateComplexTypeDescription_WithArrayProperty_ShouldNotExplodeArray()
    {
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("Entity", typeof(TestClassWithArray))
            .ToArray();


        Assert.IsTrue(
            typeDescriptions.Any(pair => pair.FieldName == "Entity" && pair.Type == typeof(TestClassWithArray)));


        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Entity.Id" && pair.Type == typeof(int)));
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Entity.Name" && pair.Type == typeof(string)));


        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Entity.Numbers" && pair.Type == typeof(int[])));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Entity.Items" && pair.Type == typeof(TestSubClass[])));


        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName.StartsWith("Entity.Numbers.")));
        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName.StartsWith("Entity.Items.")));


        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Entity.SubClass" && pair.Type == typeof(TestSubClass)));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Entity.SubClass.SomeInt" && pair.Type == typeof(int)));


        // Note: This is different from arrays - Lists have Count, Capacity, etc.
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Entity.StringList"));

        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName.StartsWith("Entity.StringList.")));
    }

    public class TestClassWithArray
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int[] Numbers { get; set; } = [];
        public TestSubClass[] Items { get; set; } = [];
        public List<string> StringList { get; } = [];
        public TestSubClass? SubClass { get; set; }
    }

    private sealed record AsOfIndexTestRow(string Name, string? Team, int Score);

    private sealed record TypedRowSourceEntity(int Id);

    private sealed class ReflectedChunkSchema(RowSource<TypedRowSourceEntity> source) : ISchema
    {
        public string Name => "reflected";

        public ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            throw new NotSupportedException();
        }

        public SourceDescriptor DescribeSource(
            string name,
            SourceDescribeContext context,
            params object?[] parameters)
        {
            throw new NotSupportedException();
        }

        public IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
            string name,
            SourceRuntimeSettingsDescribeContext context,
            params object?[] parameters)
        {
            throw new NotSupportedException();
        }

        public SourcePlanResult TryPlanSource(
            string name,
            SourcePlanRequest request,
            params object?[] parameters)
        {
            throw new NotSupportedException();
        }

        public RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return (RowSource<T>)(object)source;
        }

        public SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext)
        {
            throw new NotSupportedException();
        }

        public SchemaMethodInfo[] GetRawConstructors(
            string methodName,
            SourceMetadataContext metadataContext)
        {
            throw new NotSupportedException();
        }

        public bool TryResolveMethod(
            string method,
            Type[] parameters,
            Type? entityType,
            [NotNullWhen(true)]
            out MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveRawMethod(
            string method,
            Type[] parameters,
            [NotNullWhen(true)]
            out MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveAggregationMethod(
            string method,
            Type[] parameters,
            Type? entityType,
            [NotNullWhen(true)]
            out MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveAggregationMethod(
            string method,
            Type[] parameters,
            Type? entityType,
            Func<MethodInfo, bool> methodFilter,
            [NotNullWhen(true)]
            out MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveWindowFunction(
            string method,
            [NotNullWhen(true)] out MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> GetAllLibraryMethods()
        {
            return new Dictionary<string, IReadOnlyList<MethodInfo>>();
        }
    }

    public class TestSubClass
    {
        public int SomeInt { get; set; }
    }

    private sealed class UnknownReadOnlyList<T>(IReadOnlyList<T> rows) : IReadOnlyList<T>
    {
        public int Count => rows.Count;

        public T this[int index] => rows[index];

        public IEnumerator<T> GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class TestClass
    {
        public TestClass? Test { get; set; }

        public int SomeInt { get; set; }

        public string SomeString { get; set; } = string.Empty;

        public object? SomeObject { get; set; }

        public TestSubClass? SubClass { get; set; }

        public int SomeMethod()
        {
            return 0;
        }
    }
}
