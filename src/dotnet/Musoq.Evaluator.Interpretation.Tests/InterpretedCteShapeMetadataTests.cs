using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class InterpretedCteShapeMetadataTests
{
    [TestMethod]
    [DataRow(typeof(byte[]))]
    [DataRow(typeof(string[]))]
    public void ConcreteArrayIndex_ShouldNotInheritAnEnclosingRecordName(Type arrayType)
    {
        var node = new AccessObjectArrayNode(new Musoq.Parser.Tokens.NumericAccessToken("Values", "0", default),
            arrayType, "s", "Musoq.Generated.Interpreters.Item");
        var expression = (ArrayAccess)new ExpressionConverter().Convert(node);
        Assert.IsNull(((ColumnRef)expression.Array).GeneratedTypeName);
    }

    [TestMethod]
    public void TypedArrayKernel_ShouldHaveNoBoxingInstructions()
    {
        var method = typeof(SafeArrayAccess).GetMethod(nameof(SafeArrayAccess.GetArrayElement))!
            .MakeGenericMethod(typeof(string));
        Assert.IsFalse(GeneratedExecutionIlInspection.ContainsBoxOpcode(method));
    }

    [TestMethod]
    public void DecodedColumn_WhenConvertedToLogicalProjection_ShouldRetainItsSchemaIdentity()
    {
        const string typeName = "Musoq.Generated.Interpreters.Inner";
        var node = new AccessColumnNode("Inner", "s", typeof(object), default, typeName);
        var expression = new ExpressionConverter().Convert(node);
        var schema = OutputSchemaFactory.ForProjection([new ProjectedField("Renamed", expression, 0)]);
        Assert.AreEqual(typeName, schema.Columns[0].IntendedTypeName);
    }

    [TestMethod]
    public void RenamedCteEnumColumn_ShouldKeepIdentityAndSourceType()
    {
        var descriptor = new EnumTypeDescriptor("State", EnumTypeOrigin.QueryLocal, EnumUnderlyingKind.Int32,
            false, [new EnumMemberDescriptor("Ready", EnumScalarValue.FromInt32(1))]);
        var column = new SchemaColumn("State", 0, typeof(int), typeof(int), descriptor);
        var renamed = StructuralProjectionColumns.Rename(column, "Renamed");
        Assert.AreSame(descriptor, renamed.EnumType);
        Assert.AreEqual(typeof(int), renamed.SourceReadType);
    }

    [TestMethod]
    public void RenamedCteInterpretedColumn_ShouldKeepGeneratedTypeAndStability()
    {
        var column = new SchemaColumn("Inner", 0, typeof(object), "Musoq.Generated.Interpreters.Inner", ColumnStability.Volatile);
        var renamed = StructuralProjectionColumns.Rename(column, "Renamed");
        Assert.AreEqual("Musoq.Generated.Interpreters.Inner", renamed.IntendedTypeName);
        Assert.AreEqual(ColumnStability.Volatile, renamed.Stability);
    }
}
