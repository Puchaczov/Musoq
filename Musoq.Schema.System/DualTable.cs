using Musoq.Schema.DataSources;

namespace Musoq.Schema.System
{
    public class DualTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => new ISchemaColumn[]
        {
            new SchemaColumn(nameof(DualEntity.Dummy), 0, typeof(string)), 
        };
    }
}