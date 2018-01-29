using System;
using FQL.Schema.DataSources;
using FQL.Schema.Managers;

namespace FQL.Schema.Word
{
    public class WordSchema : SchemaBase
    {
        private const string TypeSchema = "directory";

        public WordSchema(MethodsAggregator methodsAggregator)
            : base(methodsAggregator)
        {
        }

        public WordSchema()
            : base(new MethodsAggregator(new MethodsManager(), new PropertiesManager()))
        {
        }

        public override ISchemaTable GetTableByName(string name, string[] parameters)
        {
            if (name.ToLowerInvariant() == TypeSchema)
                return new WordBasedTable();

            throw new NotSupportedException();
        }

        public override RowSource GetRowSource(string name, string[] parameters)
        {
            switch (name.ToLowerInvariant())
            {
                case TypeSchema:
                    return new WordSource(parameters[0], TryRecognizeBoolean(parameters[1]));
            }

            throw new NotSupportedException();
        }

        private bool TryRecognizeBoolean(string str)
        {
            str = str.Trim().ToLowerInvariant();
            if (str == "1")
                return true;
            if (str == "0")
                return false;
            if (str == "true")
                return true;
            if (str == "false")
                return false;

            throw new NotSupportedException($"value('{str}') as {nameof(Boolean)}");
        }
    }
}