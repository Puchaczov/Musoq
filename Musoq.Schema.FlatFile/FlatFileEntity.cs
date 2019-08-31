using Musoq.Schema.Attributes;

namespace Musoq.Schema.FlatFile
{
    public class FlatFileEntity
    {
        [EntityProperty]
        public string Line { get; set; }

        [EntityProperty]
        public int LineNumber { get; set; }
    }
}