namespace Musoq.Schema.Disk.Tests.Core
{
    internal class DiskSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new DiskSchema();
        }
    }
}