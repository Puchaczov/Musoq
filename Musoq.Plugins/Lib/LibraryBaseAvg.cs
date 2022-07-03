using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationGetMethod]
        public decimal Avg([InjectGroup] Group group, string name)
            => Avg(group, name, 0);

        [AggregationGetMethod]
        public decimal Avg([InjectGroup] Group group, string name, int parent)
        {
            var parentGroup = GetParentGroup(group, parent);
            return Sum(parentGroup, name) / parentGroup.Count;
        }

        [AggregationGetMethod]
        public void SetAvg([InjectGroup] Group group, string name, long? value, int parent = 0)
        {
            SetSum(group, name, value, parent);
            var parentGroup = GetParentGroup(group, parent);
            if(value.HasValue)
                parentGroup.Hit();
        }

        [AggregationSetMethod]
        public void SetAvg([InjectGroup] Group group, string name, decimal? value, int parent = 0)
        {
            SetSum(group, name, value, parent);
            var parentGroup = GetParentGroup(group, parent);
            if (value.HasValue)
                parentGroup.Hit();
        }
    }
}
