using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Parser;

public class SchemaFromNode : Musoq.Parser.Nodes.From.SchemaFromNode
{
    private readonly string _positionalId;
    
    public SchemaFromNode(string schema, string method, ArgsListNode parameters, string alias, int inSourcePosition) 
        : base(schema, method, parameters, alias, typeof(RowSource), inSourcePosition)
    {
        _positionalId = $"{alias}:{inSourcePosition}";
    }
    
    public override string Id => _positionalId;

    public override int GetHashCode()
    {
        return _positionalId.GetHashCode();
    }
    
    public override bool Equals(object obj)
    {
        if (obj is SchemaFromNode schemaFromNode)
            return _positionalId == schemaFromNode._positionalId;

        return base.Equals(obj);
    }
}