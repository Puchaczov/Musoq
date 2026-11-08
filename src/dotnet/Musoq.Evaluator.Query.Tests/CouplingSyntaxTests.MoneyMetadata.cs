using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public partial class CouplingSyntaxTests
{
    [TestMethod]
    [TestProperty("CandidateId", "P3-016")]
    public void MoneyTableColumnOnTypedProvider_ShouldAdvertiseProviderDecimal()
    {
        const string query = "table Amounts { Value: money }; couple test.Entities with table Amounts as Source; select a.Value as Value from Source() a";
        var entity = new DeclaredAmountEntity { Value = 12.5m };
        var schema = new GenericSchema<LibraryBase>(new Dictionary<string, (ISchemaTable SchemaTable, object RowSource)>
        {
            ["Entities"] = (new GenericEntityTable<DeclaredAmountEntity>(),
                new GenericChunkSource<DeclaredAmountEntity>([entity], GenericEntityTable<DeclaredAmountEntity>.NameToIndexMap, GenericEntityTable<DeclaredAmountEntity>.IndexToObjectAccessMap))
        });
        var provider = new GenericSchemaProvider(new Dictionary<string, ISchema> { ["test"] = schema, ["#test"] = schema });
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: provider);
        using var result = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsUnordered(result, new object?[] { 12.5m });
        TableMaterializationTestHelper.AssertColumns(result, ("Value", typeof(decimal)));
    }

    public sealed class DeclaredAmountEntity
    {
        public decimal Value { get; init; }
    }
}
