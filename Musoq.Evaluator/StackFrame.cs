using System.Collections.Generic;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator
{
    public class StackFrame
    {
        public StackFrame()
        {
            CurrentGroup = new Group(null, new string[0], new object[0]);
            Groups.Add("root", CurrentGroup);
        }

        public Stack<bool> BooleanStack { get; } = new Stack<bool>();
        public Stack<long> LongsStack { get; } = new Stack<long>();
        public Stack<decimal> NumericsStack { get; } = new Stack<decimal>();
        public Stack<string> StringsStack { get; } = new Stack<string>();
        public Stack<object> ObjectsStack { get; } = new Stack<object>();
        public Stack<IEnumerator<IObjectResolver>> SourceStack { get; } = new Stack<IEnumerator<IObjectResolver>>();
        public IDictionary<string, Table> Tables { get; } = new Dictionary<string, Table>();
        public long[] Registers { get; } = new long[4];
        public IDictionary<string, Group> Groups { get; } = new Dictionary<string, Group>();
        public Group CurrentGroup { get; set; }
        public AmendableQueryStats Stats { get; set; } = new AmendableQueryStats();
    }
}