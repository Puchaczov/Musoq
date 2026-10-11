using Musoq.Schema;

namespace Musoq.Examples.DataSources.Csv.Tests;

internal readonly record struct NativeCsvTestRow(string Name);
internal readonly struct NativeCsvTestMaterializer : IQueryRowMaterializer<NativeCsvTestRow>
{
    public static NativeCsvTestRow Materialize<TReader>(ref TReader reader)
        where TReader : IQuerySourceFieldReader, allows ref struct => new(reader.Read<string>(0));
}
