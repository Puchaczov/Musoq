using System;
using System.Reflection;
using System.Text;

namespace Musoq.Parser.Nodes
{
    public class AccessCallChainNode : Node
    {
        public (PropertyInfo Property, object Arg)[] Props { get; }

        public string ColumnName { get; }

        public Type ColumnType { get; }

        public AccessCallChainNode(string columnName, Type columnType, (PropertyInfo Property, object Arg)[] props)
        {
            ColumnName = columnName;
            ColumnType = columnType;
            Props = props;

            Id = $"{nameof(AccessCallChainNode)}{ToString()}";
        }
        
        public override Type ReturnType
        {
            get
            {
                var prop = Props[Props.Length - 1];

                if (prop.Arg == null)
                    return prop.Property.PropertyType;

                return prop.Property.PropertyType.GetElementType();
            }
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            var callChain = new StringBuilder();

            callChain.Append(ColumnName);
            callChain.Append('.');

            (PropertyInfo Property, object Arg) prop;

            for (var i = 0; i < Props.Length - 1; ++i)
            {
                prop = Props[i];
                if(prop.Arg == null)
                    callChain.Append($"{prop.Property.Name}.");
                else
                    callChain.Append($"{prop.Property.Name}[{prop.Arg}]");
            }
            
            prop = Props[Props.Length - 1];
            if (prop.Arg == null)
                callChain.Append($"{prop.Property.Name}");
            else
                callChain.Append($"{prop.Property.Name}[{prop.Arg}]");

            return callChain.ToString();
        }
    }
}