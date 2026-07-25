using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class MethodDeclarationHelperTests
{
    private static string RequireTypeText(ParameterSyntax parameter)
    {
        Assert.IsNotNull(parameter.Type);
        return parameter.Type.ToString();
    }

    private static SyntaxList<AccessorDeclarationSyntax> RequireAccessors(PropertyDeclarationSyntax property)
    {
        Assert.IsNotNull(property.AccessorList);
        return property.AccessorList.Accessors;
    }

    private static BlockSyntax RequireBody(MethodDeclarationSyntax method)
    {
        Assert.IsNotNull(method.Body);
        return method.Body;
    }

    [TestMethod]
    public void CreateStandardParameterList_ReturnsParameterListWithCorrectParameters()
    {
        var parameterList = MethodDeclarationHelper.CreateStandardParameterList();


        Assert.IsNotNull(parameterList);
        Assert.AreEqual(5, parameterList.Parameters.Count);

        var parameters = parameterList.Parameters.ToArray();
        Assert.AreEqual("provider", parameters[0].Identifier.ValueText);
        Assert.AreEqual("sourceRuntimeSettingsBySourceContextId", parameters[1].Identifier.ValueText);
        Assert.AreEqual("sourceExecutionPlans", parameters[2].Identifier.ValueText);
        Assert.AreEqual("logger", parameters[3].Identifier.ValueText);
        Assert.AreEqual("token", parameters[4].Identifier.ValueText);
    }

    [TestMethod]
    public void CreateStandardParameterList_HasCorrectParameterTypes()
    {
        // Act
        var parameterList = MethodDeclarationHelper.CreateStandardParameterList();

        // Assert
        var parameters = parameterList.Parameters.ToArray();


        Assert.Contains("ISchemaProvider", RequireTypeText(parameters[0]));


        Assert.Contains("IReadOnlyDictionary", RequireTypeText(parameters[1]));


        Assert.Contains("IReadOnlyDictionary", RequireTypeText(parameters[2]));


        Assert.Contains("ILogger", RequireTypeText(parameters[3]));


        Assert.Contains("CancellationToken", RequireTypeText(parameters[4]));
    }

    [TestMethod]
    public void CreateStandardPrivateMethod_WithValidInputs_ReturnsCorrectMethodDeclaration()
    {
        // Arrange
        var methodName = "TestMethod";
        var body = SyntaxFactory.Block();

        // Act
        var method = MethodDeclarationHelper.CreateStandardPrivateMethod(methodName, body);

        // Assert
        Assert.IsNotNull(method);
        Assert.AreEqual(methodName, method.Identifier.ValueText);
        Assert.AreEqual(1, method.Modifiers.Count);
        Assert.IsTrue(method.Modifiers[0].IsKind(SyntaxKind.PrivateKeyword));
        Assert.Contains("Table", method.ReturnType.ToString());
        Assert.AreEqual(5, method.ParameterList.Parameters.Count);
        Assert.IsNotNull(method.Body);
        Assert.AreEqual(0, RequireBody(method).Statements.Count);
    }

    [TestMethod]
    public void CreateStandardPrivateMethod_WithNullMethodName_ThrowsArgumentException()
    {
        var body = SyntaxFactory.Block();


        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreateStandardPrivateMethod(null!, body));
    }

    [TestMethod]
    public void CreateStandardPrivateMethod_WithEmptyMethodName_ThrowsArgumentException()
    {
        var body = SyntaxFactory.Block();


        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreateStandardPrivateMethod("", body));
    }

    [TestMethod]
    public void CreateStandardPrivateMethod_WithWhitespaceMethodName_ThrowsArgumentException()
    {
        var body = SyntaxFactory.Block();


        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreateStandardPrivateMethod("   ", body));
    }

    [TestMethod]
    public void CreateStandardPrivateMethod_WithNullBody_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MethodDeclarationHelper.CreateStandardPrivateMethod("TestMethod", null!));
    }

    [TestMethod]
    public void CreatePublicProperty_WithValidInputs_ReturnsCorrectPropertyDeclaration()
    {
        var typeName = "string";
        var propertyName = "TestProperty";


        var property = MethodDeclarationHelper.CreatePublicProperty(typeName, propertyName);


        Assert.IsNotNull(property);
        Assert.AreEqual(propertyName, property.Identifier.ValueText);
        Assert.Contains(typeName, property.Type.ToString());
        Assert.AreEqual(1, property.Modifiers.Count);
        Assert.IsTrue(property.Modifiers[0].IsKind(SyntaxKind.PublicKeyword));
        var accessors = RequireAccessors(property);
        Assert.AreEqual(2, accessors.Count);
        Assert.IsTrue(accessors[0].IsKind(SyntaxKind.GetAccessorDeclaration));
        Assert.IsTrue(accessors[1].IsKind(SyntaxKind.SetAccessorDeclaration));
    }

    [TestMethod]
    public void CreatePublicProperty_WithNullTypeName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreatePublicProperty(null!, "TestProperty"));
    }

    [TestMethod]
    public void CreatePublicProperty_WithEmptyTypeName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreatePublicProperty("", "TestProperty"));
    }

    [TestMethod]
    public void CreatePublicProperty_WithNullPropertyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreatePublicProperty("string", null!));
    }

    [TestMethod]
    public void CreatePublicProperty_WithEmptyPropertyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreatePublicProperty("string", ""));
    }

    [TestMethod]
    public void CreateSourceRuntimeSettingsBySourceContextIdProperty_ReturnsCorrectPropertyDeclaration()
    {
        var property = MethodDeclarationHelper.CreateSourceRuntimeSettingsBySourceContextIdProperty();


        Assert.IsNotNull(property);
        Assert.AreEqual("SourceRuntimeSettingsBySourceContextId", property.Identifier.ValueText);
        Assert.Contains("IReadOnlyDictionary", property.Type.ToString());
        Assert.IsFalse(property.Type.ToString().Contains("uint", StringComparison.Ordinal));
        Assert.Contains("string", property.Type.ToString());
        Assert.AreEqual(1, property.Modifiers.Count);
        Assert.IsTrue(property.Modifiers[0].IsKind(SyntaxKind.PublicKeyword));
        Assert.AreEqual(2, RequireAccessors(property).Count);
    }

    [TestMethod]
    public void CreateSourceExecutionPlansProperty_ReturnsCorrectPropertyDeclaration()
    {
        var property = MethodDeclarationHelper.CreateSourceExecutionPlansProperty();


        Assert.IsNotNull(property);
        Assert.AreEqual("SourceExecutionPlans", property.Identifier.ValueText);
        Assert.Contains("IReadOnlyDictionary", property.Type.ToString());
        Assert.Contains("string", property.Type.ToString());
        Assert.Contains("SourceExecutionPlan", property.Type.ToString());
        Assert.AreEqual(1, property.Modifiers.Count);
        Assert.IsTrue(property.Modifiers[0].IsKind(SyntaxKind.PublicKeyword));
        Assert.AreEqual(2, RequireAccessors(property).Count);
    }

    [TestMethod]
    public void CreateRunMethod_WithValidMethodCallExpression_ReturnsCorrectMethodDeclaration()
    {
        // Arrange
        var methodCallExpression =
            "SomeMethod(Provider, SourceRuntimeSettingsBySourceContextId, SourceExecutionPlans, Logger, token)";

        // Act
        var method = MethodDeclarationHelper.CreateRunMethod(methodCallExpression);

        // Assert
        Assert.IsNotNull(method);
        Assert.AreEqual("Run", method.Identifier.ValueText);
        Assert.AreEqual(1, method.Modifiers.Count);
        Assert.IsTrue(method.Modifiers[0].IsKind(SyntaxKind.PublicKeyword));
        Assert.Contains("Table", method.ReturnType.ToString());
        Assert.AreEqual(1, method.ParameterList.Parameters.Count);
        Assert.AreEqual("token", method.ParameterList.Parameters[0].Identifier.ValueText);
        Assert.Contains("CancellationToken", RequireTypeText(method.ParameterList.Parameters[0]));


        var bodyText = RequireBody(method).ToString();
        Assert.Contains("return", bodyText);
        Assert.Contains(methodCallExpression, bodyText);
    }

    [TestMethod]
    public void CreateRunMethod_WithNullMethodCallExpression_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreateRunMethod(null!));
    }

    [TestMethod]
    public void CreateRunMethod_WithEmptyMethodCallExpression_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreateRunMethod(""));
    }

    [TestMethod]
    public void CreateRunMethod_WithWhitespaceMethodCallExpression_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreateRunMethod("   "));
    }

    [TestMethod]
    public void CreateOnDataSourceProgressMethod_ReturnsAggressivelyInlinedHelper()
    {
        var method = MethodDeclarationHelper.CreateOnDataSourceProgressMethod();
        var methodText = method.NormalizeWhitespace().ToFullString();

        Assert.AreEqual("OnDataSourceProgress", method.Identifier.ValueText);
        Assert.Contains(
            "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]",
            methodText);
    }

    [TestMethod]
    public void CreateOnPhaseChangedMethod_ReturnsAggressivelyInlinedHelper()
    {
        var method = MethodDeclarationHelper.CreateOnPhaseChangedMethod();
        var methodText = method.NormalizeWhitespace().ToFullString();

        Assert.AreEqual("OnPhaseChanged", method.Identifier.ValueText);
        Assert.Contains(
            "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]",
            methodText);
    }

    [TestMethod]
    public void ComplexParameterTypes_AreCorrectlyGenerated()
    {
        var parameterList = MethodDeclarationHelper.CreateStandardParameterList();
        var posProperty = MethodDeclarationHelper.CreateSourceRuntimeSettingsBySourceContextIdProperty();
        var queriesProperty = MethodDeclarationHelper.CreateSourceExecutionPlansProperty();


        var posParam = parameterList.Parameters[1];
        Assert.AreEqual(RequireTypeText(posParam), posProperty.Type.ToString());

        var queriesParam = parameterList.Parameters[2];

        Assert.Contains("IReadOnlyDictionary", RequireTypeText(queriesParam));
        Assert.Contains("IReadOnlyDictionary", queriesProperty.Type.ToString());
    }

    [TestMethod]
    public void AllMethods_ProduceValidSyntax()
    {
        var parameterList = MethodDeclarationHelper.CreateStandardParameterList();
        var method = MethodDeclarationHelper.CreateStandardPrivateMethod("TestMethod", SyntaxFactory.Block());
        var property1 = MethodDeclarationHelper.CreatePublicProperty("string", "TestProperty");
        var property2 = MethodDeclarationHelper.CreateSourceRuntimeSettingsBySourceContextIdProperty();
        var property3 = MethodDeclarationHelper.CreateSourceExecutionPlansProperty();
        var runMethod = MethodDeclarationHelper.CreateRunMethod("TestCall()");
        var onDataSourceProgressMethod = MethodDeclarationHelper.CreateOnDataSourceProgressMethod();
        var onPhaseChangedMethod = MethodDeclarationHelper.CreateOnPhaseChangedMethod();


        Assert.IsFalse(parameterList.ContainsDiagnostics);
        Assert.IsFalse(method.ContainsDiagnostics);
        Assert.IsFalse(property1.ContainsDiagnostics);
        Assert.IsFalse(property2.ContainsDiagnostics);
        Assert.IsFalse(property3.ContainsDiagnostics);
        Assert.IsFalse(runMethod.ContainsDiagnostics);
        Assert.IsFalse(onDataSourceProgressMethod.ContainsDiagnostics);
        Assert.IsFalse(onPhaseChangedMethod.ContainsDiagnostics);
    }
}
