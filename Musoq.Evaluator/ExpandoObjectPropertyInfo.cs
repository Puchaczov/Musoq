using System;
using System.Dynamic;
using System.Globalization;
using System.Reflection;

namespace Musoq.Evaluator;

internal sealed class ExpandoObjectPropertyInfo : PropertyInfo
{
    public ExpandoObjectPropertyInfo(string name, Type propertyType)
    {
        Name = name;
        ReflectedType = typeof(ExpandoObject);
        PropertyType = propertyType;
    }

    public override PropertyAttributes Attributes => PropertyAttributes.None;
    public override bool CanRead => true;
    public override bool CanWrite => false;
    
    public override Type PropertyType { get; }

    public override object[] GetCustomAttributes(bool inherit)
    {
        return [];
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        return [];
    }

    public override bool IsDefined(Type attributeType, bool inherit)
    {
        return false;
    }

    public override Type DeclaringType => typeof(ExpandoObject);
    
    public override string Name { get; }
    
    public override Type ReflectedType { get; }
    
    public override MethodInfo[] GetAccessors(bool nonPublic)
    {
        return [];
    }

    public override MethodInfo GetGetMethod(bool nonPublic)
    {
        throw new NotImplementedException();
    }

    public override ParameterInfo[] GetIndexParameters()
    {
        return [];
    }

    public override MethodInfo GetSetMethod(bool nonPublic)
    {
        throw new NotImplementedException();
    }

    public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index,
        CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}