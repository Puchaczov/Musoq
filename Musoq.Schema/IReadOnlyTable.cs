using System.Collections.Generic;

namespace Musoq.Schema
{
    public interface IReadOnlyTable
    {
        IReadOnlyList<IReadOnlyRow> Rows { get; }

        int Count { get; }
    }
}
