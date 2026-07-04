using System.Reflection;

namespace Musoq.Evaluator.Build;

internal interface IAssemblyLoader
{
    Assembly Load(byte[] assemblyBytes);
}
