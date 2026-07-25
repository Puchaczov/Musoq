using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Plugins;
using Musoq.Schema.Optimization;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionPortableSymbolFactoryTests
{
    [TestMethod]
    public void FromType_WhenPrimitive_ShouldUseStablePrimitiveSymbol()
    {
        var symbol = ExecutionPortableSymbolFactory.FromType(typeof(int));

        Assert.AreEqual(ExecutionPortableTypeKind.Primitive, symbol.Kind);
        Assert.AreEqual("primitive:int32", symbol.StableName);
        Assert.AreEqual("int32", symbol.DisplayName);
        Assert.AreEqual(ExecutionPortableSymbolPortability.Portable, symbol.Portability);
        StringAssert.Contains(symbol.PortabilityReason, "primitive");
    }

    [TestMethod]
    public void FromType_WhenNullable_ShouldUseStableNullableSymbol()
    {
        var symbol = ExecutionPortableSymbolFactory.FromType(typeof(int?));

        Assert.AreEqual(ExecutionPortableTypeKind.Nullable, symbol.Kind);
        Assert.AreEqual("nullable<primitive:int32>", symbol.StableName);
        Assert.AreEqual(ExecutionPortableTypeKind.Primitive, symbol.Arguments[0].Kind);
        Assert.AreEqual(ExecutionPortableSymbolPortability.Portable, symbol.Portability);
    }

    [TestMethod]
    public void FromType_WhenArray_ShouldUseStableArraySymbol()
    {
        var symbol = ExecutionPortableSymbolFactory.FromType(typeof(string[]));

        Assert.AreEqual(ExecutionPortableTypeKind.Array, symbol.Kind);
        Assert.AreEqual("array:1<primitive:string>", symbol.StableName);
        Assert.AreEqual(1, symbol.ArrayRank);
        Assert.AreEqual(ExecutionPortableTypeKind.Primitive, symbol.Arguments[0].Kind);
        Assert.AreEqual(ExecutionPortableSymbolPortability.Portable, symbol.Portability);
    }

    [TestMethod]
    public void FromType_WhenGeneric_ShouldUseStableGenericSymbol()
    {
        var symbol = ExecutionPortableSymbolFactory.FromType(typeof(Dictionary<string, int?>));

        Assert.AreEqual(ExecutionPortableTypeKind.Map, symbol.Kind);
        Assert.AreEqual(
            "map<primitive:string,nullable<primitive:int32>>",
            symbol.StableName);
        Assert.AreEqual(2, symbol.Arguments.Count);
        Assert.AreEqual(ExecutionPortableSymbolPortability.Portable, symbol.Portability);
        Assert.IsNotNull(symbol.Container);
        Assert.AreEqual(ExecutionPortableContainerKind.Map, symbol.Container.Kind);
        Assert.IsTrue(symbol.Container.RequiresKeyEquality);
        Assert.IsTrue(symbol.Container.RequiresKeyHashing);
    }

    [TestMethod]
    public void FromType_WhenGenericDefinitionIsNotCataloged_ShouldMarkClrOnly()
    {
        var symbol = ExecutionPortableSymbolFactory.FromType(typeof(Lazy<int>));

        Assert.AreEqual(ExecutionPortableTypeKind.ClrOnly, symbol.Kind);
        Assert.AreEqual(ExecutionPortableSymbolPortability.ClrOnly, symbol.Portability);
        StringAssert.Contains(symbol.PortabilityReason, "No portable container or host contract");
    }

    [TestMethod]
    public void FromType_WhenPortableGenericHasClrOnlyArgument_ShouldMarkClrOnlyWithArgumentReason()
    {
        var symbol = ExecutionPortableSymbolFactory.FromType(typeof(List<Uri>));

        Assert.AreEqual(ExecutionPortableTypeKind.List, symbol.Kind);
        Assert.AreEqual(ExecutionPortableSymbolPortability.ClrOnly, symbol.Portability);
        StringAssert.Contains(symbol.PortabilityReason, "non-portable argument");
        StringAssert.Contains(symbol.PortabilityReason, "System.Uri");
    }

    [TestMethod]
    public void FromType_WhenObject_ShouldUseObjectFallbackSymbol()
    {
        var symbol = ExecutionPortableSymbolFactory.FromType(typeof(object));

        Assert.AreEqual(ExecutionPortableTypeKind.HostOpaque, symbol.Kind);
        Assert.AreEqual("host-opaque:dynamic-object", symbol.StableName);
        Assert.AreEqual(ExecutionPortableSymbolPortability.HostImport, symbol.Portability);
    }

    [TestMethod]
    public void FromType_WhenKnownHostRuntimeType_ShouldMarkHostImport()
    {
        var symbol = ExecutionPortableSymbolFactory.FromType(typeof(SourceExecutionPlan));

        Assert.AreEqual(ExecutionPortableTypeKind.HostOpaque, symbol.Kind);
        Assert.AreEqual(ExecutionPortableSymbolPortability.HostImport, symbol.Portability);
        StringAssert.Contains(symbol.PortabilityReason, "host runtime type");
    }

    [TestMethod]
    public void GeneratedRow_WhenTypeNameIsProvided_ShouldUseStableGeneratedRowSymbol()
    {
        var fields = new[]
        {
            new ExecutionPortableRowFieldDescriptor(
                "Value",
                ExecutionPortableSymbolFactory.FromType(typeof(int)),
                FieldNullability.NotNullable.ToString())
        };
        var symbol = ExecutionPortableSymbolFactory.GeneratedRow("ResultRow0", fields);
        var repeated = ExecutionPortableSymbolFactory.GeneratedRow("RenamedResultRow", fields);

        Assert.AreEqual(ExecutionPortableTypeKind.GeneratedRow, symbol.Kind);
        StringAssert.StartsWith(symbol.StableName, "generated-row:sha256:");
        Assert.AreEqual(symbol.StableName, repeated.StableName);
        Assert.AreEqual("ResultRow0", symbol.DisplayName);
        Assert.AreEqual(ExecutionPortableSymbolPortability.Portable, symbol.Portability);
        Assert.HasCount(1, symbol.Fields);
        Assert.AreEqual("Value", symbol.Fields[0].Name);
        Assert.AreEqual("primitive:int32", symbol.Fields[0].Type.StableName);
        Assert.AreEqual(FieldNullability.NotNullable.ToString(), symbol.Fields[0].Nullability);
    }

    [TestMethod]
    public void ExecutionTypeRef_ShouldExposePortableDescriptorWithoutExposingClrType()
    {
        var typeRef = ExecutionClrBindingFactory.FromClr(typeof(int?));

        Assert.AreEqual("nullable<primitive:int32>", typeRef.Descriptor.StableName);
        Assert.AreEqual(ExecutionPortableTypeKind.Nullable, typeRef.Descriptor.Kind);
        Assert.AreEqual(ExecutionPortableSymbolPortability.Portable, typeRef.Descriptor.Portability);
        var clrType = typeof(ExecutionTypeRef).GetProperty(
            "ClrType",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNull(clrType);
    }

    [TestMethod]
    public void FromMethod_WhenPluginMethodIsProvided_ShouldUseStableCallableSymbol()
    {
        var method = typeof(LibraryBase)
            .GetMethod(nameof(LibraryBase.ToUpper), BindingFlags.Public | BindingFlags.Instance, [typeof(string)]) ??
            throw new InvalidOperationException("Could not resolve LibraryBase.ToUpper.");

        var symbol = ExecutionPortableSymbolFactory.FromMethod(method);

        Assert.AreEqual(ExecutionPortableCallableKind.HostPlugin, symbol.Kind);
        Assert.AreEqual(nameof(LibraryBase.ToUpper), symbol.MethodName);
        Assert.AreEqual("Musoq.Plugins.LibraryBase.ToUpper", symbol.DisplayName);
        Assert.AreEqual("primitive:string", symbol.ReturnType!.StableName);
        Assert.AreEqual("primitive:string", symbol.ParameterTypes.Single().StableName);
        Assert.Contains("Musoq.Plugins.LibraryBase", symbol.StableName);
        Assert.DoesNotContain(", Version=", symbol.StableName);
        Assert.AreEqual(ExecutionPortableSymbolPortability.HostImport, symbol.Portability);
        StringAssert.Contains(symbol.PortabilityReason, "plugin library callable");
    }

    [TestMethod]
    public void FromMethod_WhenAggregateMethodIsProvided_ShouldUseHostImportCallableSymbol()
    {
        var method = typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(static method => method
                .GetCustomAttributes(inherit: false)
                .Any(static attribute => attribute.GetType().FullName == "Musoq.Plugins.Attributes.AggregateFunctionAttribute")) ??
            throw new InvalidOperationException("Could not resolve aggregate method on LibraryBase.");

        var symbol = ExecutionPortableSymbolFactory.FromMethod(method);

        Assert.AreEqual(ExecutionPortableCallableKind.HostAggregate, symbol.Kind);
        Assert.AreEqual(ExecutionPortableSymbolPortability.HostImport, symbol.Portability);
    }

    [TestMethod]
    public void FromType_WhenUnknownClrType_ShouldMarkClrOnly()
    {
        var symbol = ExecutionPortableSymbolFactory.FromType(typeof(Uri));

        Assert.AreEqual(ExecutionPortableTypeKind.ClrOnly, symbol.Kind);
        Assert.AreEqual(ExecutionPortableSymbolPortability.ClrOnly, symbol.Portability);
        StringAssert.Contains(symbol.StableName, "clr:System.Uri@");
        Assert.DoesNotContain(", Version=", symbol.StableName);
        StringAssert.Contains(symbol.PortabilityReason, "No portable catalog entry");
    }

    [TestMethod]
    public void FromMethod_WhenUnknownClrMethod_ShouldMarkClrOnly()
    {
        var method = typeof(string)
            .GetMethod(nameof(string.Contains), BindingFlags.Public | BindingFlags.Instance, [typeof(string)]) ??
            throw new InvalidOperationException("Could not resolve string.Contains.");

        var symbol = ExecutionPortableSymbolFactory.FromMethod(method);

        Assert.AreEqual(ExecutionPortableCallableKind.ClrMethod, symbol.Kind);
        Assert.AreEqual(ExecutionPortableSymbolPortability.ClrOnly, symbol.Portability);
        StringAssert.Contains(symbol.PortabilityReason, "No portable callable catalog entry");
    }
}
