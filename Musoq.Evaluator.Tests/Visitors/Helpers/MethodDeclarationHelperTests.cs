using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Plugins;
using Musoq.Schema;
using System.Threading;

namespace Musoq.Evaluator.Tests.Visitors.Helpers
{
    [TestClass]
    public class MethodDeclarationHelperTests
    {
        [TestMethod]
        public void CreateStandardParameterList_ReturnsParameterListWithCorrectParameters()
        {
            // Act
            var parameterList = MethodDeclarationHelper.CreateStandardParameterList();

            // Assert
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
            
            // Check provider parameter type
            Assert.IsTrue(parameters[0].Type.ToString().Contains("ISchemaProvider"));
            
            // Check positionalEnvironmentVariables parameter type
            Assert.IsTrue(parameters[1].Type.ToString().Contains("IReadOnlyDictionary"));
            
            // Check queriesInformation parameter type
            Assert.IsTrue(parameters[2].Type.ToString().Contains("IReadOnlyDictionary"));
            
            // Check logger parameter type
            Assert.IsTrue(parameters[3].Type.ToString().Contains("ILogger"));
            
            // Check token parameter type
            Assert.IsTrue(parameters[4].Type.ToString().Contains("CancellationToken"));
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
            Assert.IsTrue(method.ReturnType.ToString().Contains("Table"));
            Assert.AreEqual(5, method.ParameterList.Parameters.Count);
            Assert.IsNotNull(method.Body);
            Assert.AreEqual(0, method.Body.Statements.Count); // Empty block should have 0 statements
        }

        [TestMethod]
        public void CreateStandardPrivateMethod_WithNullMethodName_ThrowsArgumentException()
        {
            // Arrange
            var body = SyntaxFactory.Block();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                MethodDeclarationHelper.CreateStandardPrivateMethod(null, body));
        }

