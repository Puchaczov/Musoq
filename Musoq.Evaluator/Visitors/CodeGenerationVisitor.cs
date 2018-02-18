using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Instructions;
using Musoq.Evaluator.Instructions.Arythmetic;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors
{
    public class CodeGenerationVisitor : IExpressionVisitor
    {
        private readonly IList<ByteCodeInstruction> _instructions;

        private readonly Dictionary<string, Label> _labels;
        private readonly Stack<int> _orders = new Stack<int>();
        private readonly ISchemaProvider _schemaProvider;
        private readonly Stack<GeneratorSelectScope> _selectScope = new Stack<GeneratorSelectScope>();

        private readonly Dictionary<string, TableMetadata> _tableMetadatas;

        private int _order;
        private readonly bool isSingleQueryMode = true;

        public CodeGenerationVisitor(ISchemaProvider schemaProvider,
            Dictionary<string, TableMetadata> tableMetadatas)
        {
            _instructions = new List<ByteCodeInstruction>();
            _schemaProvider = schemaProvider;
            _tableMetadatas = tableMetadatas;
            _labels = new Dictionary<string, Label>();
        }

        public VirtualMachine VirtualMachine { get; private set; }
        private GeneratorSelectScope SelectScope => _selectScope.Peek();

        private string AnotherValueFromSourceLabelName
            => $"{SelectScope.Name}.GetAnotherValueFromSource";

        private string WhereClauseBeginsLabelName
            => $"{SelectScope.Name}.WhereClauseBegins";

        private string EndOfQueryProcessing
            => $"{SelectScope.Name}.EndOfQueryProcessing";

        public void Visit(Node node)
        {
        }

        public void Visit(StarNode node)
        {
            GenerateConvertingInstructions(node);
            switch (node.ReturnType.Name)
            {
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new PerformActionInstruction((f) => f.Current.LongsStack.Push(f.Current.LongsStack.Pop() * f.Current.LongsStack.Pop()), "MUL LNGS"));
                    break;
                case nameof(Decimal):
                    _instructions.Add(new PerformActionInstruction(f => f.Current.NumericsStack.Push(f.Current.NumericsStack.Pop() * f.Current.NumericsStack.Pop()), "MUL DECS"));
                    break;
            }
        }

        public void Visit(FSlashNode node)
        {
            GenerateConvertingInstructions(node);
            switch (node.ReturnType.Name)
            {
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new PerformActionInstruction(f =>
                    {
                        var b = f.Current.LongsStack.Pop();
                        var a = f.Current.LongsStack.Pop();
                        f.Current.LongsStack.Push(a / b);
                    }, "DIV LNGS"));
                    break;
                case nameof(Decimal):
                    _instructions.Add(new PerformActionInstruction(f =>
                    {
                        var b = f.Current.NumericsStack.Pop();
                        var a = f.Current.NumericsStack.Pop();
                        f.Current.NumericsStack.Push(a / b);
                    }, "DIV DECS"));
                    break;
            }
        }

        public void Visit(ModuloNode node)
        {
            GenerateConvertingInstructions(node);
            switch (node.ReturnType.Name)
            {
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new PerformActionInstruction(f =>
                    {
                        var b = f.Current.LongsStack.Pop();
                        var a = f.Current.LongsStack.Pop();
                        f.Current.LongsStack.Push(a % b);
                    }, "MOD LNGS"));
                    break;
                case nameof(Decimal):
                    _instructions.Add(new PerformActionInstruction(f =>
                    {
                        var b = f.Current.NumericsStack.Pop();
                        var a = f.Current.NumericsStack.Pop();
                        f.Current.NumericsStack.Push(a % b);
                    }, "MOD DECS"));
                    break;
            }
        }

        public void Visit(AddNode node)
        {
            GenerateConvertingInstructions(node);
            switch (node.ReturnType.Name)
            {
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new PerformActionInstruction((f) => f.Current.LongsStack.Push(f.Current.LongsStack.Pop() + f.Current.LongsStack.Pop()), "ADD LNGS"));
                    break;
                case nameof(Decimal):
                    _instructions.Add(new PerformActionInstruction(f => f.Current.NumericsStack.Push(f.Current.NumericsStack.Pop() + f.Current.NumericsStack.Pop()), "ADD DECS"));
                    break;
            }
        }

        public void Visit(HyphenNode node)
        {
            GenerateConvertingInstructions(node);
            switch (node.ReturnType.Name)
            {
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new PerformActionInstruction(f =>
                    {
                        var b = f.Current.LongsStack.Pop();
                        var a = f.Current.LongsStack.Pop();
                        f.Current.LongsStack.Push(a - b);
                    }, "SUB LNGS"));
                    break;
                case nameof(Decimal):
                    _instructions.Add(new PerformActionInstruction(f =>
                    {
                        var b = f.Current.NumericsStack.Pop();
                        var a = f.Current.NumericsStack.Pop();
                        f.Current.NumericsStack.Push(a - b);
                    }, "SUB DECS"));
                    break;
            }
        }

        public void Visit(AndNode node)
        {
        }

        public void Visit(OrNode node)
        {
        }

        public void Visit(ShortCircuitingNodeLeft node)
        {
            ByteCodeInstruction instruction;
            var order = _order++;
            _orders.Push(order);

            switch (node.UsedFor)
            {
                case TokenType.And:
                    instruction = new JmpState(_labels, $"loadfalse{order}", false);
                    break;
                case TokenType.Or:
                    instruction = new JmpState(_labels, $"loadtrue{order}", true);
                    break;
                default:
                    throw new NotSupportedException();
            }

            _instructions.Add(instruction);
        }

        public void Visit(ShortCircuitingNodeRight node)
        {
            var order = _orders.Pop();

            _instructions.Add(new Jmp(_labels, $"endofexp{order}"));

            switch (node.UsedFor)
            {
                case TokenType.And:
                    _instructions.Add(new LoadBoolean(false));
                    _labels.Add($"loadfalse{order}", new Label(_instructions.Count - 1));
                    break;
                case TokenType.Or:
                    _instructions.Add(new LoadBoolean(true));
                    _labels.Add($"loadtrue{order}", new Label(_instructions.Count - 1));
                    break;
                default:
                    throw new NotSupportedException();
            }

            _labels.Add($"endofexp{order}", new Label(_instructions.Count));
        }

        public void Visit(EqualityNode node)
        {
            GenerateConvertingInstructions(node);
            switch (node.ReturnType.Name)
            {
                case nameof(String):
                    _instructions.Add(new EqualStringInstruction());
                    break;
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new EqualLongInstruction());
                    break;
                case nameof(Decimal):
                    _instructions.Add(new EqualDecimalInstruction());
                    break;
                default:
                    _instructions.Add(new EqualObjectInstruction());
                    break;
            }
        }

        public void Visit(GreaterOrEqualNode node)
        {
            GenerateConvertingInstructions(node);
            switch (node.ReturnType.Name)
            {
                case nameof(String):
                    _instructions.Add(new GreaterOrEqualStringInstruction());
                    break;
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new GreaterOrEqualLongsInstruction());
                    break;
                case nameof(Decimal):
                    _instructions.Add(new GreaterOrEqualNumericInstruction());
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void Visit(LessOrEqualNode node)
        {
            GenerateConvertingInstructions(node);
            switch (node.ReturnType.Name)
            {
                case nameof(String):
                    _instructions.Add(new LessOrEqualStringInstruction());
                    break;
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new LessOrEqualLongsInstruction());
                    break;
                case nameof(Decimal):
                    _instructions.Add(new LessOrEqualNumericInstruction());
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void Visit(GreaterNode node)
        {
            GenerateConvertingInstructions(node);
            switch (node.ReturnType.Name)
            {
                case nameof(String):
                    _instructions.Add(new GreaterStringInstruction());
                    break;
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new GreaterLongInstruction());
                    break;
                case nameof(Decimal):
                    _instructions.Add(new GreaterDecimalInstruction());
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void Visit(LessNode node)
        {
            GenerateConvertingInstructions(node);
            switch (node.ReturnType.Name)
            {
                case nameof(String):
                    _instructions.Add(new LessStringInstruction());
                    break;
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new LessLongInstruction());
                    break;
                case nameof(Decimal):
                    _instructions.Add(new LessDecimalInstruction());
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void Visit(DiffNode node)
        {
            GenerateConvertingInstructions(node);
            switch (node.ReturnType.Name)
            {
                case nameof(String):
                    _instructions.Add(new DiffStringInstruction());
                    break;
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new DiffLongInstruction());
                    break;
                case nameof(Decimal):
                    _instructions.Add(new DiffDecimalInstruction());
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void Visit(NotNode node)
        {
            switch (node.ReturnType.Name)
            {
                case nameof(String):
                    break;
                case nameof(Boolean):
                    _instructions.Add(new NegateBooleanInstruction());
                    break;
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    break;
                case nameof(Decimal):
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void Visit(LikeNode node)
        {
            _instructions.Add(new LikeInstruction());
        }

        public void Visit(FieldNode node)
        {
        }

        public void Visit(SelectNode node)
        {
            if(node.Fields.Length == 0)
                return;

            _instructions.Add(new GrabRow(node.Fields.Select(f => f.ReturnType).ToArray()));
        }

        public void Visit(StringNode node)
        {
            _instructions.Add(new LoadString(node.Value));
        }

        public void Visit(DecimalNode node)
        {
            _instructions.Add(new LoadNumeric(node.Value));
        }

        public void Visit(IntegerNode node)
        {
            _instructions.Add(new LoadLong(node.Value));
        }

        public void Visit(WordNode node)
        {
            _instructions.Add(new LoadString(node.Value));
        }

        public void Visit(ContainsNode node)
        {
            _instructions.Add(new LoadLong(node.ToCompareExpression.Args.Length));
            _instructions.Add(new PopToRegister(Register.A));

            switch (node.ReturnType.Name)
            {
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    _instructions.Add(new Contains<long>(Register.A, frame => frame.LongsStack.Pop()));
                    break;
                case nameof(Decimal):
                    _instructions.Add(new Contains<decimal>(Register.A, frame => frame.NumericsStack.Pop()));
                    break;
                case nameof(String):
                    _instructions.Add(new Contains<string>(Register.A, frame => frame.StringsStack.Pop()));
                    break;
                default:
                    _instructions.Add(new Contains<object>(Register.A, frame => frame.ObjectsStack.Pop()));
                    break;
            }
        }

        public void Visit(AccessMethodNode node)
        {
            var method = node.Method;
            _instructions.Add(new PrepareMethodCall(method, Activator.CreateInstance(method.ReflectedType)));
            _instructions.Add(new AccessMethod(method.GetParameters().Length, method));
        }

        public void Visit(GroupByAccessMethodNode node)
        {
            var method = node.Method;
            _instructions.Add(new PrepareMethodCall(method, Activator.CreateInstance(method.ReflectedType)));
            _instructions.Add(new AccessMethod(method.GetParameters().Length, method));
        }

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
            var method = node.Method;
            _instructions.Add(new PrepareMethodCall(method, Activator.CreateInstance(method.ReflectedType)));
            _instructions.Add(new AccessMethod(method.GetParameters().Length, method));
        }

        public void Visit(AccessColumnNode node)
        {
            _instructions.Add(new AccessColumn(node.Name, node.ReturnType));
        }

        public void Visit(AllColumnsNode node)
        {
        }

        public void Visit(AccessObjectArrayNode node)
        {
            _instructions.Add(new AccessProperty(node.PropertyInfo, node.Token.Index));
        }

        public void Visit(AccessObjectKeyNode node)
        {
            _instructions.Add(new AccessProperty(node.PropertyInfo, node.Token.Value));
        }

        public void Visit(PropertyValueNode node)
        {
            _instructions.Add(new AccessProperty(node.PropertyInfo, null));
        }

        public void Visit(AccessPropertyNode node)
        {
            _instructions.Add(new AccessProperty(node.PropertyInfo, null));
        }

        public void Visit(AccessCallChainNode node)
        {
            _instructions.Add(new AccessColumn(node.ColumnName, node.ColumnType));
            _instructions.Add(new AccessCallChain(node.Props));
        }

        public void Visit(ArgsListNode node)
        {
            //_instructions.Add(new LoadNumeric<T>(node.Args.Length)); ??
        }

        public void Visit(WhereNode node)
        {
            _instructions.Add(new JmpState(_labels, AnotherValueFromSourceLabelName, false));
        }

        public void Visit(GroupByNode node)
        {
            _instructions.Add(new CreateGroupIdentifier(node.Fields.Length, node.Fields.Select(f => f.ReturnType).ToArray()));
            _instructions.Add(new AccessGroup(node.Fields.Length, node.Fields));
        }

        public void Visit(HavingNode node)
        {
            _instructions.Add(new JmpState(_labels, AnotherValueFromSourceLabelName, false));
        }

        public void Visit(SkipNode node)
        {
            _instructions.Add(new SkipRows(node.Value, _labels, EndOfQueryProcessing));
            _labels[WhereClauseBeginsLabelName].StartIndex += 1;
        }

        public void Visit(TakeNode node)
        {
            _instructions.Add(new CheckTableRowsAmount(node.Value, _labels, EndOfQueryProcessing));
        }

        public void Visit(ExistingTableFromNode node)
        {
            var scope = new GeneratorSelectScope
            {
                Name = node.Id,
                Alias = node.Schema
            };
            _selectScope.Push(scope);

            _instructions.Add(new UseTableWithStandardColumns(node.Schema));
            _instructions.Add(new GrabFirstValueFromSource(_labels, AnotherValueFromSourceLabelName));
            _labels.Add(WhereClauseBeginsLabelName, new Label(_instructions.Count));
        }

        public void Visit(SchemaFromNode node)
        {
            var scope = new GeneratorSelectScope
            {
                Name = node.Id,
                Alias = node.Alias
            };
            _selectScope.Push(scope);

            var schema = _schemaProvider.GetSchema(node.Schema);
            var source = schema.GetRowSource(node.Method, node.Parameters);
            var enumarble = source.Rows;
            _instructions.Add(new LoadSource(enumarble));
            _instructions.Add(new GrabFirstValueFromSource(_labels, AnotherValueFromSourceLabelName));
            _labels.Add(WhereClauseBeginsLabelName, new Label(_instructions.Count));
        }

        public void Visit(NestedQueryFromNode node)
        {
            var scope = new GeneratorSelectScope
            {
                Name = node.Id,
                Alias = node.Schema
            };
            _selectScope.Push(scope);

            _instructions.Add(new UseTableWithRemappedColumns(node.Schema, node.ColumnToIndexMap));
            _instructions.Add(new GrabFirstValueFromSource(_labels, AnotherValueFromSourceLabelName));
            _labels.Add(WhereClauseBeginsLabelName, new Label(_instructions.Count));
        }

        public void Visit(CreateTableNode node)
        {
            _instructions.Add(new LoadTable(node.Schema, node.Keys, _tableMetadatas[node.Schema].Columns.ToArray()));
        }

        public void Visit(TranslatedSetTreeNode node)
        {
        }

        public void Visit(IntoNode node)
        {
            _instructions.Add(new AddNewRow(node.Name));
        }

        public void Visit(IntoGroupNode node)
        {
            _instructions.Add(new AddNewGroup(node.Name, node.ColumnToValue));
        }

        public void Visit(ShouldBePresentInTheTable node)
        {
            var table = _tableMetadatas[node.Table];
            var indexedColumns = new List<int>();

            for (var i = 0; i < table.Indexes.Count; ++i)
            {
                var stringifiedField = table.Indexes.ElementAt(i);
                for (var j = 0; j < table.Columns.Count && indexedColumns.Count <= table.Indexes.Count; ++j)
                {
                    var orderedField = table.Columns.ElementAt(j);

                    if (orderedField.Name != stringifiedField) continue;

                    indexedColumns.Add(orderedField.ColumnOrder);
                    break;
                }
            }

            _instructions.Add(new CheckTableHasKey(node.Table, indexedColumns.ToArray(), true));
            _instructions.Add(new JmpState(_labels, AnotherValueFromSourceLabelName, node.ExpectedResult));
            //TO DO: Remove grabbed row from stack if doesn't required anymore.
        }

        public void Visit(TranslatedSetOperatorNode node)
        {
        }

        public void Visit(QueryNode node)
        {
            _instructions.Add(new MoveToAnotherValueFromSource());
            _labels.Add(AnotherValueFromSourceLabelName, new Label(_instructions.Count - 1));
            _instructions.Add(new JmpState(_labels, WhereClauseBeginsLabelName, true));
        }

        public void Visit(InternalQueryNode node)
        {
            Visit((QueryNode) node);
            _labels.Add(EndOfQueryProcessing, new Label(_instructions.Count));
            if (node.ShouldLoadResultTableAsResult && isSingleQueryMode)
                _instructions.Add(new LoadString(node.ResultTable));
        }

        public void Visit(RootNode node)
        {
            _instructions.Add(new Exit());
            VirtualMachine = new VirtualMachine(_instructions.ToArray());
        }

        public void Visit(UnionNode node)
        {
        }

        public void Visit(UnionAllNode node)
        {
        }

        public void Visit(ExceptNode node)
        {
        }

        public void Visit(RefreshNode node)
        {
        }

        public void Visit(IntersectNode node)
        {
        }

        public void Visit(PutTrueNode node)
        {
            _instructions.Add(new LoadBoolean(true));
        }

        public void Visit(MultiStatementNode node)
        {
        }

        private void GenerateConvertingInstructions(BinaryNode node)
        {
            foreach (var inst in EvaluationHelper.GetConvertingInstructions(node.Left.ReturnType,
                node.Right.ReturnType))
            {
                _instructions.Add(inst);
            }
        }

        private class GeneratorSelectScope
        {
            public string Name { get; set; }
            public string Alias { get; set; }
        }
    }
}