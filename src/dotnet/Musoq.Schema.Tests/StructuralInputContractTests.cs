using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Attributes;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class StructuralInputContractTests
{
    [TestMethod]
    public void Metadata_SelectsSolePublicConstructorAndBuildsNestedContract()
    {
        var descriptor = StructuralTypeDescriptor.FromClrType(typeof(PatternRecord));

        Assert.AreEqual(StructuralTypeKind.Record, descriptor.Kind);
        Assert.AreEqual("(Id: string, Mode: string = 'literal')", descriptor.ToCanonicalSql());
        Assert.IsFalse(descriptor.IsNullable);
        Assert.IsTrue(descriptor.Fields[1].HasDefault);
        Assert.AreEqual("'literal'", descriptor.Fields[1].Default.CanonicalText);
    }

    [TestMethod]
    public void Metadata_HandlesRecursiveCollectionsAndNullableLeaves()
    {
        var descriptor = StructuralTypeDescriptor.FromClrType(typeof(int?[][]));

        Assert.AreEqual(StructuralTypeKind.Collection, descriptor.Kind);
        Assert.AreEqual("int?[][]", descriptor.ToCanonicalSql());
        Assert.AreEqual(StructuralTypeKind.Collection, descriptor.ElementType!.Kind);
        Assert.IsTrue(descriptor.ElementType.ElementType!.IsNullable);
    }

    [TestMethod]
    public void Metadata_SelectsExactlyOneMarkedConstructor()
    {
        var constructor = StructuralInputMetadata.SelectConstructor(typeof(MarkedRecord));

        Assert.AreEqual(typeof(int), constructor.GetParameters()[0].ParameterType);
        var contract = StructuralInputMetadata.CreateContract("marked", constructor);
        Assert.AreEqual("value", contract.Parameters[0].Name);
    }

    [TestMethod]
    public void Metadata_RejectsAmbiguousAndMultiplyMarkedConstructors()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            StructuralInputMetadata.SelectConstructor(typeof(AmbiguousRecord)));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            StructuralInputMetadata.SelectConstructor(typeof(MultiplyMarkedRecord)));
    }

    [TestMethod]
    public void Metadata_RejectsUnsupportedOpenRefAndCyclicTypes()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            StructuralTypeDescriptor.FromClrType(typeof(OpenRecord<>)));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            StructuralTypeDescriptor.FromClrType(typeof(CyclicRecord)));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            StructuralInputMetadata.SelectConstructor(typeof(PrivateRecord)));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            StructuralInputMetadata.CreateContract(
                "ref",
                typeof(RefRecord).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single()));
    }

    [TestMethod]
    public void ValuesAndPlansAreImmutableSnapshots()
    {
        var fields = new Dictionary<string, StructuralValue>(StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = StructuralValue.FromScalar("todo")
        };
        var value = StructuralValue.FromRecord(fields);
        fields["Id"] = StructuralValue.FromScalar("changed");
        var elements = new List<StructuralValue> { value };
        var collection = StructuralValue.FromCollection(elements);
        elements.Clear();

        Assert.AreEqual("todo", value.Fields["id"].Scalar);
        Assert.HasCount(1, collection.Elements);
        Assert.ThrowsExactly<ArgumentException>(() => StructuralValue.FromRecord([
            new KeyValuePair<string, StructuralValue>("Id", StructuralValue.FromScalar(1)),
            new KeyValuePair<string, StructuralValue>("id", StructuralValue.FromScalar(2))]));

        var plan = new StructuralConstructionPlan(
            typeof(PatternRecord),
            new StructuralConstructorDescriptor(
                StructuralInputMetadata.SelectConstructor(typeof(PatternRecord)),
                StructuralTypeDescriptor.FromClrType(typeof(PatternRecord)).Fields),
            [1, 0]);
        Assert.AreEqual(2, plan.SourceFieldIndexes.Count);
    }

    [TestMethod]
    public void Limits_DefaultsAndValidationAreStable()
    {
        Assert.AreEqual(32, StructuralInputLimits.Default.MaxDepth);
        Assert.AreEqual(100_000, StructuralInputLimits.Default.MaxNodes);
        Assert.AreEqual(67_108_864, StructuralInputLimits.Default.MaxStringBytes);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new StructuralInputLimits(0, 1, 1));
        var explicitNull = StructuralDefaultDescriptor.Create(null);
        Assert.IsTrue(explicitNull.HasValue);
        Assert.AreEqual("null", explicitNull.CanonicalText);
        Assert.IsFalse(StructuralDefaultDescriptor.Absent.HasValue);
    }

    [TestMethod]
    public void StructuralSqlLiteralFormatter_UsesCanonicalEscapesAndNullText()
    {
        Assert.AreEqual("null", StructuralSqlLiteralFormatter.Format(null));
        Assert.AreEqual("'a\\\\b\\'c\\n'", StructuralSqlLiteralFormatter.Format("a\\b'c\n"));
        Assert.AreEqual("'x'", StructuralSqlLiteralFormatter.Format('x'));
        Assert.AreEqual("true", StructuralSqlLiteralFormatter.Format(true));
    }

    [TestMethod]
    public void SchemaBase_RegistersTypedSourceSeparatelyFromTableMetadata()
    {
        var schema = new TypedFixtureSchema();

        Assert.IsTrue(schema.TryGetTypedSourceRegistration("TYPED", out var registration));
        Assert.AreEqual(typeof(TypedRowSource), registration.SourceType);
        Assert.AreEqual(typeof(TypedRow), registration.RowType);
        Assert.AreEqual(0, registration.Overloads.Single().Contract.Parameters.Count);
        Assert.IsTrue(schema.GetRawConstructors(CreateMetadataContext()).All(static item => item.StructuralContract == null));
        Assert.IsNotNull(schema.GetConstructors("typed_source").Single().StructuralContract);
        Assert.IsFalse(schema.TryGetTypedSourceRegistration("missing", out _));
    }

    [TestMethod]
    public void SchemaBase_PublishesStructuralParameterMetadataForTypedSource()
    {
        var schema = new InputTypedFixtureSchema();

        Assert.IsTrue(schema.TryGetTypedSourceRegistration("input", out var registration));
        var parameter = registration.Overloads.Single().Contract.Parameters.Single();
        Assert.AreEqual("patterns", parameter.Name);
        Assert.AreEqual(StructuralTypeKind.Collection, parameter.Type.Kind);
        Assert.AreEqual("(Id: string, Mode: string = 'literal')[]?", parameter.Type.ToCanonicalSql());
        Assert.AreEqual(typeof(InputPattern[]), parameter.Type.ClrType);
    }
    [TestMethod]
    public void SchemaBase_RejectsDuplicateTypedRegistrationAndKeepsLegacySourceApis()
    {
        var schema = new TypedFixtureSchema();
        Assert.ThrowsExactly<InvalidOperationException>(() => schema.RegisterAgain());
        var legacy = new LegacyFixtureSchema();
        Assert.IsNotEmpty(legacy.GetRawConstructors(CreateMetadataContext()));
    }

    [TestMethod]
    public void SchemaBase_ExposesEveryTypedSourceConstructorInCanonicalOrder()
    {
        var schema = new OverloadedFixtureSchema();

        Assert.IsTrue(schema.TryGetTypedSourceRegistration("overloaded", out var registration));
        Assert.HasCount(2, registration.Overloads);
        CollectionAssert.AreEqual(
            new[] { "text", "value" },
            registration.Overloads.SelectMany(static overload => overload.Contract.Parameters).Select(static parameter => parameter.Name).ToArray());
        Assert.IsTrue(registration.Overloads.All(static overload => overload.InjectsExecutionContext));
    }

    [TestMethod]
    public void TypedSourceRegistration_AppliesPerSourceLimits()
    {
        var schema = new LimitedFixtureSchema();

        Assert.IsTrue(schema.TryGetTypedSourceRegistration("limited", out var registration));
        var limits = registration.Overloads.Single().Contract.Limits;
        Assert.AreEqual(4, limits.MaxDepth);
        Assert.AreEqual(9, limits.MaxNodes);
        Assert.AreEqual(20, limits.MaxStringBytes);
    }
    [TestMethod]
    public void TypedSourceRegistration_RejectsMisplacedExecutionContext()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => new InvalidContextFixtureSchema());
    }

    [TestMethod]
    public void StructuralCollections_UseOnlyTheSupportedReceivingInterfaces()
    {
        var list = StructuralTypeDescriptor.FromClrType(typeof(IReadOnlyList<int>));
        var enumerable = StructuralTypeDescriptor.FromClrType(typeof(IEnumerable<int>));

        Assert.AreEqual(StructuralTypeKind.Collection, list.Kind);
        Assert.AreEqual(StructuralTypeKind.Collection, enumerable.Kind);
        Assert.ThrowsExactly<InvalidOperationException>(() => StructuralTypeDescriptor.FromClrType(typeof(List<int>)));
        Assert.ThrowsExactly<InvalidOperationException>(() => StructuralTypeDescriptor.FromClrType(typeof(IReadOnlyCollection<int>)));
    }
    private static SourceMetadataContext CreateMetadataContext()
    {
        return new SourceMetadataContext(
            "schema-test",
            new CancellationTokenSource().Token,
            [],
            new Dictionary<string, string>(),
            NullLogger.Instance);
    }

    private sealed record PatternRecord(string Id, string Mode = "literal");

    private sealed class MarkedRecord
    {
        public MarkedRecord(string text) => Text = text;
        [StructuralInputConstructor]
        public MarkedRecord(int value) => Value = value;
        public string? Text { get; }
        public int Value { get; }
    }

    private sealed class AmbiguousRecord
    {
        public AmbiguousRecord(int value) => Value = value;
        public AmbiguousRecord(string value) => Text = value;
        public int Value { get; }
        public string? Text { get; }
    }

    private sealed class MultiplyMarkedRecord
    {
        [StructuralInputConstructor]
        public MultiplyMarkedRecord(int value) => Value = value;
        [StructuralInputConstructor]
        public MultiplyMarkedRecord(string value) => Text = value;
        public int Value { get; }
        public string? Text { get; }
    }

    private sealed class OpenRecord<T>
    {
        public OpenRecord(T value) => Value = value;
        public T Value { get; }
    }

    private sealed class CyclicRecord
    {
        public CyclicRecord(CyclicRecord child) => Child = child;
        public CyclicRecord Child { get; }
    }

    private sealed class PrivateRecord
    {
        private PrivateRecord(int value) => Value = value;
        public int Value { get; }
    }
    private sealed class RefRecord
    {
        public RefRecord(ref int value) => Value = value;
        public int Value { get; }
    }

    private sealed record TypedRow(int Value);

    private sealed class TypedRowSource : RowSource<TypedRow>
    {
        public TypedRowSource(SourceExecutionContext context) => _ = context;
        public override IEnumerable<IReadOnlyList<TypedRow>> Chunks => [[new TypedRow(1)]];
    }

    private sealed class TypedTable : ISchemaTable
    {
        private static readonly ISchemaColumn[] SchemaColumns = [new SchemaColumn("Value", 0, typeof(int))];
        public ISchemaColumn[] Columns => SchemaColumns;
        public SchemaTableMetadata Metadata { get; } = new(typeof(TypedRow));
        public ISchemaColumn? GetColumnByName(string name) =>
            SchemaColumns.SingleOrDefault(column => string.Equals(column.ColumnName, name, StringComparison.OrdinalIgnoreCase));
        public ISchemaColumn[] GetColumnsByName(string name) => GetColumnByName(name) is { } column ? [column] : [];
    }

    private sealed record InputPattern(string Id, string Mode = "literal");

    private sealed class InputTypedSource : RowSource<TypedRow>
    {
        public InputTypedSource(InputPattern[] patterns, SourceExecutionContext context)
        {
            _ = patterns;
            _ = context;
        }

        public override IEnumerable<IReadOnlyList<TypedRow>> Chunks => [[new TypedRow(1)]];
    }

    private sealed class InputTypedFixtureSchema : SchemaBase
    {
        public InputTypedFixtureSchema() : base("input", CreateLibrary())
        {
            AddTable<TypedTable>("input");
            AddTypedSource<InputTypedSource>("input");
        }

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new Musoq.Plugins.LibraryBase());
            return new MethodsAggregator(manager);
        }
    }
    private sealed class TypedFixtureSchema : SchemaBase
    {
        public TypedFixtureSchema() : base("typed", CreateLibrary())
        {
            AddTable<TypedTable>("typed");
            AddTypedSource<TypedRowSource>("typed");
        }

        public void RegisterAgain() => AddTypedSource<TypedRowSource>("typed");

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new Musoq.Plugins.LibraryBase());
            return new MethodsAggregator(manager);
        }
    }

    private sealed class LegacyFixtureSchema : SchemaBase
    {
        public LegacyFixtureSchema() : base("legacy", CreateLibrary()) => AddTable<TypedTable>("typed");

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new Musoq.Plugins.LibraryBase());
            return new MethodsAggregator(manager);
        }
    }
    private sealed class OverloadedSource : RowSource<TypedRow>
    {
        public OverloadedSource(string text, SourceExecutionContext context)
        {
            _ = text;
            _ = context;
        }

        public OverloadedSource(int value, SourceExecutionContext context)
        {
            _ = value;
            _ = context;
        }

        public override IEnumerable<IReadOnlyList<TypedRow>> Chunks => [[new TypedRow(1)]];
    }

    private sealed class OverloadedFixtureSchema : SchemaBase
    {
        public OverloadedFixtureSchema() : base("overloaded", CreateLibrary()) => AddTypedSource<OverloadedSource>("overloaded");

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new Musoq.Plugins.LibraryBase());
            return new MethodsAggregator(manager);
        }
    }

    private sealed class LimitedFixtureSchema : SchemaBase
    {
        public LimitedFixtureSchema() : base("limited", CreateLibrary()) =>
            AddTypedSource<TypedRowSource>("limited", new TypedSourceRegistrationOptions(new StructuralInputLimits(4, 9, 20)));

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new Musoq.Plugins.LibraryBase());
            return new MethodsAggregator(manager);
        }
    }
    private sealed class InvalidContextSource : RowSource<TypedRow>
    {
        public InvalidContextSource(SourceExecutionContext context, int value)
        {
            _ = context;
            _ = value;
        }

        public override IEnumerable<IReadOnlyList<TypedRow>> Chunks => [[new TypedRow(1)]];
    }

    private sealed class InvalidContextFixtureSchema : SchemaBase
    {
        public InvalidContextFixtureSchema() : base("invalid-context", CreateLibrary()) => AddTypedSource<InvalidContextSource>("invalid");

        private static MethodsAggregator CreateLibrary()
        {
            var manager = new MethodsManager();
            manager.RegisterLibraries(new Musoq.Plugins.LibraryBase());
            return new MethodsAggregator(manager);
        }
    }}
