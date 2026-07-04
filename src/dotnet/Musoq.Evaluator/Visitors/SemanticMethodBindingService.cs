using System;
using System.Reflection;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticMethodBindingService(Action<Assembly> addAssembly)
{
    public void RegisterContextAssemblies(Type entityType)
    {
        addAssembly(entityType.Assembly);
        AddBaseTypeAssembly(entityType.BaseType);
    }

    private void AddBaseTypeAssembly(Type? entityType)
    {
        if (entityType == null)
            return;

        addAssembly(entityType.Assembly);
        AddBaseTypeAssembly(entityType.BaseType);
    }
}
