using System;

namespace FQL.Plugins.Attributes
{
    public class InjectSourceAttribute : InjectTypeAttribute
    {
        public override Type InjectType => null;
    }
}