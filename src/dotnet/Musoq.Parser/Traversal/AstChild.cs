using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Parser.Nodes;

namespace Musoq.Parser.Traversal;

internal sealed record AstChild(string Path, Node Node);
