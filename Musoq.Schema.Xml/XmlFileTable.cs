using Musoq.Schema.DataSources;
using System.Dynamic;

namespace Musoq.Schema.Xml
{
    public class XmlFileTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => new ISchemaColumn[] 
        {
        };

        public ISchemaColumn GetColumnByName(string name)
        {
            return new SchemaColumn(name, 0, typeof(IDynamicMetaObjectProvider));
        }
    }
}
