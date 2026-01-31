using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class MethodDeclarationHelperTests
{
    [TestMethod]
    public void CreateStandardParameterList_ReturnsParameterListWithCorrectParameters()
    {
        var parameterList = MethodDeclarationHelper.CreateStandardParameterList();


        Assert.IsNotNull(parameterList);
        Assert.AreEqual(5, parameterList.Parameters.Count);

        var parameters = parameterList.Parameters.ToArray();
        Assert.AreEqual("provider", parameters[0].Identifier.ValueText);
        Assert.AreEqual("positionalEnvironmentVariables", parameters[1].Identifier.ValueText);
        Assert.AreEqual("queriesInformation", parameters[2].Identifier.ValueText);
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


        Assert.Contains("ISchemaProvider", parameters[0].Type.ToString());


        Assert.Contains("IReadOnlyDictionary", parameters[1].Type.ToString());


        Assert.Contains("IReadOnlyDictionary", parameters[2].Type.ToString());


        Assert.Contains("ILogger", parameters[3].Type.ToString());


        Assert.Contains("CancellationToken", parameters[4].Type.ToString());
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
        Assert.AreEqual(0, method.Body.Statements.Count);
    }

    [TestMethod]
    public void CreateStandardPrivateMethod_WithNullMethodName_ThrowsArgumentException()
    {
        var body = SyntaxFactory.Block();


        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreateStandardPrivateMethod(null, body));
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
            MethodDeclarationHelper.CreateStandardPrivateMethod("TestMethod", null));
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
        Assert.AreEqual(2, property.AccessorList.Accessors.Count);
        Assert.IsTrue(property.AccessorList.Accessors[0].IsKind(SyntaxKind.GetAccessorDeclaration));
        Assert.IsTrue(property.AccessorList.Accessors[1].IsKind(SyntaxKind.SetAccessorDeclaration));
    }

    [TestMethod]
    public void CreatePublicProperty_WithNullTypeName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreatePublicProperty(null, "TestProperty"));
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
            MethodDeclarationHelper.CreatePublicProperty("string", null));
    }

    [TestMethod]
    public void CreatePublicProperty_WithEmptyPropertyName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreatePublicProperty("string", ""));
    }

    [TestMethod]
    public void CreatePositionalEnvironmentVariablesProperty_ReturnsCorrectPropertyDeclaration()
    {
        var property = MethodDeclarationHelper.CreatePositionalEnvironmentVariablesProperty();


        Assert.IsNotNull(property);
        Assert.AreEqual("PositionalEnvironmentVariables", property.Identifier.ValueText);
        Assert.Contains("IReadOnlyDictionary", property.Type.ToString());
        Assert.Contains("uint", property.Type.ToString());
        Assert.Contains("string", property.Type.ToString());
        Assert.AreEqual(1, property.Modifiers.Count);
        Assert.IsTrue(property.Modifiers[0].IsKind(SyntaxKind.PublicKeyword));
        Assert.AreEqual(2, property.AccessorList.Accessors.Count);
    }

    [TestMethod]
    public void CreateQueriesInformationProperty_ReturnsCorrectPropertyDeclaration()
    {
        var property = MethodDeclarationHelper.CreateQueriesInformationProperty();


        Assert.IsNotNull(property);
        Assert.AreEqual("QueriesInformation", property.Identifier.ValueText);
        Assert.Contains("IReadOnlyDictionary", property.Type.ToString());
        Assert.Contains("string", property.Type.ToString());
        Assert.Contains("QuerySourceInfo", property.Type.ToString());
        Assert.AreEqual(1, property.Modifiers.Count);
        Assert.IsTrue(property.Modifiers[0].IsKind(SyntaxKind.PublicKeyword));
        Assert.AreEqual(2, property.AccessorList.Accessors.Count);
    }

    [TestMethod]
    public void CreateRunMethod_WithValidMethodCallExpression_ReturnsCorrectMethodDeclaration()
    {
        // Arrange
        var methodCallExpression =
            "SomeMethod(Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, token)";

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
        Assert.Contains("CancellationToken", method.ParameterList.Parameters[0].Type.ToString());


        var bodyText = method.Body.ToString();
        Assert.Contains("return", bodyText);
        Assert.Contains(methodCallExpression, bodyText);
    }

    [TestMethod]
    public void CreateRunMethod_WithNullMethodCallExpression_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            MethodDeclarationHelper.CreateRunMethod(null));
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
    public void ComplexParameterTypes_AreCorrectlyGenerated()
    {
        var parameterList = MethodDeclarationHelper.CreateStandardParameterList();
        var posProperty = MethodDeclarationHelper.CreatePositionalEnvironmentVariablesProperty();
        var queriesProperty = MethodDeclarationHelper.CreateQueriesInformationProperty();


        var posParam = parameterList.Parameters[1];
        Assert.AreEqual(posParam.Type.ToString(), posProperty.Type.ToString());

        var queriesParam = parameterList.Parameters[2];

        Assert.Contains("IReadOnlyDictionary", queriesParam.Type.ToString());
        Assert.Contains("IReadOnlyDictionary", queriesProperty.Type.ToString());
    }

    [TestMethod]
    public void AllMethods_ProduceValidSyntax()
    {
        var parameterList = MethodDeclarationHelper.CreateStandardParameterList();
        var method = MethodDeclarationHelper.CreateStandardPrivateMethod("TestMethod", SyntaxFactory.Block());
        var property1 = MethodDeclarationHelper.CreatePublicProperty("string", "TestProperty");
        var property2 = MethodDeclarationHelper.CreatePositionalEnvironmentVariablesProperty();
        var property3 = MethodDeclarationHelper.CreateQueriesInformationProperty();
        var runMethod = MethodDeclarationHelper.CreateRunMethod("TestCall()");


        Assert.IsFalse(parameterList.ContainsDiagnostics);
        Assert.IsFalse(method.ContainsDiagnostics);
        Assert.IsFalse(property1.ContainsDiagnostics);
        Assert.IsFalse(property2.ContainsDiagnostics);
        Assert.IsFalse(property3.ContainsDiagnostics);
        Assert.IsFalse(runMethod.ContainsDiagnostics);
    }
}
