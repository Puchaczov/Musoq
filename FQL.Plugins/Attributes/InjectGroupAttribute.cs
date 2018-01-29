using System;

namespace FQL.Plugins.Attributes
{
    public class InjectGroupAttribute : InjectTypeAttribute
    {
        public override Type InjectType => typeof(Group);
    }
}