using System;

namespace Musoq.Plugins.Attributes
{
    public class InjectGroupAccessName : InjectTypeAttribute
    {
        public override Type InjectType => typeof(string);
    }
}