using System;
using System.Reflection;

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
            return "ACCESS CALL CHAIN";
        }
    }
}