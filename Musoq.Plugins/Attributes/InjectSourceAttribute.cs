using System;

namespace Musoq.Plugins.Attributes
{
    public class InjectSourceAttribute : InjectTypeAttribute
    {
        public override Type InjectType => null;
    }
}