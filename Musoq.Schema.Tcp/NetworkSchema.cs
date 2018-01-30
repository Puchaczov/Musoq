using System.Net.Sockets;
using FQL.Schema.DataSources;
using FQL.Schema.Managers;

namespace FQL.Schema.Tcp
{
    public class NetworkSchema : SchemaBase
    {
        private const string SchemaName = "Network";
        
        private TcpClient _client;
        private NetworkTable _table;

        public NetworkSchema(MethodsAggregator methodsAggregator) 
            : base(SchemaName, methodsAggregator)
        {
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            _client = new TcpClient();
            var address = parameters[0].Split(':');
            _client.Connect(address[0], int.Parse(address[1]));
            _table = new NetworkTable(_client);
            return _table;
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            return new NetworkSource(_client, _table.Columns);
        }
    }
}
