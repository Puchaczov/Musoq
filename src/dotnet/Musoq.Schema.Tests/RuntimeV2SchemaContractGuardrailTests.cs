using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class RuntimeV2SchemaContractGuardrailTests
{
    [TestMethod]
    public void SchemaInterface_ShouldExposeRuntimeV2ContextAndTypedRowSourceContract()
    {
        var getTable = typeof(ISchema).GetMethod(
            nameof(ISchema.GetTableByName),
            [typeof(string), typeof(SourceMetadataContext), typeof(object[])]);
        Assert.IsNotNull(getTable);
        Assert.AreEqual(typeof(ISchemaTable), getTable.ReturnType);

        var getRowSource = typeof(ISchema)
            .GetMethods()
            .Single(static method => method.Name == nameof(ISchema.GetRowSource));
        Assert.IsTrue(getRowSource.IsGenericMethodDefinition);
        Assert.AreEqual(typeof(RowSource<>), getRowSource.ReturnType.GetGenericTypeDefinition());
        CollectionAssert.AreEqual(
            new[] { typeof(string), typeof(SourceExecutionContext), typeof(object[]) },
            getRowSource.GetParameters().Select(static parameter => parameter.ParameterType).ToArray());
    }

    [TestMethod]
    public void SchemaAssembly_ShouldNotReintroduceRuntimeV1ContractTypes()
    {
        string[] retiredTypeNames =
        [
            "RuntimeContext",
            "QuerySourceInfo",
            "QueryHints",
            "IObjectResolver",
            "EntityResolver"
        ];

        var publicTypeNames = typeof(ISchema).Assembly
            .GetExportedTypes()
            .Select(static type => type.Name)
            .ToHashSet(StringComparer.Ordinal);

        var offenders = retiredTypeNames
            .Where(publicTypeNames.Contains)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Runtime v2 intentionally removed runtime-v1 schema contract types: " +
            string.Join(", ", offenders));
    }
}