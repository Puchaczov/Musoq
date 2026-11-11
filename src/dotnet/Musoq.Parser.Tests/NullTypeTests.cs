using System;
using System.Globalization;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for NullNode.NullType reflection methods to improve branch coverage.
/// </summary>
[TestClass]
public class NullTypeTests
{
    private static Type NullTypeInstance => NullNode.NullType.Instance;

    #region Basic Properties Tests

    [TestMethod]
    public void NullType_Instance_ReturnsNonNull()
    {
        Assert.IsNotNull(NullTypeInstance);
    }

    [TestMethod]
    public void NullType_Name_ReturnsNull()
    {
        Assert.AreEqual("Null", NullTypeInstance.Name);
    }

    [TestMethod]
    public void NullType_FullName_ReturnsExpected()
    {
        Assert.AreEqual(typeof(NullNode.NullType).FullName, NullTypeInstance.FullName);
    }

    [TestMethod]
    public void NullType_Namespace_ReturnsExpected()
    {
        Assert.AreEqual("Musoq.Parser.Nodes", NullTypeInstance.Namespace);
    }

    [TestMethod]
    public void NullType_Assembly_ReturnsExpected()
    {
        Assert.AreEqual(typeof(NullNode).Assembly, NullTypeInstance.Assembly);
    }

    [TestMethod]
    public void NullType_AssemblyQualifiedName_ReturnsExpected()
    {
        Assert.AreEqual(typeof(object).AssemblyQualifiedName, NullTypeInstance.AssemblyQualifiedName);
    }

    [TestMethod]
    public void NullType_BaseType_ReturnsExpected()
    {
        Assert.IsNull(NullTypeInstance.BaseType);
    }

    [TestMethod]
    public void NullType_GUID_ReturnsNonEmpty()
    {
        Assert.AreNotEqual(Guid.Empty, NullTypeInstance.GUID);
    }

    [TestMethod]
    public void NullType_Module_ReturnsExpected()
    {
        Assert.AreEqual(typeof(NullNode.NullType).Module, NullTypeInstance.Module);
    }

    [TestMethod]
    public void NullType_UnderlyingSystemType_ReturnsExpected()
    {
        Assert.AreEqual(typeof(object), NullTypeInstance.UnderlyingSystemType);
    }

    #endregion

    #region Constructor Tests

    [TestMethod]
    public void GetConstructors_WithPublicBinding_ReturnsConstructors()
    {
        var constructors = NullTypeInstance.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(constructors);
        CollectionAssert.AreEqual(typeof(object).GetConstructors(BindingFlags.Public | BindingFlags.Instance), constructors);
    }

