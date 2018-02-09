using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using Newtonsoft.Json.Linq;

namespace Musoq.Schema.Json
{
    public class JsonLibrary : LibraryBase
    {
        [BindableMethod]
        public int Length(JArray array)
        {
            return array.Count;
        }
    }
}