using System;
using Musoq.Parser.Nodes;
using Musoq.Plugins;

namespace Musoq.Evaluator.Instructions
{
    public class AccessGroup : ByteCodeInstruction
    {
        private readonly FieldNode[] _fields;
        private readonly int _fieldsLength;

        public AccessGroup(int fieldsLength, FieldNode[] fields)
        {
            _fieldsLength = fieldsLength;
            _fields = fields;
        }

        public override void Execute(IVirtualMachine virtualMachine)
        {
            var len = _fieldsLength;
            var ids = new string[len];

            for (var i = 0; i < len; i++) ids[i] = virtualMachine.Current.StringsStack.Pop();

            var allValues = new object[len];
            for (var i = 0; i < len; i++)
            {
                switch (_fields[i].ReturnType.Name)
                {
                    case nameof(Int16):
                        allValues[i] = (short)virtualMachine.Current.LongsStack.Pop();
                        break;
                    case nameof(Int32):
                        allValues[i] = (int)virtualMachine.Current.LongsStack.Pop();
                        break;
                    case nameof(Int64):
                        allValues[i] = virtualMachine.Current.LongsStack.Pop();
                        break;
                    case nameof(Decimal):
                        allValues[i] = virtualMachine.Current.NumericsStack.Pop();
                        break;
                    case nameof(String):
                        allValues[i] = virtualMachine.Current.StringsStack.Pop();
                        break;
                    case nameof(Boolean):
                        allValues[i] = virtualMachine.Current.BooleanStack.Pop();
                        break;
                    case nameof(Object):
                        allValues[i] = virtualMachine.Current.ObjectsStack.Pop();
                        break;
                }
            }

            for (var i = 0; i < ids.Length; i++)
            {
                var groupValues = new object[i + 1];
                var groupFieldNames = new string[i + 1];

                for (var j = 0; j <= i; j++)
                {
                    groupValues[j] = allValues[j];
                    groupFieldNames[j] = _fields[j].FieldName;
                }

                var parentId = i > 0 ? ids[i - 1] : string.Empty;

                var groups = virtualMachine.Current.Groups;

                var parent = groups.ContainsKey(parentId) ? groups[parentId] : groups["root"];

                var id = ids[i];
                if (!groups.ContainsKey(id))
                    groups.Add(id, new Group(parent, groupFieldNames, groupValues));

                var group = groups[id];
                group.Hit();

                virtualMachine.Current.CurrentGroup = group;
            }

            virtualMachine[Register.Ip] += 1;
        }

        public override string DebugInfo()
        {
            return "ACCESS GROUP";
        }
    }
}