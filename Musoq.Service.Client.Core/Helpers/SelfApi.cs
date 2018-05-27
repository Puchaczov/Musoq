using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Musoq.Service.Client.Core.Helpers
{
    public class SelfApi
    {
        private readonly string _address;

        public SelfApi(string address)
        {
            _address = address;
        }

        public async Task<Guid> UsedFiles()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(_address)
            };

            var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/self/usedfiles", content);
            var respContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Guid>(respContent);
        }
    }
}