using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Musoq.Plugins;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;

namespace Musoq.Schema.FlatFile
{
    public static class FlatFileHelper
    {
        public static readonly IDictionary<string, int> FlatNameToIndexMap;
        public static readonly IDictionary<int, Func<FlatFileEntity, object>> FlatIndexToMethodAccessMap;
        public static readonly ISchemaColumn[] FlatColumns;

        static FlatFileHelper()
        {
            FlatNameToIndexMap = new Dictionary<string, int>
            {
                {nameof(FlatFileEntity.LineNumber), 0},
                {nameof(FlatFileEntity.Line), 1}
            };

            FlatIndexToMethodAccessMap = new Dictionary<int, Func<FlatFileEntity, object>>
            {
                {0, info => info.LineNumber},
                {1, info => info.Line}
            };

            FlatColumns = new ISchemaColumn[]
            {
                new SchemaColumn(nameof(FlatFileEntity.LineNumber), 0, typeof(int)),
                new SchemaColumn(nameof(FlatFileEntity.Line), 0, typeof(string))
            };
        }
    }

    public class FlatFileTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } = FlatFileHelper.FlatColumns;
    }

    public class FlatFileSchema : SchemaBase
    {
        private const string SchemaName = "FlatFile";

        public FlatFileSchema() 
            : base(SchemaName, CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            return new FlatFileTable();
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            return new FlatFileSource(parameters[0]);
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            var propertiesManager = new PropertiesManager();

            var library = new FlatFileLibrary();

            methodsManager.RegisterLibraries(library);
            propertiesManager.RegisterProperties(library);

            return new MethodsAggregator(methodsManager, propertiesManager);
        }
    }

    public class FlatFileLibrary : LibraryBase
    { }

    public class FlatFileEntity
    {
        public string Line { get; set; }
        public int LineNumber { get; set; }
    }

    public class FlatFileSource : RowSourceBase<FlatFileEntity>
    {
        private readonly string _filePath;

        public FlatFileSource(string filePath)
        {
            _filePath = filePath;
        }

        protected override async void CollectChunks(BlockingCollection<IReadOnlyList<EntityResolver<FlatFileEntity>>> chunkedSource)
        {
            const int chunkSize = 1000;

            if(!File.Exists(_filePath))
                return;

            var rowNum = 0;

            using (var file = File.OpenRead(_filePath))
            {
                using (var reader = new StreamReader(file))
                {
                    var list = new List<EntityResolver<FlatFileEntity>>();

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        var entity = new FlatFileEntity()
                        {
                            Line = line,
                            LineNumber = ++rowNum
                        };

                        list.Add(new EntityResolver<FlatFileEntity>(entity, FlatFileHelper.FlatNameToIndexMap, FlatFileHelper.FlatIndexToMethodAccessMap));

                        if (rowNum <= chunkSize)
                            continue;

                        rowNum = 0;
                        chunkedSource.Add(list);

                        list = new List<EntityResolver<FlatFileEntity>>(chunkSize);
                    }

                    chunkedSource.Add(list);
                }
            }
        }
    }

    public class FlatFileSchemaProvider : ISchemaProvider {

        public ISchema GetSchema(string schema)
        {
            return new FlatFileSchema();
        }
    }
}
