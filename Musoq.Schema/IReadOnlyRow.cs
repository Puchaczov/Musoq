using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Schema
{
    public interface IReadOnlyRow
    {
        object this[int columnNumber] { get; }
    }
}
