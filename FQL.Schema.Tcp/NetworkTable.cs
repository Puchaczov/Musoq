using System;
using System.IO;
using System.Net.Sockets;
using FQL.Schema.DataSources;
using FQL.Schema.Tcp.Core;
using Newtonsoft.Json;

namespace FQL.Schema.Tcp
{
    public class NetworkTable : ISchemaTable
    {
        private readonly TcpClient _client;
        private ISchemaColumn[] _columns;

        public NetworkTable(TcpClient client)
        {
            _client = client;
        }

        public ISchemaColumn[] Columns
        {
            get
            {
                if (_columns == null)
                {
                    using (var stream = _client.GetStream())
                    {
                        var reader = new BinaryReader(stream);
                        var writer = new BinaryWriter(stream);

                        writer.Write(BitConverter.GetBytes((byte)Commands.GetHeaders));
                        var json = reader.ReadString();
                        _columns = JsonConvert.DeserializeObject<SchemaColumn[]>(json);
                        return _columns;
                    }
                }
                else
                {
                    return _columns;
                }
            }
        }
    }
}
