using Musoq.Schema.DataSources;
using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Service.Core.Tests.Schema
{
    public class TestSource : RowSource
    {
        public override IEnumerable<IObjectResolver> Rows => new List<IObjectResolver>();
    }
}
