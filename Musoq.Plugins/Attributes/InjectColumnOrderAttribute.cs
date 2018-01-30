using System;

namespace Musoq.Plugins.Attributes
{
    public class InjectColumnOrderAttribute : InjectTypeAttribute
    {
        public override Type InjectType => typeof(int);
    }
}