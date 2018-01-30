using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Musoq.Schema.DataSources;
using Musoq.Schema.Tcp.Core;

namespace Musoq.Schema.Tcp
{
    public class NetworkSource : RowSource
    {
        private readonly TcpClient _client;
        private readonly ISchemaColumn[] _columns;

        public NetworkSource(TcpClient client, ISchemaColumn[] columns)
        {
            _client = client;
            _columns = columns;
        }

        public override IEnumerable<IObjectResolver> Rows
        {
            get
            {
                using (var stream = _client.GetStream())
                {
                    var reader = new BinaryReader(stream);
                    var writer = new BinaryWriter(stream);

                    writer.Write(BitConverter.GetBytes((byte)Commands.GetRowsChunk));

                    var countOfRows = reader.ReadInt32();
                    while (countOfRows > 0)
                    {
                        var dict = new Dictionary<string, object>();

                        foreach (var column in _columns)
                        {
                            switch (column.ColumnType.Name)
                            {
                                case nameof(Boolean):
                                    dict.Add(column.ColumnName, reader.ReadBoolean());
                                    break;
                                case nameof(Byte):
                                    dict.Add(column.ColumnName, reader.ReadByte());
                                    break;
                                case nameof(Int16):
                                    dict.Add(column.ColumnName, reader.ReadInt16());
                                    break;
                                case nameof(Int32):
                                    dict.Add(column.ColumnName, reader.ReadInt32());
                                    break;
                                case nameof(Int64):
                                    dict.Add(column.ColumnName, reader.ReadInt64());
                                    break;
                                case nameof(Decimal):
                                    dict.Add(column.ColumnName, reader.ReadDecimal());
                                    break;
                                case nameof(DateTimeOffset):
                                    dict.Add(column.ColumnName, reader.Read());
                                    break;
                                case nameof(String):
                                    var stringLength = reader.ReadInt32();
                                    var bytes = reader.ReadBytes(stringLength);
                                    var text = Encoding.Unicode.GetString(bytes);
                                    dict.Add(column.ColumnName, text);
                                    break;
                            }
                        }

                        countOfRows -= 1;

                        if (countOfRows <= 0)
                        {
                            writer.Write(BitConverter.GetBytes((byte)Commands.GetRowsChunk));
                            countOfRows = reader.ReadInt32();
                        }

                        yield return new DictionaryResolver(dict);
                    }
                }
            }
        }
    }
}
