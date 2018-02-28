namespace Musoq.Schema.FlatFile
{
    public class FlatFileSchemaProvider : ISchemaProvider {

        public ISchema GetSchema(string schema)
        {
            return new FlatFileSchema();
        }
    }
}