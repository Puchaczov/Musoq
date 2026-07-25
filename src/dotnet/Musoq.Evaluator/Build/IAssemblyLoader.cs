namespace Musoq.Evaluator.Build;

internal interface IAssemblyLoader
{
    LoadedAssemblyHandle Load(byte[] assemblyBytes);
}
