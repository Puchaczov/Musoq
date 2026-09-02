using System.Collections.Generic;
using Musoq.Evaluator.Tests.Schema.QueryRows;

namespace Musoq.Evaluator.Tests;

internal static partial class GeneratedCodeSamplesCatalog
{
    private static IReadOnlyList<GeneratedCodeSample> CreateQueryScopedRowSamples()
    {
        return
        [
            QueryScopedRowSample(
                "Q236_QueryRowLegacyFallback",
                "select r.Id, r.Name from #queryrowsample.rows() r",
                GeneratedQueryRowSampleShape.Narrow,
                queryScopedRowsEnabled: false),
            QueryScopedRowSample(
                "Q237_QueryRowReadonlyStruct",
                "select r.Id, r.Name from #queryrowsample.rows() r",
                GeneratedQueryRowSampleShape.Narrow,
                queryScopedRowsEnabled: true),
            QueryScopedRowSample(
                "Q238_QueryRowSealedClass",
                "select r.G0, r.G1, r.G2, r.G3, r.G4 from #queryrowsample.rows() r",
                GeneratedQueryRowSampleShape.Wide,
                queryScopedRowsEnabled: true),
            QueryScopedRowSample(
                "Q239_QueryRowZeroField",
                "select Count(*) as Total from #queryrowsample.rows() r",
                GeneratedQueryRowSampleShape.Narrow,
                queryScopedRowsEnabled: true),
            QueryScopedRowSample(
                "Q240_QueryRowSpecialNames",
                "select r.[display name], r.[na-me], r.[MiastoŁódź], r.[select] from #queryrowsample.rows() r",
                GeneratedQueryRowSampleShape.SpecialNames,
                queryScopedRowsEnabled: true),
            QueryScopedRowSample(
                "Q241_QueryRowLifetimeBoundary",
                "select l.Id as LeftId, r.Id as RightId from #queryrowsample.rows() l " +
                "inner join #queryrowsample.rows() r on l.Id = r.Id",
                GeneratedQueryRowSampleShape.Narrow,
                queryScopedRowsEnabled: true),
            QueryScopedRowSample(
                "Q324_EnumQueryScopedRows",
                "enum JobStatus : short { Queued = 10s, Running = 20s, Finished = 30s };" +
                "flags enum FileAccess : uint { None = 0ui, Read = 1ui, Write = 2ui, ReadWrite = 3ui };" +
                "table EnumRows { Id: int, Status: JobStatus, Access: FileAccess };" +
                "couple #queryrowsample.rows with table EnumRows as Rows;" +
                "select Status, EnumName(Status) as StatusName, HasAllFlags(Access, 'Read', 'Write') as CanWrite " +
                "from Rows() where Status in ('Queued', 'Running')",
                GeneratedQueryRowSampleShape.Enum,
                queryScopedRowsEnabled: true)
        ];
    }

    private static GeneratedCodeSample QueryScopedRowSample(
        string name,
        string query,
        GeneratedQueryRowSampleShape shape,
        bool queryScopedRowsEnabled)
    {
        return new GeneratedCodeSample
        {
            Name = name,
            FileName = $"{name}.cs",
            Query = query,
            Category = "QueryScopedRows",
            Format = GeneratedCodeSampleFormat.QueryHeaderAndGeneratedCode,
            CreateSchemaProvider = () => new GeneratedQueryRowSampleSchemaProvider(
                shape,
                queryScopedRowsEnabled)
        };
    }
}
