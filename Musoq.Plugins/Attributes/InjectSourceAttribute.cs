using System;

namespace Musoq.Plugins.Attributes;

internal sealed class InjectSourceAttribute : InjectTypeAttribute
{
    public override Type? InjectType => null;
}