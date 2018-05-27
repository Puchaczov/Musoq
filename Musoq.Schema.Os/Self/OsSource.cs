using System.Collections.Concurrent;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Os.Self
{
    public class OsSource : RowSourceBase<object[]>
    {
        protected override void CollectChunks(
            BlockingCollection<IReadOnlyList<EntityResolver<object[]>>> chunkedSource)
        {
            var list = new List<EntityResolver<object[]>>();

            var env = new OsHelper.Environment();

            list.Add(new EntityResolver<object[]>(new object[] { nameof(OsHelper.Environment.Processor), env.Processor}, OsHelper.ProcessNameToIndexMap, OsHelper.ProcessIndexToMethodAccessMap));
            list.Add(new EntityResolver<object[]>(new object[] { nameof(OsHelper.Environment.EntryDirectory), env.EntryDirectory }, OsHelper.ProcessNameToIndexMap, OsHelper.ProcessIndexToMethodAccessMap));
            list.Add(new EntityResolver<object[]>(new object[] { nameof(OsHelper.Environment.Memory), env.Memory }, OsHelper.ProcessNameToIndexMap, OsHelper.ProcessIndexToMethodAccessMap));
            list.Add(new EntityResolver<object[]>(new object[] { nameof(OsHelper.Environment.OperatingSystem), env.OperatingSystem }, OsHelper.ProcessNameToIndexMap, OsHelper.ProcessIndexToMethodAccessMap));
            list.Add(new EntityResolver<object[]>(new object[] { nameof(OsHelper.Environment.RoslynVersion), env.RoslynVersion }, OsHelper.ProcessNameToIndexMap, OsHelper.ProcessIndexToMethodAccessMap));
            list.Add(new EntityResolver<object[]>(new object[] { nameof(OsHelper.Environment.ProcessorCount), env.ProcessorCount }, OsHelper.ProcessNameToIndexMap, OsHelper.ProcessIndexToMethodAccessMap));

            chunkedSource.Add(list);
        }
    }
}