using System;
using System.Globalization;
using System.Reflection;

namespace Musoq.Parser.Nodes;

public class NullNode : Node
{
    public NullNode()
    {
        Id = nameof(NullNode);
        ReturnType = new NullType();
    }

    public NullNode(Type ofType)
    {
        Id = nameof(NullNode);
        ReturnType = ofType;
    }

    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return "null";
    }

    public class NullType : Type
    {
        private readonly Type _underlyingType = typeof(object);
        public static NullType Instance { get; } = new();

        public override Assembly Assembly => typeof(NullNode).Assembly;
        public override string AssemblyQualifiedName => _underlyingType.AssemblyQualifiedName;
        public override Type BaseType => _underlyingType.BaseType;
        public override string FullName => typeof(NullType).FullName;
        public override Guid GUID => typeof(NullType).GUID;
        public override Module Module => typeof(NullType).Module;
        public override string Namespace => typeof(NullType).Namespace;
        public override string Name => "Null";

        public override Type UnderlyingSystemType => _underlyingType.UnderlyingSystemType;

        protected override bool IsArrayImpl()
        {
            return false;
        }

        protected override bool IsByRefImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        protected override bool IsPointerImpl()
        {
            return false;
        }

        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return _underlyingType.Attributes;
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention,
            Type[] types, ParameterModifier[] modifiers)
        {
            return _underlyingType.GetConstructor(bindingAttr, binder, callConvention, types, modifiers);
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return _underlyingType.GetConstructors(bindingAttr);
        }

        public override Type GetElementType()
        {
            return _underlyingType.GetElementType();
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            return _underlyingType.GetEvent(name, bindingAttr);
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return _underlyingType.GetEvents(bindingAttr);
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            return _underlyingType.GetField(name, bindingAttr);
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return _underlyingType.GetFields(bindingAttr);
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return _underlyingType.GetMembers(bindingAttr);
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder,
            CallingConventions callConvention,
            Type[] types, ParameterModifier[] modifiers)
        {
            return _underlyingType.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return _underlyingType.GetMethods(bindingAttr);
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return _underlyingType.GetProperties(bindingAttr);
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target,
            object[] args,
            ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            return _underlyingType.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture,
                namedParameters);
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder,
            Type returnType, Type[] types,
            ParameterModifier[] modifiers)
        {
            return _underlyingType.GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
        }

        protected override bool HasElementTypeImpl()
        {
            return _underlyingType.HasElementType;
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            return _underlyingType.GetNestedType(name, bindingAttr);
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return _underlyingType.GetNestedTypes(bindingAttr);
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            return _underlyingType.GetInterface(name, ignoreCase);
        }

        public override Type[] GetInterfaces()
        {
            return _underlyingType.GetInterfaces();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return _underlyingType.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return _underlyingType.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return _underlyingType.IsDefined(attributeType, inherit);
        }
    }
}
