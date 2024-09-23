using System;

namespace Musoq.Plugins.Attributes
{
    public sealed class InjectGroupAccessName : InjectTypeAttribute
    {
        public override Type InjectType => typeof(string);
    }
}