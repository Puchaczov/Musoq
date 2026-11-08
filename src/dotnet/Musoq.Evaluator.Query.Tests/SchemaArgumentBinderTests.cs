using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.StructuralInputs;
using Musoq.Schema.Reflection;
using SchemaConstructorInfo = Musoq.Schema.Reflection.ConstructorInfo;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class SchemaArgumentBinderTests
{
    [TestMethod]
    public void BindStaticArguments_ShouldReturnLiteralValuesInOrder()
    {
        var args = new ArgsListNode(
        [
            new IntegerNode("1", string.Empty),
            new DecimalNode("2.5"),
            new BooleanNode(true),
            new BooleanNode(false),
            new StringNode("text"),
            new WordNode("word"),
            new HexIntegerNode("0x10"),
            new BinaryIntegerNode("0b10"),
            new OctalIntegerNode("0o10")
        ]);

        var values = SchemaArgumentBinder.BindStaticArguments(args);

        CollectionAssert.AreEqual(
            new object[] { 1, 2.5m, true, false, "text", "word", 16L, 2L, 8L },
            values);
    }

    [TestMethod]
    public void BindStaticArguments_ShouldStopAtFirstDynamicArgument()
    {
        var args = new ArgsListNode(
        [
            new IdentifierNode("rowValue", typeof(string)),
            new StringNode("static")
        ]);

        var values = SchemaArgumentBinder.BindStaticArguments(args);

        CollectionAssert.AreEqual(Array.Empty<object?>(), values);
    }

    [TestMethod]
    public void BindStaticArguments_ShouldKeepOnlyTheMaterializablePrefix()
    {
        var args = new ArgsListNode(
        [
            new StringNode("first"),
            new IdentifierNode("rowValue", typeof(string)),
            new IntegerNode("3")
        ]);

        var values = SchemaArgumentBinder.BindStaticArguments(args);

        CollectionAssert.AreEqual(new object?[] { "first" }, values);
    }

    [TestMethod]
    public void BoundInvocation_ShouldNotShiftStaticValuesAfterDynamicSlot()
    {
        var args = new ArgsListNode(
        [
            new IdentifierNode("rowValue", typeof(string)),
            new IntegerNode("2")
        ]);
        var constructor = typeof(BoundSourceTable)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single(candidate => candidate.GetParameters().Length == 2);
        var method = new SchemaMethodInfo(
            "source",
            new SchemaConstructorInfo(
                constructor,
                false,
                ("first", typeof(string)),
                ("second", typeof(int))));
        var signature = SchemaSourceSignature.Create(method);
        var invocation = new BoundSchemaInvocation(
            signature,
            [
                new BoundSchemaArgument(0, 0, null),
                new BoundSchemaArgument(1, 1, null)
            ],
            usesNamedArguments: true);

        var values = SchemaArgumentBinder.BindStaticArguments(args, invocation: invocation);

        CollectionAssert.AreEqual(Array.Empty<object?>(), values);
    }

    private sealed class BoundSourceTable
    {
        public BoundSourceTable(string first, int second)
        {
            _ = (first, second);
        }
    }

    [TestMethod]
    public void Bind_CteRelationArgument_ShouldProduceRelationBinding()
    {
        var sourceConstructor = typeof(RelationSource)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();
        var constructor = new SchemaConstructorInfo(
            sourceConstructor,
            false,
            ("rows", typeof(IReadOnlyList<RelationRecord>)));
        constructor.StructuralContract = StructuralInputMetadata.CreateContract(
            "source",
            sourceConstructor);
        var method = new SchemaMethodInfo("source", constructor);
        var columns = new ISchemaColumn[]
        {
            new Musoq.Schema.DataSources.SchemaColumn("Id", 0, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn("Name", 1, typeof(string))
        };
        var relation = new CteRelationBinding(
            "rows",
            columns,
            CteRelationShapeFactory.CreateRecordShape(columns));
        var result = SchemaSourceArgumentBinder.Bind(
            new ArgsListNode([new IdentifierNode("rows")]),
            [method],
            relationResolver: static node => node is IdentifierNode identifier && identifier.Name == "rows"
                ? new CteRelationBinding(
                    "rows",
                    [
                        new Musoq.Schema.DataSources.SchemaColumn("Id", 0, typeof(int)),
                        new Musoq.Schema.DataSources.SchemaColumn("Name", 1, typeof(string))
                    ],
                    CteRelationShapeFactory.CreateRecordShape(
                    [
                        new Musoq.Schema.DataSources.SchemaColumn("Id", 0, typeof(int)),
                        new Musoq.Schema.DataSources.SchemaColumn("Name", 1, typeof(string))
                    ]))
                : null);

        Assert.IsNull(result.Failure);
        Assert.IsNotNull(result.Invocation);
        Assert.IsTrue(result.Invocation!.HasRelations);
        Assert.IsTrue(result.Invocation.Arguments.Single().UsesRelation);
        Assert.AreEqual("rows", result.Invocation.Arguments.Single().Relation!.Name);
        _ = relation;
    }

    [TestMethod]
    public void Bind_CtePrimitiveRelationWithMultipleColumns_ShouldBeRejected()
    {
        var sourceConstructor = typeof(PrimitiveRelationSource)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();
        var constructor = new SchemaConstructorInfo(
            sourceConstructor,
            false,
            ("values", typeof(IReadOnlyList<int>)));
        constructor.StructuralContract = StructuralInputMetadata.CreateContract(
            "primitive",
            sourceConstructor);
        var method = new SchemaMethodInfo("primitive", constructor);
        var columns = new ISchemaColumn[]
        {
            new Musoq.Schema.DataSources.SchemaColumn("First", 0, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn("Second", 1, typeof(int))
        };
        var shape = CteRelationShapeFactory.CreateRecordShape(columns);
        var result = SchemaSourceArgumentBinder.Bind(
            new ArgsListNode([new IdentifierNode("numbers")]),
            [method],
            relationResolver: node => node is IdentifierNode
                ? new CteRelationBinding("numbers", columns, shape)
                : null);

        Assert.IsNull(result.Invocation);
        Assert.IsNotNull(result.Failure);
        Assert.AreEqual(DiagnosticCode.MQ3088_NoMatchingCallableOverload, result.Failure!.Code);
    }

    [TestMethod]
    public void TypedBinding_ShouldPreferExactConversionCost()
    {
        var intMethod = CreateTypedMethod(typeof(IntSource), "value");
        var longMethod = CreateTypedMethod(typeof(LongSource), "value");
        var result = SchemaSourceArgumentBinder.Bind(
            new ArgsListNode([new IntegerNode("1", string.Empty)]),
            [longMethod, intMethod]);

        Assert.IsNull(result.Failure);
        Assert.IsNotNull(result.Invocation);
        Assert.AreEqual(typeof(int), result.Invocation!.Signature.Parameters[0].ParameterType);
    }

    [TestMethod]
    public void TypedBinding_ShouldReportEqualStructuralApplicabilityAsAmbiguous()
    {
        var first = CreateTypedMethod(typeof(FirstCollectionSource), "values");
        var second = CreateTypedMethod(typeof(SecondCollectionSource), "values");
        var result = SchemaSourceArgumentBinder.Bind(
            new ArgsListNode([new ArrayLiteralNode([new IntegerNode("1", string.Empty)])]),
            [second, first]);

        Assert.IsNull(result.Invocation);
        Assert.IsNotNull(result.Failure);
        Assert.AreEqual(DiagnosticCode.MQ3089_AmbiguousCallableOverload, result.Failure!.Code);
    }

    [TestMethod]
    public void TypedBinding_ShouldDistinguishScalarAndCteInterpretations()
    {
        var constructor = typeof(RecordRelationSource).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var metadata = new SchemaConstructorInfo(
            constructor,
            false,
            ("rows", typeof(IReadOnlyList<RelationRecord>)))
        {
            StructuralContract = StructuralInputMetadata.CreateContract("source", constructor)
        };
        var columns = new ISchemaColumn[]
        {
            new Musoq.Schema.DataSources.SchemaColumn("Id", 0, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn("Name", 1, typeof(string))
        };
        var relation = new CteRelationBinding(
            "rows",
            columns,
            CteRelationShapeFactory.CreateRecordShape(columns),
            ScalarType: typeof(IReadOnlyList<RelationRecord>));
        var argument = new IdentifierNode("rows", typeof(IReadOnlyList<RelationRecord>));
        var result = SchemaSourceArgumentBinder.Bind(
            new ArgsListNode([argument]),
            [new SchemaMethodInfo("source", metadata)],
            relationResolver: node => ReferenceEquals(node, argument) ? relation : null);

        Assert.IsNull(result.Invocation);
        Assert.IsNotNull(result.Failure);
        Assert.AreEqual(DiagnosticCode.MQ3116_AmbiguousRelationArgument, result.Failure!.Code);
    }

    [TestMethod]
    public void TypedBinding_ShouldReportScalarAndCteAmbiguityAcrossOverloads()
    {
        var scalarConstructor = typeof(CrossInterpretationScalarSource)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();
        var relationConstructor = typeof(CrossInterpretationRelationSource)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();
        var scalarMethod = new SchemaMethodInfo(
            "source",
            new SchemaConstructorInfo(scalarConstructor, false, ("value", typeof(int)))
            {
                StructuralContract = StructuralInputMetadata.CreateContract("source", scalarConstructor)
            });
        var relationMethod = new SchemaMethodInfo(
            "source",
            new SchemaConstructorInfo(
                relationConstructor,
                false,
                ("rows", typeof(IReadOnlyList<RelationRecord>)))
            {
                StructuralContract = StructuralInputMetadata.CreateContract("source", relationConstructor)
            });
        var argument = new IdentifierNode("rows", typeof(int));
        var columns = new ISchemaColumn[]
        {
            new Musoq.Schema.DataSources.SchemaColumn("Id", 0, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn("Name", 1, typeof(string))
        };
        var relation = new CteRelationBinding(
            "rows",
            columns,
            CteRelationShapeFactory.CreateRecordShape(columns),
            ScalarType: typeof(int));

        var result = SchemaSourceArgumentBinder.Bind(
            new ArgsListNode([argument]),
            [relationMethod, scalarMethod],
            relationResolver: node => ReferenceEquals(node, argument) ? relation : null);

        Assert.IsNull(result.Invocation);
        Assert.IsNotNull(result.Failure);
        Assert.AreEqual(DiagnosticCode.MQ3116_AmbiguousRelationArgument, result.Failure!.Code);
        StringAssert.Contains(result.Failure.Message, "scalar column");
        Assert.IsTrue(result.Failure.Arguments!.ContainsKey("scalarCandidates"));
        Assert.IsTrue(result.Failure.Arguments.ContainsKey("relationCandidates"));
    }

    [TestMethod]
    public void TypedBinding_ShouldKeepIncompatibleCteFromFallingBackToScalar()
    {
        var constructor = typeof(IntSource)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();
        var metadata = new SchemaConstructorInfo(constructor, false, ("value", typeof(int)))
        {
            StructuralContract = StructuralInputMetadata.CreateContract("source", constructor)
        };
        var method = new SchemaMethodInfo("source", metadata);
        var argument = new IdentifierNode("rows", typeof(int));
        var columns = new ISchemaColumn[]
        {
            new Musoq.Schema.DataSources.SchemaColumn("Id", 0, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn("Name", 1, typeof(string))
        };
        var relation = new CteRelationBinding(
            "rows",
            columns,
            CteRelationShapeFactory.CreateRecordShape(columns),
            ScalarType: typeof(int));

        var result = SchemaSourceArgumentBinder.Bind(
            new ArgsListNode([argument]),
            [method],
            relationResolver: node => ReferenceEquals(node, argument) ? relation : null);

        Assert.IsNull(result.Invocation);
        Assert.IsNotNull(result.Failure);
        Assert.AreEqual(DiagnosticCode.MQ3088_NoMatchingCallableOverload, result.Failure!.Code);
        StringAssert.Contains(result.Failure.Message, "CTE relation requires a collection");
        Assert.AreEqual("structural", result.Failure.Arguments!["reasonKind"]);
    }

    [TestMethod]
    public void TypedBinding_ShouldPreserveSpecificStructuralFailure()
    {
        var constructor = typeof(RecordRelationSource)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();
        var metadata = new SchemaConstructorInfo(
            constructor,
            false,
            ("rows", typeof(IReadOnlyList<RelationRecord>)))
        {
            StructuralContract = StructuralInputMetadata.CreateContract("source", constructor)
        };
        var method = new SchemaMethodInfo("source", metadata);
        var argument = new IdentifierNode("rows", typeof(IReadOnlyList<RelationRecord>));
        var columns = new ISchemaColumn[]
        {
            new Musoq.Schema.DataSources.SchemaColumn("Id", 0, typeof(int)),
            new Musoq.Schema.DataSources.SchemaColumn("Unexpected", 1, typeof(string))
        };
        var relation = new CteRelationBinding(
            "rows",
            columns,
            CteRelationShapeFactory.CreateRecordShape(columns));

        var result = SchemaSourceArgumentBinder.Bind(
            new ArgsListNode([argument]),
            [method],
            relationResolver: node => ReferenceEquals(node, argument) ? relation : null);

        Assert.IsNull(result.Invocation);
        Assert.IsNotNull(result.Failure);
        Assert.AreEqual(DiagnosticCode.MQ3088_NoMatchingCallableOverload, result.Failure!.Code);
        StringAssert.Contains(result.Failure.Message, "unexpected field");
        Assert.AreEqual("structural", result.Failure.Arguments!["reasonKind"]);
    }

    [TestMethod]
    public void UntypedBinding_ShouldRejectStructuralLiteralInsteadOfFallingBackToLegacyDispatch()
    {
        var constructor = typeof(LegacyCollectionSource)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single();
        var method = new SchemaMethodInfo(
            "source",
            new SchemaConstructorInfo(
                constructor,
                false,
                ("values", typeof(IReadOnlyList<int>))));

        var result = SchemaSourceArgumentBinder.Bind(
            new ArgsListNode([new ArrayLiteralNode([new IntegerNode("1", string.Empty)])]),
            [method]);

        Assert.IsNull(result.Invocation);
        Assert.IsNotNull(result.Failure);
        Assert.AreEqual(DiagnosticCode.MQ3088_NoMatchingCallableOverload, result.Failure!.Code);
        StringAssert.Contains(result.Failure.Message, "typed source registration");
    }

    private static SchemaMethodInfo CreateTypedMethod(Type sourceType, string parameterName)
    {
        var constructor = sourceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var metadata = new SchemaConstructorInfo(constructor, false, (parameterName, constructor.GetParameters()[0].ParameterType))
        {
            StructuralContract = StructuralInputMetadata.CreateContract("source", constructor)
        };
        return new SchemaMethodInfo("source", metadata);
    }

    private sealed class IntSource
    {
        public IntSource(int value) => _ = value;
    }

    private sealed class LongSource
    {
        public LongSource(long value) => _ = value;
    }

    private sealed class FirstCollectionSource
    {
        public FirstCollectionSource(IReadOnlyList<int> values) => _ = values;
    }

    private sealed class SecondCollectionSource
    {
        public SecondCollectionSource(IReadOnlyList<int> values) => _ = values;
    }

    private sealed class RecordRelationSource
    {
        public RecordRelationSource(IReadOnlyList<RelationRecord> rows) => _ = rows;
    }

    private sealed class CrossInterpretationScalarSource
    {
        public CrossInterpretationScalarSource(int value) => _ = value;
    }

    private sealed class CrossInterpretationRelationSource
    {
        public CrossInterpretationRelationSource(IReadOnlyList<RelationRecord> rows) => _ = rows;
    }
    private sealed record RelationRecord(int Id, string Name);

    private sealed class RelationSource(IReadOnlyList<RelationRecord> rows)
    {
        private readonly IReadOnlyList<RelationRecord> _rows = rows;
    }

    private sealed class PrimitiveRelationSource(IReadOnlyList<int> values)
    {
        private readonly IReadOnlyList<int> _values = values;
    }

    private sealed class LegacyCollectionSource(IReadOnlyList<int> values)
    {
        private readonly IReadOnlyList<int> _values = values;
    }

}
