using System;

namespace Musoq.Plugins.Attributes
{
    public class InjectQueryStats : InjectTypeAttribute
    {
        public override Type InjectType => typeof(QueryStats);
    }
}