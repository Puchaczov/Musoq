using System;

namespace Musoq.Plugins.Attributes
{
    public abstract class InjectTypeAttribute : Attribute
    {
        /// <inheritdoc />
        /// <summary>
        ///     Initialize object.
        /// </summary>
        internal InjectTypeAttribute()
        {
        }

        /// <summary>
        ///     Gets the type have to be injected when dynamic invocation performed.
        /// </summary>
        public abstract Type? InjectType { get; }
    }
}