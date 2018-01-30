using System.Reflection;

namespace Musoq.Evaluator.Instructions
{
    public class AccessProperty : AcessObject
    {
        public AccessProperty(PropertyInfo property, object arg)
            : base(property.PropertyType)
        {
            Property = property;
            Arg = arg;
        }

        protected PropertyInfo Property { get; }

        private object Arg { get; }

        public override string DebugInfo()
        {
            return $"ACESS {Property.Name}";
        }

        protected override object GetValue(object obj)
        {
            if(Arg != null)
                return Property.GetValue(obj, new []{ Arg });

            return Property.GetValue(obj);
        }
    }
}