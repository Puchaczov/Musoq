using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationGetMethod]
        public int Count([InjectGroup] Group group, string name)
            => Count(group, name, 0);

        [AggregationGetMethod]
        public int Count([InjectGroup] Group group, string name, int parent)
        {
            var parentGroup = GetParentGroup(group, parent);
            return parentGroup.GetValue<int>(name);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, string value, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);

            if (value == null)
            {
                parentGroup.GetOrCreateValue<int>(name);
                return;
            }

            var values = parentGroup.GetOrCreateValue<int>(name);
            parentGroup.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, decimal? value, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);

            if (value == null)
            {
                parentGroup.GetOrCreateValue<int>(name);
                return;
            }

            var values = parentGroup.GetOrCreateValue<int>(name);
            parentGroup.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, DateTimeOffset? value, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (value == null)
            {
                parentGroup.GetOrCreateValue<int>(name);
                return;
            }

            var values = parentGroup.GetOrCreateValue<int>(name);
            parentGroup.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, DateTime? value, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (value == null)
            {
                parentGroup.GetOrCreateValue<int>(name);
                return;
            }

            var values = parentGroup.GetOrCreateValue<int>(name);
            parentGroup.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, long? value, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (value == null)
            {
                parentGroup.GetOrCreateValue<int>(name);
                return;
            }

            var values = parentGroup.GetOrCreateValue<int>(name);
            parentGroup.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, int? value, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (value == null)
            {
                parentGroup.GetOrCreateValue<int>(name);
                return;
            }

            var values = parentGroup.GetOrCreateValue<int>(name);
            parentGroup.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, bool? value, int parent = 0)
        {
            var parentGroup = GetParentGroup(group, parent);
            if (value == null)
            {
                parentGroup.GetOrCreateValue<int>(name);
                return;
            }

            var values = parentGroup.GetOrCreateValue<int>(name);
            parentGroup.SetValue(name, values + 1);
        }
    }
}
