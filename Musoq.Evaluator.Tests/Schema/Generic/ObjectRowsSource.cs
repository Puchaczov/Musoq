using System.Collections.Generic;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.Schema.Generic;

public class ObjectRowsSource(IEnumerable<IObjectResolver> rows) : RowSource
{
    public override IEnumerable<IObjectResolver> Rows { get; } = rows ?? [];
}