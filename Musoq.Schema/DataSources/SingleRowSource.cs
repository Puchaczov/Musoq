using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Schema.DataSources
{
    public class SingleRowSource : RowSourceBase<string>
    {
        protected override void CollectChunks(BlockingCollection<IReadOnlyList<IObjectResolver>> chunkedSource)
        {
            var list = new List<EntityResolver<string>>
            {
                new EntityResolver<string>(
                string.Empty,
                new Dictionary<string, int>()
                {
                    {"Column1", 0}
                },
                new Dictionary<int, Func<string, object>>()
                {
                    {0, (str) => str}
                })
            };
            chunkedSource.Add(list);
        }
    }
}
