using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Evaluator.CSharpTemplates
{
    public abstract partial class SelectClause
    {
        public abstract string[] Rows { get; }

        public abstract string Table { get; }
    }
}
