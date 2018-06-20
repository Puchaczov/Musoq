using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Musoq.Service.Client.Core.Helpers
{
    public class ContextApi
    {
        private readonly string _address;

        public ContextApi(string address)
        {
            _address = address;
        }

        public async Task<Method[]> Methods(Guid id)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(_address)
            };
            var response = await client.GetAsync("api/context/methods");
            return JsonConvert.DeserializeObject<Method[]>(await response.Content.ReadAsStringAsync());
        }
    }
}