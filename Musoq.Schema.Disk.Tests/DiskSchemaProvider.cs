namespace Musoq.Schema.Disk.Tests
{
    internal class DiskSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new DiskSchema();
        }
    }
}