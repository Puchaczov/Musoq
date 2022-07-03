using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationGetMethod]
        public decimal Max([InjectGroup] Group group, string name)
            => Max(group, name, 0);

        [AggregationGetMethod]
        public decimal Max([InjectGroup] Group group, string name, int parent)
        {
            var parentGroup = GetParentGroup(group, parent);
            return parentGroup.GetValue<decimal>(name);
        }

        [AggregationSetMethod]
        public void SetMax([InjectGroup] Group group, string name, long? value, int parent = 0)
            => SetMax(group, name, (decimal?) value, parent);

        [AggregationSetMethod]
        public void SetMax([InjectGroup] Group group, string name, decimal? value, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (!value.HasValue)
            {
                parentGroup.GetOrCreateValue(name, decimal.MinValue);
                return;
            }

            var storedValue = parentGroup.GetOrCreateValue(name, decimal.MinValue);

            if (storedValue < value)
                parentGroup.SetValue(name, value);
        }
    }
}
