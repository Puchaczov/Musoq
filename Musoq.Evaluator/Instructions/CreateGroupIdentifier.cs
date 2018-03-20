using System;
using System.Text;

namespace Musoq.Evaluator.Instructions
{
    public class CreateGroupIdentifier : ByteCodeInstruction
    {
        private readonly int _groupCount;
        private readonly Type[] _types;

        public CreateGroupIdentifier(int groupCount, Type[] types)
        {
            _groupCount = groupCount;
            _types = types;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var values = new (object Value, string Name)[_groupCount];
            for (var i = _groupCount - 1; i >= 0; i--)
            {
                var type = _types[i];
                switch (type.Name)
                {
                    case nameof(Int16):
                        values[i] = ((short)virtualMachine.Current.LongsStack.Pop(), type.Name);
                        break;
                    case nameof(Int32):
                        values[i] = ((int)virtualMachine.Current.LongsStack.Pop(), type.Name);
                        break;
                    case nameof(Int64):
                        values[i] = (virtualMachine.Current.LongsStack.Pop(), type.Name);
                        break;
                    case nameof(Decimal):
                        values[i] = (virtualMachine.Current.NumericsStack.Pop(), type.Name);
                        break;
                    case nameof(String):
                        values[i] = (virtualMachine.Current.StringsStack.Pop(), type.Name);
                        break;
                    case nameof(Boolean):
                        values[i] = (virtualMachine.Current.BooleanStack.Pop(), type.Name);
                        break;
                    default:
                        values[i] = (virtualMachine.Current.ObjectsStack.Pop(), type.Name);
                        break;
                }
            }

            var identifiers = new string[_types.Length];
            for (var i = _types.Length - 1; i >= 0; --i)
            {
                var identifier = new StringBuilder();
                for (var j = 0; j < i; j++)
                {
                    var value = values[j];

                    identifier.Append(value.Name);
                    identifier.Append(':');
                    identifier.Append(value.Value);
                    identifier.Append(',');
                }

                identifier.Append(values[i].Name);
                identifier.Append(':');
                identifier.Append(values[i].Value);

                identifiers[i] = identifier.ToString();
            }

            for (var i = values.Length - 1; i >= 0; --i)
            {
                var type = values[i].Name;
                switch (type)
                {
                    case nameof(Int16):
                        virtualMachine.Current.LongsStack.Push((short) values[i].Value);
                        break;
                    case nameof(Int32):
                        virtualMachine.Current.LongsStack.Push((int) values[i].Value);
                        break;
                    case nameof(Int64):
                        virtualMachine.Current.LongsStack.Push((long) values[i].Value);
                        break;
                    case nameof(Decimal):
                        virtualMachine.Current.NumericsStack.Push((decimal) values[i].Value);
                        break;
                    case nameof(String):
                        virtualMachine.Current.StringsStack.Push((string) values[i].Value);
                        break;
                    case nameof(Boolean):
                        virtualMachine.Current.BooleanStack.Push((bool) values[i].Value);
                        break;
                    default:
                        virtualMachine.Current.ObjectsStack.Push(values[i].Value);
                        break;
                }
            }

            for (var i = identifiers.Length - 1; i >= 0; i--) virtualMachine.Current.StringsStack.Push(identifiers[i]);

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return $"CREATE GROUPS OF {_groupCount}";
        }
    }
}