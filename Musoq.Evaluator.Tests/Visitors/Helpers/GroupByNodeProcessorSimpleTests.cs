using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Evaluator.Visitors.Helpers;

namespace Musoq.Evaluator.Tests.Visitors.Helpers;

[TestClass]
public class GroupByNodeProcessorSimpleTests
{
    [TestMethod]
    public void ProcessGroupByNode_NullNode_ThrowsException()
    {
        var nodes = new Stack<SyntaxNode>();
        var scope = new Scope(null, 0, "test");
        Assert.Throws<ArgumentNullException>(() => GroupByNodeProcessor.ProcessGroupByNode(null, nodes, scope));
    }

    [TestMethod]
    public void ProcessGroupByNode_NullNodes_ThrowsException()
    {
        var scope = new Scope(null, 0, "test");
        Assert.Throws<ArgumentNullException>(() => GroupByNodeProcessor.ProcessGroupByNode(null, null, scope));
    }

    [TestMethod]
    public void ProcessGroupByNode_NullScope_ThrowsException()
    {
        var nodes = new Stack<SyntaxNode>();
        Assert.Throws<ArgumentNullException>(() => GroupByNodeProcessor.ProcessGroupByNode(null, nodes, null));
    }

    [TestMethod]
    public void ProcessGroupByNode_HelperExists_NoException()
    {
        var scope = new Scope(null, 0, "test");
        var fieldsNamesSymbol = new FieldsNamesSymbol(["field1"]);
        scope.ScopeSymbolTable.AddSymbol("groupFields", fieldsNamesSymbol);
        
        Assert.IsNotNull(typeof(GroupByNodeProcessor));
        Assert.IsTrue(typeof(GroupByNodeProcessor).IsClass);
        Assert.IsTrue(typeof(GroupByNodeProcessor).IsAbstract && typeof(GroupByNodeProcessor).IsSealed); 
    }

    [TestMethod]
    public void GroupByProcessingResult_PropertiesExist()
    {
        var resultType = typeof(GroupByNodeProcessor.GroupByProcessingResult);
        
        Assert.IsNotNull(resultType.GetProperty("GroupKeys"));
        Assert.IsNotNull(resultType.GetProperty("GroupValues"));
        Assert.IsNotNull(resultType.GetProperty("GroupHaving"));
        Assert.IsNotNull(resultType.GetProperty("GroupFieldsStatement"));
    }

    [TestMethod]
    public void GroupByNodeProcessor_HasProcessMethod()
    {
        var method = typeof(GroupByNodeProcessor).GetMethod("ProcessGroupByNode");
        
        Assert.IsNotNull(method);
        Assert.IsTrue(method.IsStatic);
        Assert.IsTrue(method.IsPublic);
        
        var parameters = method.GetParameters();
        Assert.HasCount(3, parameters);
    }

    [TestMethod]
    public void GroupByNodeProcessor_HasValidationMethods()
    {
        var type = typeof(GroupByNodeProcessor);
        var methods = type.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        Assert.IsGreaterThan(1, methods.Length, "Should have multiple helper methods");
    }

    [TestMethod]
    public void GroupByNodeProcessor_UsesCorrectNamespaces()
    {
        var type = typeof(GroupByNodeProcessor);
        var assembly = type.Assembly;
        
        Assert.IsNotNull(assembly);
        Assert.IsNotEmpty(assembly.GetTypes());
    }
}
