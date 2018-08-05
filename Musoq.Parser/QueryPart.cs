using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Parser
{
    public enum QueryPart
    {
        None,
        Select,
        From,
        Where,
        GroupBy,
        Having
    }
}
