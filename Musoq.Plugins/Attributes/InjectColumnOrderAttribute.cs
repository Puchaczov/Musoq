using System;

namespace FQL.Plugins.Attributes
{
    public class InjectColumnOrderAttribute : InjectTypeAttribute
    {
        public override Type InjectType => typeof(int);
    }
}