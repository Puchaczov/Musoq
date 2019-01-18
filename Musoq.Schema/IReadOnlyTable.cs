using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Schema
{
    public interface IReadOnlyTable
    {
        IReadOnlyList<IReadOnlyRow> Rows { get; }

        int Count { get; }
    }
}
