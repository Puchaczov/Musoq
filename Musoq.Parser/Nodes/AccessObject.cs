using System;
using System.Reflection;

namespace FQL.Parser.Nodes
{
    public class AccessPropertyNode : UnaryNode
    {
        public Node Root { get; }

        public bool IsOuter { get; }

        public string Name { get; }

        public AccessPropertyNode(Node root, Node expression, bool isOuter, string name)
            : base(expression)
        {
            Root = root;
            IsOuter = isOuter;
            Name = name;
        }

        public AccessPropertyNode(Node root, Node expression, bool isOuter, string name, PropertyInfo propertyInfo)
            : this(root, expression, isOuter, name)
        {
            PropertyInfo = propertyInfo;
        }

        public override Type ReturnType => Expression.ReturnType;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public PropertyInfo PropertyInfo { get; set; }

        public override string ToString()
        {
            return $"{Root.ToString()}.{Expression.ToString()}";
        }
    }

    public class AccessObjectArrayNode : Node
    {
        public AccessObjectArrayNode(NumericAccessToken token)
        {
            Token = token;
        }
        public AccessObjectArrayNode(NumericAccessToken token, PropertyInfo propertyInfo)
            : this(token)
        {
            PropertyInfo = propertyInfo;
        }

        public NumericAccessToken Token { get; }

        public string ObjectName => Token.Name;

        public override Type ReturnType { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
        public PropertyInfo PropertyInfo { get; set; }

        public override string ToString()
        {
            return $"{ObjectName}[{Token.Index}]";
        }
    }

    public class PropertyValueNode : Node
    {
        public string Name { get; }

        public PropertyValueNode(string name)
        {
            Name = name;
        }
        public PropertyValueNode(string name, PropertyInfo propertyInfo)
        {
            Name = name;
            PropertyInfo = propertyInfo;
        }

        public override Type ReturnType { get; }
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
        public PropertyInfo PropertyInfo { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class AccessObjectKeyNode : Node
    {
        public AccessObjectKeyNode(KeyAccessToken token)
        {
            Token = token;
        }
        public AccessObjectKeyNode(KeyAccessToken token, PropertyInfo propertyInfo)
        : this(token)
        {
            PropertyInfo = propertyInfo;
        }

        public KeyAccessToken Token { get; }

        public string ObjectName => Token.Name;

        public override Type ReturnType { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public PropertyInfo PropertyInfo { get; }

        public override string ToString()
        {
            return $"{ObjectName}[{Token.Key}]";
        }
    }

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
