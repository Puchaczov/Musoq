using System;
using System.Collections.Generic;
using System.Reflection;

namespace MyTemplateNamespace
{
    internal static class T4Extensions
    {
        public static MethodInfo GetMethod(this Type type, string method, params Type[] parameters)
        {
            return type.GetRuntimeMethod(method, parameters);
        }
    }
}

namespace System.CodeDom.Compiler
{
    public class CompilerErrorCollection : List<CompilerError>
    {
    }

    public class CompilerError
    {
        public string ErrorText { get; set; }

        public bool IsWarning { get; set; }
    }
}
