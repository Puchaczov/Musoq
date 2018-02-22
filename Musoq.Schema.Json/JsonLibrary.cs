using System.Text;
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

        [BindableMethod]
        public string MakeFlat(JArray array)
        {
            var cnt = array.Count;

            if (cnt == 0)
                return string.Empty;

            var flattedArray = new StringBuilder();

            for (int i = 0; i < cnt - 1; i++)
            {
                flattedArray.Append(array[i]);
                flattedArray.Append(", ");
            }

            flattedArray.Append(array[array.Count - 1]);

            return flattedArray.ToString();
        }
    }
}