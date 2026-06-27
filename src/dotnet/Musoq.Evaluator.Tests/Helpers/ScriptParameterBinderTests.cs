using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ScriptParameterBinderTests
{
    [TestMethod]
    public void GetRequired_WhenValueExists_ShouldReturnTypedValue()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["author"] = "Ada"
        };

        var value = ScriptParameterBinder.GetRequired<string>(parameters, "author");

        Assert.AreEqual("Ada", value);
    }

    [TestMethod]
    public void GetRequired_WhenParametersAreReadOnlyDictionary_ShouldReturnTypedValue()
    {
        IReadOnlyDictionary<string, object?> parameters =
            new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["author"] = "Ada"
            });

        var value = ScriptParameterBinder.GetRequired<string>(parameters, "author");

        Assert.AreEqual("Ada", value);
    }

    [TestMethod]
    public void GetRequired_WhenValueIsMissing_ShouldThrow()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        var ex = Assert.Throws<ScriptParameterBindingException>(() =>
            ScriptParameterBinder.GetRequired<string>(parameters, "author"));
        Assert.AreEqual(DiagnosticCode.MQ7003_RequiredScriptParameterMissing, ex.Code);
        StringAssert.Contains(ex.Message, "author");
    }

    [TestMethod]
    public void GetOptional_WhenValueIsMissing_ShouldReturnDefault()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

        var value = ScriptParameterBinder.GetOptional(parameters, "limit", 100);

        Assert.AreEqual(100, value);
    }

    [TestMethod]
    public void GetOptional_WhenValueExists_ShouldReturnTypedValue()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["limit"] = 25
        };

        var value = ScriptParameterBinder.GetOptional(parameters, "limit", 100);

        Assert.AreEqual(25, value);
    }

    [TestMethod]
    public void GetRequired_WhenValueTypeValueIsNull_ShouldThrow()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["limit"] = null
        };

        var ex = Assert.Throws<ScriptParameterBindingException>(() =>
            ScriptParameterBinder.GetRequired<int>(parameters, "limit"));
        Assert.AreEqual(DiagnosticCode.MQ7005_ScriptParameterNullNotAllowed, ex.Code);
        StringAssert.Contains(ex.Message, "limit");
    }

    [TestMethod]
    public void GetRequired_WhenReferenceTypeValueIsNull_ShouldReturnNull()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["author"] = null
        };

        var value = ScriptParameterBinder.GetRequired<string>(parameters, "author");

        Assert.IsNull(value);
    }

    [TestMethod]
    public void GetOptional_WhenNullableValueTypeValueIsNull_ShouldReturnNull()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["limit"] = null
        };

        var value = ScriptParameterBinder.GetOptional<int?>(parameters, "limit", 100);

        Assert.IsNull(value);
    }

    [TestMethod]
    public void GetRequired_WhenValueHasDifferentType_ShouldThrow()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["limit"] = "100"
        };

        var ex = Assert.Throws<ScriptParameterBindingException>(() =>
            ScriptParameterBinder.GetRequired<int>(parameters, "limit"));
        Assert.AreEqual(DiagnosticCode.MQ7004_ScriptParameterTypeMismatch, ex.Code);
        StringAssert.Contains(ex.Message, "limit");
    }

    [TestMethod]
    public void GetRequiredCollection_WhenValueIsArray_ShouldReturnReadOnlyList()
    {
        var values = new[] { 1, 2, 3 };
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ids"] = values
        };

        var result = ScriptParameterBinder.GetRequiredCollection<int>(parameters, "ids");

        Assert.AreSame(values, result);
    }

    [TestMethod]
    public void GetRequiredCollection_WhenValueIsList_ShouldReturnReadOnlyList()
    {
        var values = new List<int> { 1, 2, 3 };
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ids"] = values
        };

        var result = ScriptParameterBinder.GetRequiredCollection<int>(parameters, "ids");

        Assert.AreSame(values, result);
    }

    [TestMethod]
    public void GetRequiredCollection_WhenValueIsReadOnlyList_ShouldReturnTypedValue()
    {
        IReadOnlyList<int> values = Array.AsReadOnly(new[] { 1, 2, 3 });
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ids"] = values
        };

        var result = ScriptParameterBinder.GetRequiredCollection<int>(parameters, "ids");

        Assert.AreSame(values, result);
    }

    [TestMethod]
    public void GetRequiredCollection_WhenValueIsNull_ShouldThrow()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ids"] = null
        };

        var ex = Assert.Throws<ScriptParameterBindingException>(() =>
            ScriptParameterBinder.GetRequiredCollection<int>(parameters, "ids"));
        Assert.AreEqual(DiagnosticCode.MQ7005_ScriptParameterNullNotAllowed, ex.Code);
        StringAssert.Contains(ex.Message, "ids");
    }

    [TestMethod]
    public void GetRequiredCollection_WhenValueHasDifferentElementType_ShouldThrow()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ids"] = new[] { "1", "2" }
        };

        var ex = Assert.Throws<ScriptParameterBindingException>(() =>
            ScriptParameterBinder.GetRequiredCollection<int>(parameters, "ids"));
        Assert.AreEqual(DiagnosticCode.MQ7004_ScriptParameterTypeMismatch, ex.Code);
        StringAssert.Contains(ex.Message, "ids");
    }

    [TestMethod]
    public void GetOptional_WhenValueHasDifferentType_ShouldThrowDiagnosticException()
    {
        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["limit"] = "100"
        };

        var ex = Assert.Throws<ScriptParameterBindingException>(() =>
            ScriptParameterBinder.GetOptional(parameters, "limit", 100));
        Assert.AreEqual(DiagnosticCode.MQ7004_ScriptParameterTypeMismatch, ex.Code);
        StringAssert.Contains(ex.Message, "int");
    }
}
