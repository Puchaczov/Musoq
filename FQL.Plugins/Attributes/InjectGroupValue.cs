using System;

namespace FQL.Plugins.Attributes
{
    public class InjectGroupValue : InjectTypeAttribute
    {
        public InjectGroupValue(Type type)
        {
            InjectType = type;
        }

        public override Type InjectType { get; }
    }
}