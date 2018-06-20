using Musoq.ContentAggregator.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Musoq.ContentAggregator.Models
{
    public class QueryModel
    {
        public string Name { get; set; }

        public string Text { get; set; }

        public ShowAt ShowAt { get; set; }

        public Guid ScriptId { get; set; }
    }
}
