using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Plugins.Attributes
{
    public class InjectQueryStats : InjectTypeAttribute
    {
        public override Type InjectType => typeof(QueryStats);
    }
}
