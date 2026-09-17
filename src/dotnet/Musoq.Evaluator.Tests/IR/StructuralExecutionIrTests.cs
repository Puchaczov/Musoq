using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Targets.Abstractions;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class StructuralExecutionIrTests
{
    [TestMethod]
    public void StructuralExpressions_PreserveAuthoredOrderAndPresence()
    {
        var integer = new ExecutionLiteral(1, typeof(int));
        var text = new ExecutionLiteral("todo", typeof(string));
        var record = new ExecutionStructuralRecord(
            ExecutionClrBindingFactory.FromClr(typeof(SampleInput)),
            [
                new ExecutionStructuralField("Name", text),
                new ExecutionStructuralField("Id", integer, IsPresent: false)
            ]);
        var array = new ExecutionStructuralArray(
            ExecutionClrBindingFactory.FromClr(typeof(int[])),
            ExecutionClrBindingFactory.FromClr(typeof(int)),
            [integer, new ExecutionLiteral(2, typeof(int))]);

        Assert.AreEqual("Name", record.Fields[0].Name);
        Assert.IsFalse(record.Fields[1].IsPresent);
        Assert.AreEqual(2, array.Elements.Count);
        Assert.AreEqual("array", array.ReturnType.Descriptor.Kind == ExecutionPortableTypeKind.Array ? "array" : string.Empty);
    }

    [TestMethod]
    public void ConstructionPlan_IsIndexedAndDefensivelyCopied()
    {
        var fields = new[]
        {
            new ExecutionStructuralFieldPlan("Id", ExecutionClrBindingFactory.FromClr(typeof(int)), true, false, null),
            new ExecutionStructuralFieldPlan("Name", ExecutionClrBindingFactory.FromClr(typeof(string)), true, false, null)
        };
        var indexes = new[] { 1, 0 };
        var defaults = new ExecutionExpression?[] { null, null };
        var constructor = ExecutionClrBindingFactory.FromClr(
            typeof(SampleInput).GetConstructor([typeof(int), typeof(string)])!);
        var plan = new ExecutionStructuralConstructionPlan(
            ExecutionClrBindingFactory.FromClr(typeof(SampleInput)),
            constructor,
            new ExecutionStructuralShape(StructuralTypeKind.Record, null, fields, null, false),
            indexes,
            defaults,
            ExecutionStructuralPreparationLifetime.Inline);

        indexes[0] = 99;
        defaults[0] = new ExecutionLiteral(42, typeof(int));

        Assert.AreEqual(1, plan.SourceFieldIndexes[0]);
        Assert.IsNull(plan.Defaults[0]);
        StringAssert.Contains(plan.InputShape.CanonicalType, "Id: int");
    }

    [TestMethod]
    public void CatalogAndPrinter_RegisterStructuralOperations()
    {
        var integer = new ExecutionLiteral(1, typeof(int));
        var array = new ExecutionStructuralArray(
            ExecutionClrBindingFactory.FromClr(typeof(int[])),
            ExecutionClrBindingFactory.FromClr(typeof(int)),
            [integer]);
        var record = new ExecutionStructuralRecord(
            ExecutionClrBindingFactory.FromClr(typeof(SampleInput)),
            [new ExecutionStructuralField("Id", integer)]);
        var constructor = ExecutionClrBindingFactory.FromClr(
            typeof(SampleInput).GetConstructor([typeof(int), typeof(string)])!);
        var plan = new ExecutionStructuralConstructionPlan(
            ExecutionClrBindingFactory.FromClr(typeof(SampleInput)),
            constructor,
            new ExecutionStructuralShape(
                StructuralTypeKind.Record,
                null,
                [
                    new ExecutionStructuralFieldPlan("Id", ExecutionClrBindingFactory.FromClr(typeof(int)), true, false, null),
                    new ExecutionStructuralFieldPlan("Name", ExecutionClrBindingFactory.FromClr(typeof(string)), false, true, "'default'")
                ],
                null,
                false),
            [0, -1],
            [null, new ExecutionLiteral("default", typeof(string))],
            ExecutionStructuralPreparationLifetime.Statement);
        var node = new ExecutionPrepareStructuralInput(
            new ExecutionVariable("prepared", typeof(SampleInput)),
            record,
            plan);
        var executionPlan = new ExecutionPlan(
            "Q_Structural",
            [],
            new ExecutionBlock([node]));

        Assert.AreEqual("expr.structural-record", ExecutionOperationCatalog.Resolve(record).Value);
        Assert.AreEqual("expr.structural-array", ExecutionOperationCatalog.Resolve(array).Value);
        Assert.AreEqual("structural.prepare", ExecutionOperationCatalog.Resolve(node).Value);

        var printed = ExecutionPlanPrinter.Print(executionPlan);
        StringAssert.Contains(printed, "PrepareStructuralInput");
        StringAssert.Contains(printed, "Id: 1");
        StringAssert.Contains(printed, "shape (Id: int");
    }

    [TestMethod]
    public void StructuralShapeFactory_PreservesRecursiveRecordAndCollectionShapes()
    {
        var descriptor = StructuralTypeDescriptor.FromClrType(typeof(Envelope));
        var shape = ExecutionStructuralShapeFactory.Create(descriptor);

        Assert.AreEqual(StructuralTypeKind.Record, shape.Kind);
        Assert.AreEqual(1, shape.Fields.Count);
        var collectionShape = shape.Fields[0].Shape;
        Assert.IsNotNull(collectionShape);
        Assert.AreEqual(StructuralTypeKind.Collection, collectionShape.Kind);
        Assert.IsNotNull(collectionShape.ElementShape);
        Assert.AreEqual(StructuralTypeKind.Record, collectionShape.ElementShape!.Kind);
        Assert.AreEqual("(Items: (Value: int32)[])", shape.CanonicalType);
    }

    [TestMethod]
    public void ConstructionPlan_ContainsPortablePreparationMetadata()
    {
        var constructor = ExecutionClrBindingFactory.FromClr(
            typeof(SampleInput).GetConstructor([typeof(int), typeof(string)])!);
        var shape = new ExecutionStructuralShape(
            StructuralTypeKind.Record,
            null,
            [
                new ExecutionStructuralFieldPlan("Id", ExecutionClrBindingFactory.FromClr(typeof(int)), true, false, null),
                new ExecutionStructuralFieldPlan("Name", ExecutionClrBindingFactory.FromClr(typeof(string)), true, false, null)
            ],
            null,
            false);
        var first = new ExecutionStructuralConstructionPlan(
            ExecutionClrBindingFactory.FromClr(typeof(SampleInput)),
            constructor,
            shape,
            [0, 1],
            [null, null],
            ExecutionStructuralPreparationLifetime.Statement,
            new ExecutionStructuralLimitPlan(8, 50, 512),
            ExecutionStructuralInputOrigin.Parameter,
            ExecutionStructuralMetricsStrategy.TypedPrepass,
            ExecutionStructuralOwnershipMode.Copy);
        var second = new ExecutionStructuralConstructionPlan(
            ExecutionClrBindingFactory.FromClr(typeof(SampleInput)),
            constructor,
            shape,
            [0, 1],
            [null, null],
            ExecutionStructuralPreparationLifetime.Statement,
            new ExecutionStructuralLimitPlan(9, 50, 512),
            ExecutionStructuralInputOrigin.Parameter,
            ExecutionStructuralMetricsStrategy.TypedPrepass,
            ExecutionStructuralOwnershipMode.Copy);

        Assert.AreEqual(ExecutionStructuralInputOrigin.Parameter, first.Origin);
        Assert.AreEqual(ExecutionStructuralMetricsStrategy.TypedPrepass, first.MetricsStrategy);
        Assert.AreEqual(ExecutionStructuralOwnershipMode.Copy, first.Ownership);
        Assert.AreNotEqual(first.MetadataFingerprint, second.MetadataFingerprint);
    }

    [TestMethod]
    public void ExecutionPlan_FreezesStoredTableRepresentationsAndPrintsDecisions()
    {
        var rowShape = new GeneratedRowShape("Rows", []);
        var representation = new ExecutionStoredTableRepresentationPlan(
            3,
            rowShape,
            ownership: ExecutionStructuralOwnershipMode.Transfer);
        var representations = new[] { representation };
        var plan = new ExecutionPlan("Q_Stored", [], new ExecutionBlock([]), storedTableRepresentations: representations);
        representations[0] = new ExecutionStoredTableRepresentationPlan(4, rowShape);

        Assert.AreEqual(3, plan.StoredTableRepresentations.Single().TableIndex);
        StringAssert.Contains(ExecutionPlanPrinter.Print(plan), "StoredTableRepresentations");
        StringAssert.Contains(ExecutionPlanPrinter.Print(plan), "ownership=Transfer");
        Assert.Throws<ArgumentException>(() => new ExecutionPlan(
            "Q_DuplicateStored",
            [],
            new ExecutionBlock([]),
            storedTableRepresentations: [representation, representation]));
    }

    [TestMethod]
    public void CteCollectionInput_CarriesPlannerOwnershipAndDemandMetadata()
    {
        var rowShape = new GeneratedRowShape(
            "CteRows",
            [new FieldBinding("Value", "Value", 0, typeof(int), FieldNullability.Unknown, new GeneratedFieldAccess("Value"))]);
        var input = new ExecutionCteCollectionInput(
            "numbers",
            new ExecutionStoredTableRows(2, rowShape),
            ExecutionClrBindingFactory.FromClr(typeof(int[])),
            ExecutionClrBindingFactory.FromClr(typeof(int)),
            [new ExecutionCteCollectionField("Value", 0, ExecutionClrBindingFactory.FromClr(typeof(int)))])
        {
            LimitBinding = new ExecutionStructuralLimitBinding(
                new ExecutionStructuralLimitPlan(4, 12, 128),
                ExecutionStructuralInputOrigin.Cte,
                "n.argument[0]",
                "n:1"),
            Ownership = ExecutionStructuralOwnershipMode.Copy,
            Lifetime = ExecutionStructuralPreparationLifetime.Statement,
            MetricsStrategy = ExecutionStructuralMetricsStrategy.TypedPrepass
        };
        var printed = ExecutionPlanPrinter.Print(new ExecutionPlan(
            "Q_CteInput",
            [],
            new ExecutionBlock([new ExecutionLet(new ExecutionVariable("values", typeof(int[])), input)]),
            storedTableRepresentations: [new ExecutionStoredTableRepresentationPlan(
                2,
                rowShape,
                ownership: ExecutionStructuralOwnershipMode.Copy,
                lifetime: ExecutionStructuralPreparationLifetime.Statement)]));

        StringAssert.Contains(printed, "ownership Copy");
        StringAssert.Contains(printed, "metrics TypedPrepass");
        StringAssert.Contains(printed, "limits depth=4;nodes=12;strings=128");
    }

    private readonly record struct SampleInput(int Id, string Name);

    private readonly record struct Nested(int Value);

    private readonly record struct Envelope(Nested[] Items);
}
