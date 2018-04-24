using System;
using System.Collections.Generic;
using System.Text;
using Musoq.Evaluator.CSharpTemplates;

namespace Musoq.Evaluator.CSharpTemplates
{
    public abstract partial class Query
    {
        public abstract string Select { get; }
        public abstract string Where { get; }
        public abstract string Source { get; }
        public abstract string Group { get; }
    }
}
