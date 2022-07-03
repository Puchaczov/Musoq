using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationGetMethod]
        public decimal Sum([InjectGroup] Group group, string name)
            => Sum(group, name, 0);

        [AggregationGetMethod]
        public decimal Sum([InjectGroup] Group group, string name, int parent)
        {
            return GetParentGroup(group, parent).GetValue<decimal>(name);
        }

        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, decimal? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);
            parentGroup.SetValue(name, value + number);
        }

        [AggregationSetMethod]
        public void SetSum([InjectGroup] Group group, string name, long? number, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!number.HasValue)
            {
                parentGroup.GetOrCreateValue<decimal>(name);
                return;
            }

            var value = parentGroup.GetOrCreateValue<decimal>(name);
            parentGroup.SetValue(name, value + number);
        }
    }
}