    [TestMethod]
    public void GetConstructors_WithNonPublicBinding_ReturnsConstructors()
    {
        var constructors = NullTypeInstance.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(constructors);
        CollectionAssert.AreEqual(typeof(object).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance), constructors);
    }

    [TestMethod]
    public void GetConstructors_WithStaticBinding_ReturnsEmptyOrConstructors()
    {
        var constructors = NullTypeInstance.GetConstructors(BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(constructors);
        CollectionAssert.AreEqual(typeof(object).GetConstructors(BindingFlags.Static | BindingFlags.NonPublic), constructors);
    }

    #endregion

    #region Field Tests

    [TestMethod]
    public void GetField_WithValidName_ReturnsNullForObject()
    {
        var field = NullTypeInstance.GetField("NonExistent", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNull(field);
    }

    [TestMethod]
    public void GetFields_WithPublicBinding_ReturnsFields()
    {
        var fields = NullTypeInstance.GetFields(BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(fields);
        CollectionAssert.AreEqual(typeof(object).GetFields(BindingFlags.Public | BindingFlags.Instance), fields);
    }

    [TestMethod]
    public void GetFields_WithNonPublicBinding_ReturnsFields()
    {
        var fields = NullTypeInstance.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(fields);
        CollectionAssert.AreEqual(typeof(object).GetFields(BindingFlags.NonPublic | BindingFlags.Instance), fields);
    }

    [TestMethod]
    public void GetFields_WithStaticBinding_ReturnsFields()
    {
        var fields = NullTypeInstance.GetFields(BindingFlags.Static | BindingFlags.Public);
        Assert.IsNotNull(fields);
        CollectionAssert.AreEqual(typeof(object).GetFields(BindingFlags.Static | BindingFlags.Public), fields);
    }

    #endregion

    #region Method Tests

    [TestMethod]
    public void GetMethods_WithPublicBinding_ReturnsMethods()
    {
        var methods = NullTypeInstance.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(methods);
        CollectionAssert.AreEqual(typeof(object).GetMethods(BindingFlags.Public | BindingFlags.Instance), methods);
        Assert.IsNotEmpty(methods);
    }

    [TestMethod]
    public void GetMethods_WithNonPublicBinding_ReturnsMethods()
    {
        var methods = NullTypeInstance.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(methods);
        CollectionAssert.AreEqual(typeof(object).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance), methods);
    }

    [TestMethod]
    public void GetMethods_WithStaticBinding_ReturnsMethods()
    {
        var methods = NullTypeInstance.GetMethods(BindingFlags.Static | BindingFlags.Public);
        Assert.IsNotNull(methods);
        CollectionAssert.AreEqual(typeof(object).GetMethods(BindingFlags.Static | BindingFlags.Public), methods);
    }

    #endregion

    #region Property Tests

    [TestMethod]
    public void GetProperties_WithPublicBinding_ReturnsProperties()
    {
        var properties = NullTypeInstance.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(properties);
        CollectionAssert.AreEqual(typeof(object).GetProperties(BindingFlags.Public | BindingFlags.Instance), properties);
    }

    [TestMethod]
    public void GetProperties_WithNonPublicBinding_ReturnsProperties()
    {
        var properties = NullTypeInstance.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(properties);
        CollectionAssert.AreEqual(typeof(object).GetProperties(BindingFlags.NonPublic | BindingFlags.Instance), properties);
    }

    [TestMethod]
    public void GetProperties_WithStaticBinding_ReturnsProperties()
    {
        var properties = NullTypeInstance.GetProperties(BindingFlags.Static | BindingFlags.Public);
        Assert.IsNotNull(properties);
        CollectionAssert.AreEqual(typeof(object).GetProperties(BindingFlags.Static | BindingFlags.Public), properties);
    }

    #endregion

    #region Member Tests

    [TestMethod]
    public void GetMembers_WithPublicBinding_ReturnsMembers()
    {
        var members = NullTypeInstance.GetMembers(BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(members);
        CollectionAssert.AreEqual(typeof(object).GetMembers(BindingFlags.Public | BindingFlags.Instance), members);
        Assert.IsNotEmpty(members);
    }

    [TestMethod]
    public void GetMembers_WithNonPublicBinding_ReturnsMembers()
    {
        var members = NullTypeInstance.GetMembers(BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(members);
        CollectionAssert.AreEqual(typeof(object).GetMembers(BindingFlags.NonPublic | BindingFlags.Instance), members);
    }

    [TestMethod]
    public void GetMembers_WithAllBinding_ReturnsMembers()
    {
        var members = NullTypeInstance.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                                  BindingFlags.Static);
        Assert.IsNotNull(members);
        CollectionAssert.AreEqual(typeof(object).GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                                  BindingFlags.Static), members);
    }

    #endregion

    #region Event Tests

    [TestMethod]
    public void GetEvent_WithValidName_ReturnsNullForObject()
    {
        var evt = NullTypeInstance.GetEvent("NonExistent", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNull(evt);
    }

    [TestMethod]
    public void GetEvents_WithPublicBinding_ReturnsEvents()
    {
        var events = NullTypeInstance.GetEvents(BindingFlags.Public | BindingFlags.Instance);
        Assert.IsEmpty(events);
    }

    [TestMethod]
    public void GetEvents_WithNonPublicBinding_ReturnsEvents()
    {
        var events = NullTypeInstance.GetEvents(BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsEmpty(events);
    }

    #endregion

    #region Nested Type Tests

    [TestMethod]
    public void GetNestedType_WithValidName_ReturnsNull()
    {
        var nestedType = NullTypeInstance.GetNestedType("NonExistent", BindingFlags.Public);
        Assert.IsNull(nestedType);
    }

    [TestMethod]
    public void GetNestedTypes_WithPublicBinding_ReturnsNestedTypes()
    {
        var nestedTypes = NullTypeInstance.GetNestedTypes(BindingFlags.Public);
        Assert.IsNotNull(nestedTypes);
        CollectionAssert.AreEqual(typeof(object).GetNestedTypes(BindingFlags.Public), nestedTypes);
    }

    [TestMethod]
    public void GetNestedTypes_WithNonPublicBinding_ReturnsNestedTypes()
    {
        var nestedTypes = NullTypeInstance.GetNestedTypes(BindingFlags.NonPublic);
        Assert.IsNotNull(nestedTypes);
        CollectionAssert.AreEqual(typeof(object).GetNestedTypes(BindingFlags.NonPublic), nestedTypes);
    }

    #endregion

    #region Interface Tests

    [TestMethod]
    public void GetInterface_WithValidName_ReturnsNull()
    {
        var iface = NullTypeInstance.GetInterface("NonExistent", false);
        Assert.IsNull(iface);
    }

    [TestMethod]
    public void GetInterface_IgnoreCase_ReturnsNull()
    {
        var iface = NullTypeInstance.GetInterface("NonExistent", true);
        Assert.IsNull(iface);
    }

    [TestMethod]
    public void GetInterfaces_ReturnsInterfaces()
    {
        var interfaces = NullTypeInstance.GetInterfaces();
        Assert.IsEmpty(interfaces);
    }

    #endregion

    #region Custom Attributes Tests

    [TestMethod]
    public void GetCustomAttributes_WithInherit_ReturnsAttributes()
    {
        var attributes = NullTypeInstance.GetCustomAttributes(true);
        Assert.IsNotNull(attributes);
        CollectionAssert.AreEqual(typeof(object).GetCustomAttributes(true), attributes);
    }

    [TestMethod]
    public void GetCustomAttributes_WithoutInherit_ReturnsAttributes()
    {
        var attributes = NullTypeInstance.GetCustomAttributes(false);
        Assert.IsNotNull(attributes);
        CollectionAssert.AreEqual(typeof(object).GetCustomAttributes(false), attributes);
    }

    [TestMethod]
    public void GetCustomAttributes_WithTypeAndInherit_ReturnsAttributes()
    {
        var attributes = NullTypeInstance.GetCustomAttributes(typeof(Attribute), true);
        Assert.IsNotNull(attributes);
        CollectionAssert.AreEqual(typeof(object).GetCustomAttributes(typeof(Attribute), true), attributes);
    }

    [TestMethod]
    public void GetCustomAttributes_WithTypeWithoutInherit_ReturnsAttributes()
    {
        var attributes = NullTypeInstance.GetCustomAttributes(typeof(SerializableAttribute), false);
        Assert.IsNotNull(attributes);
        CollectionAssert.AreEqual(typeof(object).GetCustomAttributes(typeof(SerializableAttribute), false), attributes);
    }

    [TestMethod]
    public void IsDefined_WithValidType_ReturnsFalse()
    {
        var isDefined = NullTypeInstance.IsDefined(typeof(FlagsAttribute), true);
        Assert.IsFalse(isDefined);
    }

    [TestMethod]
    public void IsDefined_WithSerializableType_ReturnsResult()
    {
        var isDefined = NullTypeInstance.IsDefined(typeof(SerializableAttribute), false);

        Assert.AreEqual(typeof(object).IsDefined(typeof(SerializableAttribute), false), isDefined);
    }

    #endregion

    #region Type Implementation Tests

    [TestMethod]
    public void GetElementType_ReturnsNull()
    {
        var elementType = NullTypeInstance.GetElementType();
        Assert.IsNull(elementType);
    }

    [TestMethod]
    public void IsArray_ReturnsFalse()
    {
        Assert.IsFalse(NullTypeInstance.IsArray);
    }

    [TestMethod]
    public void IsByRef_ReturnsFalse()
    {
        Assert.IsFalse(NullTypeInstance.IsByRef);
    }

    [TestMethod]
    public void IsPointer_ReturnsFalse()
    {
        Assert.IsFalse(NullTypeInstance.IsPointer);
    }

    [TestMethod]
    public void IsPrimitive_ReturnsFalse()
    {
        Assert.IsFalse(NullTypeInstance.IsPrimitive);
    }

    [TestMethod]
    public void IsCOMObject_ReturnsFalse()
    {
        Assert.IsFalse(NullTypeInstance.IsCOMObject);
    }

    [TestMethod]
    public void HasElementType_ReturnsFalse()
    {
        Assert.IsFalse(NullTypeInstance.HasElementType);
    }

    [TestMethod]
    public void Attributes_ReturnsExpected()
    {
        var attributes = NullTypeInstance.Attributes;
        Assert.AreEqual(TypeAttributes.Public, attributes & TypeAttributes.Public);
    }

    #endregion

    #region InvokeMember Tests

    [TestMethod]
    public void InvokeMember_ToString_ReturnsResult()
    {
        var obj = new object();
        var result = NullTypeInstance.InvokeMember(
            "ToString",
            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
            null,
            obj,
            [],
            null,
            CultureInfo.InvariantCulture,
            null);

        Assert.AreEqual("System.Object", result);
    }

    [TestMethod]
    public void InvokeMember_GetHashCode_ReturnsResult()
    {
        var obj = new object();
        var result = NullTypeInstance.InvokeMember(
            "GetHashCode",
            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
            null,
            obj,
            [],
            null,
            CultureInfo.InvariantCulture,
            null);

        Assert.AreEqual(obj.GetHashCode(), result);
    }

    [TestMethod]
    public void InvokeMember_GetType_ReturnsResult()
    {
        var obj = new object();
        var result = NullTypeInstance.InvokeMember(
            "GetType",
            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
            null,
            obj,
            [],
            null,
            CultureInfo.InvariantCulture,
            null);

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<Type>(result);
    }

    [TestMethod]
    public void InvokeMember_Equals_ReturnsResult()
    {
        var obj = new object();
        var result = NullTypeInstance.InvokeMember(
            "Equals",
            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
            null,
            obj,
            [obj],
            null,
            CultureInfo.InvariantCulture,
            null);

        Assert.IsTrue((bool)result!);
    }

    #endregion

    #region NullNode Tests

    [TestMethod]
    public void NullNode_DefaultConstructor_SetsNullType()
    {
        var node = new NullNode();
        Assert.IsInstanceOfType<NullNode.NullType>(node.ReturnType);
    }



    [TestMethod]
    public void NullNode_Id_IsNullNode()
    {
        var node = new NullNode();
        Assert.AreEqual("NullNode", node.Id);
    }

    [TestMethod]
    public void NullNode_ToString_ReturnsNull()
    {
        var node = new NullNode();
        Assert.AreEqual("null", node.ToString());
    }

    #endregion
}
