using System;
using System.Collections.Generic;
using System.Threading;
using Musoq.Converter;
using Musoq.Converter.Build;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Schema.NegativeTests;

public class NegativeTestsBase
{
    protected static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);

    static NegativeTestsBase()
    {
        Culture.ApplyWithDefaultCulture();
    }

    protected CancellationTokenSource TokenSource { get; } = new();
    protected ILoggerResolver LoggerResolver { get; } = new TestsLoggerResolver();

    protected static PersonEntity[] DefaultPeople =>
    [
        new() { Id = 1, Name = "Alice", Age = 25, City = "London", Salary = 50000m, BirthDate = new DateTime(1999, 1, 15), ManagerId = null, Email = "alice@test.com" },
        new() { Id = 2, Name = "Bob", Age = 35, City = "Paris", Salary = 60000m, BirthDate = new DateTime(1989, 6, 20), ManagerId = 1, Email = "bob@test.com" },
        new() { Id = 3, Name = "Charlie", Age = 28, City = "London", Salary = 55000m, BirthDate = new DateTime(1996, 3, 10), ManagerId = 1, Email = "charlie@test.com" },
        new() { Id = 4, Name = "Diana", Age = 42, City = "Berlin", Salary = 75000m, BirthDate = new DateTime(1982, 11, 5), ManagerId = 2, Email = "diana@test.com" },
        new() { Id = 5, Name = "Eve", Age = 31, City = "Paris", Salary = 62000m, BirthDate = new DateTime(1993, 8, 25), ManagerId = 2, Email = "eve@test.com" }
    ];

    protected static OrderEntity[] DefaultOrders =>
    [
        new() { OrderId = 100, PersonId = 1, Amount = 250.50m, Status = "Completed", OrderDate = new DateTime(2024, 1, 10), Notes = "First order" },
        new() { OrderId = 101, PersonId = 1, Amount = 150.00m, Status = "Pending", OrderDate = new DateTime(2024, 2, 15), Notes = null },
        new() { OrderId = 102, PersonId = 2, Amount = 500.00m, Status = "Completed", OrderDate = new DateTime(2024, 1, 20), Notes = "Rush delivery" },
        new() { OrderId = 103, PersonId = 3, Amount = 75.25m, Status = "Cancelled", OrderDate = new DateTime(2024, 3, 5), Notes = null },
        new() { OrderId = 104, PersonId = 5, Amount = 1200.00m, Status = "Completed", OrderDate = new DateTime(2024, 2, 28), Notes = "Bulk order" }
    ];

    protected static TypesEntity[] DefaultTypes =>
    [
        new()
        {
            IntCol = 42,
            LongCol = 100000L,
            ShortCol = 10,
            ByteCol = 255,
            DecimalCol = 99.99m,
            DoubleCol = 3.14d,
            FloatCol = 2.71f,
            BoolCol = true,
            StringCol = "hello",
            DateTimeCol = new DateTime(2024, 1, 1),
            DateTimeOffsetCol = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            GuidCol = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            NullableIntCol = null
        }
    ];

    protected static NestedEntity[] DefaultNested =>
    [
        new() { Id = 1, Info = new ComplexInfo { Label = "Alpha", Score = 90, Tags = ["tag1", "tag2", "tag3"] } },
        new() { Id = 2, Info = new ComplexInfo { Label = "Beta", Score = 80, Tags = ["tag4"] } },
        new() { Id = 3, Info = null }
    ];

    protected static PersonEntity[] EmptyPeople => [];

    protected static PersonEntity[] SinglePerson =>
    [
        new() { Id = 1, Name = "Solo", Age = 30, City = "Tokyo", Salary = 70000m, BirthDate = new DateTime(1994, 5, 12), ManagerId = null, Email = "solo@test.com" }
    ];

    protected ISchemaProvider CreateSchemaProvider(
        PersonEntity[] people = null,
        OrderEntity[] orders = null,
        TypesEntity[] types = null,
        NestedEntity[] nested = null)
    {
        people ??= DefaultPeople;
        orders ??= DefaultOrders;
        types ??= DefaultTypes;
        nested ??= DefaultNested;

        var tables = new Dictionary<string, (ISchemaTable Table, RowSource Source)>
        {
            {
                "people",
                (new PersonTable(),
                    new NegativeTestRowSource<PersonEntity>(people, PersonEntity.NameToIndexMap, PersonEntity.IndexToObjectAccessMap))
            },
            {
                "orders",
                (new OrderTable(),
                    new NegativeTestRowSource<OrderEntity>(orders, OrderEntity.NameToIndexMap, OrderEntity.IndexToObjectAccessMap))
            },
            {
                "empty",
                (new PersonTable(),
                    new NegativeTestRowSource<PersonEntity>(EmptyPeople, PersonEntity.NameToIndexMap, PersonEntity.IndexToObjectAccessMap))
            },
            {
                "single",
                (new PersonTable(),
                    new NegativeTestRowSource<PersonEntity>(SinglePerson, PersonEntity.NameToIndexMap, PersonEntity.IndexToObjectAccessMap))
            },
            {
                "types",
                (new TypesTable(),
                    new NegativeTestRowSource<TypesEntity>(types, TypesEntity.NameToIndexMap, TypesEntity.IndexToObjectAccessMap))
            },
            {
                "nested",
                (new NestedTable(),
                    new NegativeTestRowSource<NestedEntity>(nested, NestedEntity.NameToIndexMap, NestedEntity.IndexToObjectAccessMap))
            }
        };

        var schema = new NegativeTestSchema(tables);

        return new NegativeTestSchemaProvider(new Dictionary<string, ISchema>
        {
            { "#test", schema }
        });
    }

    protected CompiledQuery CompileQuery(string query, ISchemaProvider schemaProvider = null)
    {
        schemaProvider ??= CreateSchemaProvider();

        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            schemaProvider,
            LoggerResolver,
            TestCompilationOptions);
    }
}
