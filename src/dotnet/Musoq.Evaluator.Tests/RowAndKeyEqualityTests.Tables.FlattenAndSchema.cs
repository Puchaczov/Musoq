using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Tests;

public partial class RowAndKeyEqualityTests
{
    #region FlattenContexts Tests

    [TestMethod]
    public void FlattenContexts_WhenSingleContext_ReturnsContextDirectly()
    {
        var context = new object[] { 1, "hello", 3.14 };

        var result = EvaluationHelper.FlattenContexts(context);

        Assert.AreSame(context, result);
    }

    [TestMethod]
    public void FlattenContexts_WhenSingleNullContext_ReturnsArrayWithNull()
    {
        var result = EvaluationHelper.FlattenContexts([null]);

        Assert.IsNotNull(result);
        Assert.HasCount(1, result);
        Assert.IsNull(result[0]);
    }

    [TestMethod]
    public void FlattenContexts_WhenSingleContextIsNull_ReturnsArrayWithNull()
    {
        var result = EvaluationHelper.FlattenContexts((object?[]?)null);

        Assert.IsNotNull(result);
        Assert.HasCount(1, result);
        Assert.IsNull(result[0]);
    }

    [TestMethod]
    public void FlattenContexts_WhenMultipleContexts_ReturnsFlattenedArray()
    {
        var ctx1 = new object[] { 1, 2 };
        var ctx2 = new object[] { 3, 4, 5 };

        var result = EvaluationHelper.FlattenContexts(ctx1, ctx2);

        CollectionAssert.AreEqual(new object[] { 1, 2, 3, 4, 5 }, result);
    }

    [TestMethod]
    public void FlattenContexts_WhenMultipleContextsWithNull_InsertsNullForMissing()
    {
        var ctx1 = new object[] { 1, 2 };
        object?[]? ctx2 = null;

        var result = EvaluationHelper.FlattenContexts(ctx1, ctx2);

        Assert.HasCount(3, result);
        Assert.AreEqual(1, result[0]);
        Assert.AreEqual(2, result[1]);
        Assert.IsNull(result[2]);
    }

    #endregion

    #region SchemaRegistry Tests

    [TestMethod]
    public void SchemaRegistry_Register_AddsSchema()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");

        registry.Register("test", node);

        Assert.AreEqual(1, registry.Count);
        Assert.IsTrue(registry.ContainsSchema("test"));
    }

    [TestMethod]
    public void SchemaRegistry_Register_NullName_Throws()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");

        Assert.Throws<ArgumentNullException>(() => registry.Register(null!, node));
    }

    [TestMethod]
    public void SchemaRegistry_Register_EmptyName_Throws()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");

        Assert.Throws<ArgumentNullException>(() => registry.Register("", node));
    }

    [TestMethod]
    public void SchemaRegistry_Register_NullNode_Throws()
    {
        var registry = new SchemaRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register("test", null!));
    }

    [TestMethod]
    public void SchemaRegistry_Register_DuplicateName_Throws()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");

        registry.Register("test", node);

        Assert.Throws<InvalidOperationException>(() => registry.Register("test", node));
    }

    [TestMethod]
    public void SchemaRegistry_TryGetSchema_ReturnsTrue_WhenFound()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("test", node);

        var result = registry.TryGetSchema("test", out var registration);

        Assert.IsTrue(result);
        Assert.IsNotNull(registration);
        Assert.AreEqual("test", registration.Name);
    }

    [TestMethod]
    public void SchemaRegistry_TryGetSchema_ReturnsFalse_WhenNotFound()
    {
        var registry = new SchemaRegistry();

        var result = registry.TryGetSchema("nonexistent", out var registration);

        Assert.IsFalse(result);
        Assert.IsNull(registration);
    }

    [TestMethod]
    public void SchemaRegistry_GetSchema_ReturnsSchema_WhenFound()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("test", node);

        var registration = registry.GetSchema("test");

        Assert.IsNotNull(registration);
        Assert.AreEqual("test", registration.Name);
    }

    [TestMethod]
    public void SchemaRegistry_GetSchema_Throws_WhenNotFound()
    {
        var registry = new SchemaRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.GetSchema("nonexistent"));
    }

    [TestMethod]
    public void SchemaRegistry_Clear_RemovesAllSchemas()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("test1", node);
        registry.Register("test2", node);

        registry.Clear();

        Assert.AreEqual(0, registry.Count);
    }

    [TestMethod]
    public void SchemaRegistry_ValidateReference_Throws_WhenSchemaNotFound()
    {
        var registry = new SchemaRegistry();

        Assert.Throws<InvalidOperationException>(() =>
            registry.ValidateReference("nonexistent", "referencing"));
    }

    [TestMethod]
    public void SchemaRegistry_ValidateReference_Throws_WhenReferencedAfterReferencing()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("referencing", node);
        registry.Register("referenced", node);

        Assert.Throws<InvalidOperationException>(() =>
            registry.ValidateReference("referenced", "referencing"));
    }

    [TestMethod]
    public void SchemaRegistry_ValidateReference_DoesNotThrow_WhenValid()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("referenced", node);
        registry.Register("referencing", node);


        registry.ValidateReference("referenced", "referencing");
    }

    [TestMethod]
    public void SchemaRegistry_Schemas_ReturnsInOrder()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("first", node);
        registry.Register("second", node);
        registry.Register("third", node);

        var schemas = registry.Schemas;

        Assert.HasCount(3, schemas);
        Assert.AreEqual("first", schemas[0].Name);
        Assert.AreEqual("second", schemas[1].Name);
        Assert.AreEqual("third", schemas[2].Name);
    }

    #endregion

    #region SchemaRegistration Tests

    [TestMethod]
    public void SchemaRegistration_NullName_Throws()
    {
        var node = new IntegerNode("1");

        Assert.Throws<ArgumentNullException>(() => new SchemaRegistration(null!, node));
    }

    [TestMethod]
    public void SchemaRegistration_NullNode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SchemaRegistration("test", null!));
    }

    [TestMethod]
    public void SchemaRegistration_IsBinarySchema_ReturnsFalse_ForNonBinaryNode()
    {
        var node = new IntegerNode("1");
        var registration = new SchemaRegistration("test", node);

        Assert.IsFalse(registration.IsBinarySchema);
    }

    [TestMethod]
    public void SchemaRegistration_IsTextSchema_ReturnsFalse_ForNonTextNode()
    {
        var node = new IntegerNode("1");
        var registration = new SchemaRegistration("test", node);

        Assert.IsFalse(registration.IsTextSchema);
    }

    [TestMethod]
    public void SchemaRegistration_GeneratedType_CanBeSet()
    {
        var node = new IntegerNode("1");
        var registration = new SchemaRegistration("test", node) { GeneratedType = typeof(string) };

        Assert.AreEqual(typeof(string), registration.GeneratedType);
    }

    [TestMethod]
    public void SchemaRegistration_GeneratedTypeName_CanBeSet()
    {
        var node = new IntegerNode("1");
        var registration = new SchemaRegistration("test", node) { GeneratedTypeName = "MyNamespace.MyType" };

        Assert.AreEqual("MyNamespace.MyType", registration.GeneratedTypeName);
    }

    #endregion
}
