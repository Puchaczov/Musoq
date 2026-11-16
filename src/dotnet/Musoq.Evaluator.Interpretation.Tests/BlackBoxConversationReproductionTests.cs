using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

[TestClass]
[TestCategory("BlackBoxConversationReproduction")]
public sealed partial class BlackBoxConversationReproductionTests : BinaryOrTextualEvaluatorTestBase
{
    private const string CatalogSchema = """
        binary Entry { Id: ushort le, Length: byte, Label: string[Length] utf8 };
        binary Rec {
            Position: byte, ActorsCount: byte, BadgesCount: byte,
            Actors: Entry[ActorsCount], Badges: Entry[BadgesCount]
        };
        """;

    private const string HolderCte = """
        with holders as (
            select r.Position::long as Holder
            from #test.files() f cross apply Interpret<Rec>(f.Content) r
        )
        """;

    private const string RecoveryCtes = """
        binary Rec { EventId: byte, Revision: byte, Flags: byte, Valid: byte };
        with physical as (
            select r.EventId, r.Revision, r.Flags, r.Valid
            from #test.files() f cross apply Interpret<Rec>(f.Content) r
        ), ranked as (
            select p.*, RowNumber() over (partition by p.EventId order by p.Revision desc) as Rank
            from physical p where p.Valid = 1
        ), effective as (
            select r.* from ranked r where r.Rank = 1 and (r.Flags & 1) = 0
        )
        """;

    public TestContext TestContext { get; set; } = null!;

    private static byte[] CatalogRecord => [2, 2, 2, 1, 0, 1, 65, 2, 0, 1, 66, 11, 0, 1, 88, 12, 0, 1, 89];

    private static byte[][] RecoveryRecords =>
        [
            [1, 1, 0, 1],
            [1, 2, 0, 1],
            [1, 3, 0, 0],
            [2, 1, 0, 1],
            [2, 2, 1, 1],
            [3, 1, 0, 1],
        ];

    private Table RunQuery(string sql, QueryInstrumentationMode mode, params byte[][] records)
    {
        TestContext.WriteLine($"MODE: {mode}");
        TestContext.WriteLine($"SQL: {sql}");
        TestContext.WriteLine($"INPUT_HEX: {JsonSerializer.Serialize(Array.ConvertAll(records, Convert.ToHexString))}");

        var entities = Array.ConvertAll(records, bytes => new BinaryEntity { Content = bytes });
        var provider = new BinarySchemaProvider(
            new Dictionary<string, IEnumerable<BinaryEntity>> { ["#test"] = entities }
        );
        using var query = InstanceCreator.CompileForExecution(
            sql,
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            new CompilationOptions(usePrimitiveTypeValidation: false, instrumentationMode: mode)
        );
        var table = TableMaterializationTestHelper.Materialize(query.Run());

        TestContext.WriteLine(
            $"COLUMNS: {JsonSerializer.Serialize(table.Columns.Select(column => new { Name = column.ColumnName, Type = column.ColumnType.FullName }))}"
        );
        TestContext.WriteLine($"ROWS: {JsonSerializer.Serialize(table.Select(row => row.ReadValues()).ToArray())}");
        return table;
    }
}
