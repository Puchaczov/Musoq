using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Helpers;

public static partial class EvaluationHelper
{
    private static string GetMethodCategory(MethodInfo methodInfo)
    {
        var categoryAttr = methodInfo.GetCustomAttribute<MethodCategoryAttribute>();
        return categoryAttr?.Category ?? "Unknown";
    }

    private static string GetMethodSource(MethodInfo methodInfo)
    {
        var declaringType = methodInfo.DeclaringType;
        if (declaringType == null)
            return "Unknown";


        if (declaringType == typeof(LibraryBase))
            return "Library";

        return "Schema";
    }

    private static string GetXmlDocumentation(MethodInfo methodInfo)
    {
        try
        {
            var assembly = methodInfo.DeclaringType?.Assembly;
            if (assembly == null)
                return string.Empty;

            var assemblyPath = assembly.Location;
            if (string.IsNullOrEmpty(assemblyPath))
                return string.Empty;

            var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
            if (!File.Exists(xmlPath))
                return string.Empty;

            var xmlDoc = XmlDocCache.GetOrAdd(xmlPath, static path =>
            {
                var doc = new XmlDocument();
                doc.Load(path);
                return doc;
            });

            if (xmlDoc == null)
                return string.Empty;

            var memberName = GetMemberName(methodInfo);
            var node = xmlDoc.SelectSingleNode($"//member[@name='{memberName}']/summary");

            if (node == null)
                return string.Empty;

            var text = node.InnerText.Trim();
            text = WhitespaceNormalizerRegex.Replace(text, " ");
            return text;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetMemberName(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        if (declaringType == null)
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append("M:");
        sb.Append(declaringType.FullName);
        sb.Append('.');
        sb.Append(method.Name);

        var parameters = method.GetParameters();
        if (parameters.Length <= 0)
            return sb.ToString();

        sb.Append('(');
        for (var i = 0; i < parameters.Length; i++)
        {
            if (i > 0)
                sb.Append(',');

            var paramType = parameters[i].ParameterType;
            sb.Append(GetTypeName(paramType));
        }

        sb.Append(')');

        return sb.ToString();
    }

    private static string GetTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeName = type.GetGenericTypeDefinition().FullName ??
                                  type.GetGenericTypeDefinition().Name;
            var tickIndex = genericTypeName.IndexOf('`', StringComparison.Ordinal);
            if (tickIndex > 0)
                genericTypeName = genericTypeName.Substring(0, tickIndex);

            var genericArgs = type.GetGenericArguments();
            return $"{genericTypeName}{{{string.Join(",", genericArgs.Select(GetTypeName))}}}";
        }

        if (!type.IsArray)
            return type.FullName ?? type.Name;

        var elementType = type.GetElementType();
        return elementType is null ? type.Name : GetTypeName(elementType) + "[]";
    }
}
