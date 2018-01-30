using System;

namespace Musoq.Plugins.Attributes
{
    public class InjectGroupAttribute : InjectTypeAttribute
    {
        public override Type InjectType => typeof(Group);
    }
}