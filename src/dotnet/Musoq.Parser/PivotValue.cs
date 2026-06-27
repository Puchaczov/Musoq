using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

internal sealed record PivotValue(Node[] Expressions, string Alias);
