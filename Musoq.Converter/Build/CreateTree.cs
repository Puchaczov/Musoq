using System;
using Musoq.Converter.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Validation;

namespace Musoq.Converter.Build;

public class CreateTree(BuildChain successor) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items), "BuildItems cannot be null when creating AST tree.");

        if (string.IsNullOrWhiteSpace(items.RawQuery))
            throw AstValidationException.ForInvalidNodeStructure("Query", "CreateTree", "RawQuery is null or empty");

        try
        {
            // Early validation before expensive parsing
            var validator = new QueryValidator();
            validator.ValidateAndThrow(items.RawQuery);

            var lexer = new Lexer(items.RawQuery, true);
            var parser = new Parser.Parser(lexer);

            var rootNode = parser.ComposeAll();
            
            if (rootNode == null)
                throw AstValidationException.ForNullNode("RootNode", "CreateTree after parsing");

            items.RawQueryTree = rootNode;
        }
        catch (Exception ex) when (!(ex is AstValidationException))
        {
            throw new AstValidationException("Query", "CreateTree", $"Failed to parse SQL query: {ex.Message}", ex);
        }

        Successor?.Build(items);
    }
}