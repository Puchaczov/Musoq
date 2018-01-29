using System;

namespace FQL.Plugins.Attributes
{
    public class InjectGroupAccessName : InjectTypeAttribute
    {
        public override Type InjectType => typeof(string);
    }
}