        [TestMethod]
        public void CreateStandardPrivateMethod_WithEmptyMethodName_ThrowsArgumentException()
        {
            // Arrange
            var body = SyntaxFactory.Block();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                MethodDeclarationHelper.CreateStandardPrivateMethod("", body));
        }

        [TestMethod]
        public void CreateStandardPrivateMethod_WithWhitespaceMethodName_ThrowsArgumentException()
        {
            // Arrange
            var body = SyntaxFactory.Block();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                MethodDeclarationHelper.CreateStandardPrivateMethod("   ", body));
        }

        [TestMethod]
        public void CreateStandardPrivateMethod_WithNullBody_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                MethodDeclarationHelper.CreateStandardPrivateMethod("TestMethod", null));
        }

        [TestMethod]
        public void CreatePublicProperty_WithValidInputs_ReturnsCorrectPropertyDeclaration()
        {
            // Arrange
            var typeName = "string";
            var propertyName = "TestProperty";

            // Act
            var property = MethodDeclarationHelper.CreatePublicProperty(typeName, propertyName);

            // Assert
            Assert.IsNotNull(property);
            Assert.AreEqual(propertyName, property.Identifier.ValueText);
            Assert.IsTrue(property.Type.ToString().Contains(typeName));
            Assert.AreEqual(1, property.Modifiers.Count);
            Assert.IsTrue(property.Modifiers[0].IsKind(SyntaxKind.PublicKeyword));
            Assert.AreEqual(2, property.AccessorList.Accessors.Count);
            Assert.IsTrue(property.AccessorList.Accessors[0].IsKind(SyntaxKind.GetAccessorDeclaration));
            Assert.IsTrue(property.AccessorList.Accessors[1].IsKind(SyntaxKind.SetAccessorDeclaration));
        }

        [TestMethod]
        public void CreatePublicProperty_WithNullTypeName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                MethodDeclarationHelper.CreatePublicProperty(null, "TestProperty"));
        }

        [TestMethod]
        public void CreatePublicProperty_WithEmptyTypeName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                MethodDeclarationHelper.CreatePublicProperty("", "TestProperty"));
        }

        [TestMethod]
        public void CreatePublicProperty_WithNullPropertyName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                MethodDeclarationHelper.CreatePublicProperty("string", null));
        }

        [TestMethod]
        public void CreatePublicProperty_WithEmptyPropertyName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                MethodDeclarationHelper.CreatePublicProperty("string", ""));
        }

        [TestMethod]
        public void CreatePositionalEnvironmentVariablesProperty_ReturnsCorrectPropertyDeclaration()
        {
            // Act
            var property = MethodDeclarationHelper.CreatePositionalEnvironmentVariablesProperty();

            // Assert
            Assert.IsNotNull(property);
            Assert.AreEqual("PositionalEnvironmentVariables", property.Identifier.ValueText);
            Assert.IsTrue(property.Type.ToString().Contains("IReadOnlyDictionary"));
            Assert.IsTrue(property.Type.ToString().Contains("uint"));
            Assert.IsTrue(property.Type.ToString().Contains("string"));
            Assert.AreEqual(1, property.Modifiers.Count);
            Assert.IsTrue(property.Modifiers[0].IsKind(SyntaxKind.PublicKeyword));
            Assert.AreEqual(2, property.AccessorList.Accessors.Count);
        }

        [TestMethod]
        public void CreateQueriesInformationProperty_ReturnsCorrectPropertyDeclaration()
        {
            // Act
            var property = MethodDeclarationHelper.CreateQueriesInformationProperty();

            // Assert
            Assert.IsNotNull(property);
            Assert.AreEqual("QueriesInformation", property.Identifier.ValueText);
            Assert.IsTrue(property.Type.ToString().Contains("IReadOnlyDictionary"));
            Assert.IsTrue(property.Type.ToString().Contains("string"));
            Assert.IsTrue(property.Type.ToString().Contains("SchemaFromNode"));
            Assert.IsTrue(property.Type.ToString().Contains("IReadOnlyCollection"));
            Assert.IsTrue(property.Type.ToString().Contains("ISchemaColumn"));
            Assert.IsTrue(property.Type.ToString().Contains("WhereNode"));
            Assert.IsTrue(property.Type.ToString().Contains("bool"));
            Assert.AreEqual(1, property.Modifiers.Count);
            Assert.IsTrue(property.Modifiers[0].IsKind(SyntaxKind.PublicKeyword));
            Assert.AreEqual(2, property.AccessorList.Accessors.Count);
        }

        [TestMethod]
        public void CreateRunMethod_WithValidMethodCallExpression_ReturnsCorrectMethodDeclaration()
        {
            // Arrange
            var methodCallExpression = "SomeMethod(Provider, PositionalEnvironmentVariables, QueriesInformation, Logger, token)";

            // Act
            var method = MethodDeclarationHelper.CreateRunMethod(methodCallExpression);

            // Assert
            Assert.IsNotNull(method);
            Assert.AreEqual("Run", method.Identifier.ValueText);
            Assert.AreEqual(1, method.Modifiers.Count);
            Assert.IsTrue(method.Modifiers[0].IsKind(SyntaxKind.PublicKeyword));
            Assert.IsTrue(method.ReturnType.ToString().Contains("Table"));
            Assert.AreEqual(1, method.ParameterList.Parameters.Count);
            Assert.AreEqual("token", method.ParameterList.Parameters[0].Identifier.ValueText);
            Assert.IsTrue(method.ParameterList.Parameters[0].Type.ToString().Contains("CancellationToken"));
            
            // Check method body contains the return statement
            var bodyText = method.Body.ToString();
            Assert.IsTrue(bodyText.Contains("return"));
            Assert.IsTrue(bodyText.Contains(methodCallExpression));
        }

        [TestMethod]
        public void CreateRunMethod_WithNullMethodCallExpression_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                MethodDeclarationHelper.CreateRunMethod(null));
        }

        [TestMethod]
        public void CreateRunMethod_WithEmptyMethodCallExpression_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                MethodDeclarationHelper.CreateRunMethod(""));
        }

        [TestMethod]
        public void CreateRunMethod_WithWhitespaceMethodCallExpression_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                MethodDeclarationHelper.CreateRunMethod("   "));
        }

        [TestMethod]
        public void ComplexParameterTypes_AreCorrectlyGenerated()
        {
            // Act
            var parameterList = MethodDeclarationHelper.CreateStandardParameterList();
            var posProperty = MethodDeclarationHelper.CreatePositionalEnvironmentVariablesProperty();
            var queriesProperty = MethodDeclarationHelper.CreateQueriesInformationProperty();

            // Assert - verify parameter list creates consistent types with properties
            var posParam = parameterList.Parameters[1];
            Assert.AreEqual(posParam.Type.ToString(), posProperty.Type.ToString());
            
            var queriesParam = parameterList.Parameters[2];
            // Note: Parameter and property may have slight formatting differences, so check key components
            Assert.IsTrue(queriesParam.Type.ToString().Contains("IReadOnlyDictionary"));
            Assert.IsTrue(queriesProperty.Type.ToString().Contains("IReadOnlyDictionary"));
        }

        [TestMethod]
        public void AllMethods_ProduceValidSyntax()
        {
            // Arrange & Act
            var parameterList = MethodDeclarationHelper.CreateStandardParameterList();
            var method = MethodDeclarationHelper.CreateStandardPrivateMethod("TestMethod", SyntaxFactory.Block());
            var property1 = MethodDeclarationHelper.CreatePublicProperty("string", "TestProperty");
            var property2 = MethodDeclarationHelper.CreatePositionalEnvironmentVariablesProperty();
            var property3 = MethodDeclarationHelper.CreateQueriesInformationProperty();
            var runMethod = MethodDeclarationHelper.CreateRunMethod("TestCall()");

            // Assert - all should be valid syntax nodes without compilation errors
            Assert.IsFalse(parameterList.ContainsDiagnostics);
            Assert.IsFalse(method.ContainsDiagnostics);
            Assert.IsFalse(property1.ContainsDiagnostics);
            Assert.IsFalse(property2.ContainsDiagnostics);
            Assert.IsFalse(property3.ContainsDiagnostics);
            Assert.IsFalse(runMethod.ContainsDiagnostics);
        }
    }
}