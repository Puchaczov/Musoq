#nullable enable annotations

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Musoq.Converter.Exceptions;
using Musoq.Evaluator;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.Api;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Converter.Build;

public class BuildItems : Dictionary<string, object>
{
    public byte[] DllFile
    {
        get => (byte[])this["DLL_FILE"];
        set => this["DLL_FILE"] = value;
    }

    public byte[] PdbFile
    {
        get => (byte[])this["PDB_FILE"];
        set => this["PDB_FILE"] = value;
    }

    public RootNode TransformedQueryTree
    {
        get => (RootNode)this["TRANSFORMED_QUERY_TREE"];
        set => this["TRANSFORMED_QUERY_TREE"] = value;
    }

    public RootNode RawQueryTree
    {
        get => (RootNode)this["RAW_QUERY_TREE"];
        set => this["RAW_QUERY_TREE"] = value;
    }

    public string RawQuery
    {
        get => TryGetValue("RAW_QUERY", out var value) && value is string str
            ? str
            : throw AstValidationException.ForInvalidNodeStructure("BuildItems", "RawQuery access",
                "RawQuery is not set or is null");
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw AstValidationException.ForInvalidNodeStructure("BuildItems", "RawQuery setting",
                    "RawQuery cannot be null or whitespace");
            this["RAW_QUERY"] = value;
        }
    }

    public string AssemblyName
    {
        get => (string)this["ASSEMBLY_NAME"];
        set => this["ASSEMBLY_NAME"] = value;
    }

    public ISchemaProvider SchemaProvider
    {
        get => (ISchemaProvider)this["SCHEMA_PROVIDER"];
        set => this["SCHEMA_PROVIDER"] = value;
    }

    public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables
    {
        get => (IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>)this["ENVIRONMENT_VARIABLES"];
        set => this["ENVIRONMENT_VARIABLES"] = value;
    }

    public CSharpCompilation Compilation
    {
        get => (CSharpCompilation)this["COMPILATION"];
        set => this["COMPILATION"] = value;
    }

    public string AccessToClassPath
    {
        get => (string)this["ACCESS_TO_CLASS_PATH"];
        set => this["ACCESS_TO_CLASS_PATH"] = value;
    }

    public EmitResult EmitResult
    {
        get => (EmitResult)this["EMIT_RESULT"];
        set => this["EMIT_RESULT"] = value;
    }

    public IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]> UsedColumns
    {
        get => (IReadOnlyDictionary<SchemaFromNode, ISchemaColumn[]>)this["USED_COLUMNS"];
        set => this["USED_COLUMNS"] = value;
    }

    public IReadOnlyDictionary<SchemaFromNode, WhereNode> UsedWhereNodes
    {
        get => (IReadOnlyDictionary<SchemaFromNode, WhereNode>)this["USED_WHERE_NODES"];
        set => this["USED_WHERE_NODES"] = value;
    }

    public IReadOnlyDictionary<SchemaFromNode, QueryHints> QueryHintsPerSchema
    {
        get => (IReadOnlyDictionary<SchemaFromNode, QueryHints>)this["QUERY_HINTS_PER_SCHEMA"];
        set => this["QUERY_HINTS_PER_SCHEMA"] = value;
    }

    public Func<ISchemaProvider, IReadOnlyDictionary<string, string[]>, CompilationOptions, SchemaRegistry,
            BuildMetadataAndInferTypesVisitor>
        CreateBuildMetadataAndInferTypesVisitor
    {
        get =>
            (Func<ISchemaProvider, IReadOnlyDictionary<string, string[]>, CompilationOptions, SchemaRegistry,
                BuildMetadataAndInferTypesVisitor>)this[
                "CREATE_BUILD_METADATA_AND_INFER_TYPES_VISITOR"];
        set => this["CREATE_BUILD_METADATA_AND_INFER_TYPES_VISITOR"] = value;
    }

    public CompilationOptions CompilationOptions
    {
        get
        {
            if (!ContainsKey("COMPILATION_OPTIONS"))
                this["COMPILATION_OPTIONS"] = new CompilationOptions(ParallelizationMode.Full);

            return (CompilationOptions)this["COMPILATION_OPTIONS"];
        }
        set => this["COMPILATION_OPTIONS"] = value;
    }

    public SchemaRegistry SchemaRegistry
    {
        get => ContainsKey("SCHEMA_REGISTRY") ? (SchemaRegistry)this["SCHEMA_REGISTRY"] : null;
        set => this["SCHEMA_REGISTRY"] = value;
    }
    public string? InterpreterSourceCode
    {
        get => ContainsKey("INTERPRETER_SOURCE_CODE") ? (string)this["INTERPRETER_SOURCE_CODE"] : null;
        set => this["INTERPRETER_SOURCE_CODE"] = value;
    }

    public CteExecutionPlan? CteExecutionPlan
    {
        get => ContainsKey("CTE_EXECUTION_PLAN") ? (CteExecutionPlan)this["CTE_EXECUTION_PLAN"] : null;
        set => this["CTE_EXECUTION_PLAN"] = value!;
    }
}
