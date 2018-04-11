using System.Collections.Concurrent;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Disk.Process
{
    public class ProcessesSource : RowSourceBase<System.Diagnostics.Process>
    {
        protected override void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<System.Diagnostics.Process>>> chunkedSource)
        {
            var list = new List<EntityResolver<System.Diagnostics.Process>>();

            int i = 0;
            foreach (var process in System.Diagnostics.Process.GetProcesses())
            {
                i += 1;
                list.Add(new EntityResolver<System.Diagnostics.Process>(process, ProcessHelper.ProcessNameToIndexMap, ProcessHelper.ProcessIndexToMethodAccessMap));

                if (i < 20)
                    continue;

                i = 0;
                chunkedSource.Add(list);
                list = new List<EntityResolver<System.Diagnostics.Process>>();
            }
            chunkedSource.Add(list);
        }
    }
}