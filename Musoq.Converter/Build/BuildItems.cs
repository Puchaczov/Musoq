﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Converter.Build
{
    public class BuildItems : Dictionary<string, object>
    {
        public byte[] DllFile
        {
            get => (byte[]) this["DLL_FILE"];
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
            get => (string)this["RAW_QUERY"];
            set => this["RAW_QUERY"] = value;
        }

        public string AssemblyName
        {
            get => (string)this["ASSEMBLY_NAME"];
            set => this["ASSEMBLY_NAME"] = value;
        }

        public ISchemaProvider SchemaProvider
        {
            get => (ISchemaProvider) this["SCHEMA_PROVIDER"];
            set => this["SCHEMA_PROVIDER"] = value;
        }

        public IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>> PositionalEnvironmentVariables
        {
            get => (IReadOnlyDictionary<uint, IReadOnlyDictionary<string, string>>) this["ENVIRONMENT_VARIABLES"];
            set => this["ENVIRONMENT_VARIABLES"] = value;
        }

        public CSharpCompilation Compilation
        {
            get => (CSharpCompilation) this["COMPILATION"];
            set => this["COMPILATION"] = value;
        }

        public string AccessToClassPath
        {
            get => (string) this["ACCESS_TO_CLASS_PATH"];
            set => this["ACCESS_TO_CLASS_PATH"] = value;
        }

        public EmitResult EmitResult
        {
            get => (EmitResult) this["EMIT_RESULT"];
            set => this["EMIT_RESULT"] = value;
        }
    }
}