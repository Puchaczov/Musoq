using System;

namespace Musoq.Plugins.Attributes
{
    internal class InjectSourceAttribute : InjectTypeAttribute
    {
        public override Type InjectType => null;
    }
}