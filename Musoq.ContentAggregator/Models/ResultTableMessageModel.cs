using Musoq.Service.Client.Core;
using System;

namespace Musoq.ContentAggregator.Models
{
    public class ResultTableMessageModel : MainPageMessageModel {
        public ResultTable Result { get; set; }
        public DateTimeOffset RefreshedAt { get; set; }
    }
}
