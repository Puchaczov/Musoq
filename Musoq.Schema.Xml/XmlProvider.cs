namespace Musoq.Schema.Xml
{
    public class XmlProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new XmlSchema();
        }
    }
}
