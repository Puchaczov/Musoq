using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

/// <summary>Test-only reflection for inspecting types generated from SQL schema declarations.</summary>
internal static class PartialPayloadAssertions
{
    internal static Type ColumnType(Table table, string? expectedSchemaName = null)
    {
        var type = table.Columns.Single(static column => column.ColumnName == "ParsedFields").ColumnType;
        Assert.AreEqual("Musoq.Generated.Interpreters", type.Namespace);
        Assert.IsTrue(type.IsClass);
        Assert.AreNotEqual(typeof(object), type);
        if (expectedSchemaName != null)
            Assert.AreEqual(expectedSchemaName, type.Name);
        return type;
    }

    internal static Dictionary<string, object?> CompletedValues(object payload)
    {
        var type = payload.GetType();
        Assert.AreEqual("Musoq.Generated.Interpreters", type.Namespace);
        var presence = type.GetMethod("IsFieldComplete", [typeof(string)]);
        Assert.IsNotNull(presence);
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(property => (bool)presence.Invoke(payload, [property.Name])!)
            .ToDictionary(static property => property.Name, property => property.GetValue(payload));
    }
}
