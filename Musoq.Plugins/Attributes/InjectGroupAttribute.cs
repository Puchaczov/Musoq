using System;

namespace Musoq.Plugins.Attributes;

public sealed class InjectGroupAttribute : InjectTypeAttribute
{
    public override Type InjectType => typeof(Group);
}