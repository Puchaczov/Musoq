using System.Collections.Generic;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tables
{
    public class GroupRow : Row
    {
        private readonly IDictionary<int, string> _columnToValue;
        private readonly Group _group;

        public GroupRow(Group group, IDictionary<int, string> columnToValue)
        {
            _group = group;
            _columnToValue = columnToValue;
        }

        public override object this[int columnNumber]
            => _group.GetValue<object>(_columnToValue[columnNumber]);

        public override int Count => _columnToValue.Count;

        public override object[] Values
        {
            get
            {
                var items = new object[Count];

                for (var i = 0; i < Count; i++)
                    items[i] = this[i];

                return items;
            }
        }
    }
}