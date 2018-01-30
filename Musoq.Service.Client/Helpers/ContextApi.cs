using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FQL.Service.Client.Helpers
{
    public class ContextApi
    {
        private readonly string _address;

        public ContextApi(string address)
        {
            _address = address;
        }

        public async Task<Guid> Create(QueryContext context)
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(_address)
            };
            var serializedObject = JsonConvert.SerializeObject(context);
            var content = new StringContent(serializedObject, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/context/create", content);
            var respContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Guid>(respContent);
        }

        public async Task<Method[]> Methods(Guid id)
        {
            var client = new HttpClient()
            {
                BaseAddress = new Uri(_address)
            };
            var response = await client.GetAsync("api/context/methods");
            return JsonConvert.DeserializeObject<Method[]>(await response.Content.ReadAsStringAsync());
        }
    }
}