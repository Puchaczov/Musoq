using Musoq.Schema.Attributes;

namespace Musoq.Schema.System
{
    public class RangeItemEntity
    {
        [EntityProperty]
        public long Value { get; set; }
    }
}