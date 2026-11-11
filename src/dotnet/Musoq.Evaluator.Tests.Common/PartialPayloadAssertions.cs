using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

/// <summary>Snapshots the public completion contract of runtime SQL schema payloads.</summary>
/// <remarks>
/// Narrow reflection exception: SQL declares these CLR types at runtime, and this snapshot must
/// preserve numeric CLR types and nested object identity. JSON conversion would lose that evidence.
/// Only declared public properties and the public IsFieldComplete(string) API are inspected;
/// there is no private access, construction, or interpreter invocation. Cost is linear in the
/// bounded fixture field count, with one small dictionary per inspected payload.
/// </remarks>
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
        Assert.IsFalse(presence.IsStatic);
        Assert.AreEqual(typeof(bool), presence.ReturnType);
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(property => (bool)presence.Invoke(payload, [property.Name])!)
            .ToDictionary(static property => property.Name, property => property.GetValue(payload));
    }
}
