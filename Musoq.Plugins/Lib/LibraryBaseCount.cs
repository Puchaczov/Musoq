using System;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins
{
    public partial class LibraryBase
    {
        [AggregationGetMethod]
        public int Count([InjectGroup] Group group, string name)
        {
            return group.GetValue<int>(name);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, string value)
        {
            if (value == null)
            {
                group.GetOrCreateValue<int>(name);
                return;
            }

            var values = group.GetOrCreateValue<int>(name);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, decimal? value)
        {
            if (value == null)
            {
                group.GetOrCreateValue<int>(name);
                return;
            }

            var values = group.GetOrCreateValue<int>(name);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, DateTimeOffset? value)
        {
            if (value == null)
            {
                group.GetOrCreateValue<int>(name);
                return;
            }

            var values = group.GetOrCreateValue<int>(name);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, DateTime? value)
        {
            if (value == null)
            {
                group.GetOrCreateValue<int>(name);
                return;
            }

            var values = group.GetOrCreateValue<int>(name);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, long? value)
        {
            if (value == null)
            {
                group.GetOrCreateValue<int>(name);
                return;
            }

            var values = group.GetOrCreateValue<int>(name);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, int? value)
        {
            if (value == null)
            {
                group.GetOrCreateValue<int>(name);
                return;
            }

            var values = group.GetOrCreateValue<int>(name);
            group.SetValue(name, values + 1);
        }

        [AggregationSetMethod]
        public void SetCount([InjectGroup] Group group, string name, bool? value)
        {
            if (value == null)
            {
                group.GetOrCreateValue<int>(name);
                return;
            }

            var values = group.GetOrCreateValue<int>(name);
            group.SetValue(name, values + 1);
        }

        [AggregationGetMethod]
        public int ParentCount([InjectGroup] Group group, string name)
        {
            var parentGroup = group.GetValue<int>(name);
            return parentGroup;
        }

        [AggregationSetMethod]
        public void SetParentCount([InjectGroup] Group group, string name, long number)
        {
            var parent = GetParentGroup(group, number);

            var value = parent.GetOrCreateValue<int>(name);
            parent.SetValue(name, value + 1);
            group.GetOrCreateValueWithConverter<Group, int>(name, parent, o => ((Group)o).GetValue<int>(name));
        }
    }
}
