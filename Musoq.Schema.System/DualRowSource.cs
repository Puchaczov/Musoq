using System.Collections.Concurrent;
using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.System
{
    public class DualRowSource : RowSourceBase<DualEntity>
    {
        protected override void CollectChunks(BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource)
        {
            chunkedSource.Add(
                new[]
                {
                    new EntityResolver<DualEntity>(new DualEntity(), SystemSchemaHelper.FlatNameToIndexMap, SystemSchemaHelper.FlatIndexToMethodAccessMap)
                });
        }
    }
}