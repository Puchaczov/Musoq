using System;
using System.Collections.ObjectModel;
using System.Reflection;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Managers;

public class PropertiesManager : ManagerBase<MethodInfo>
{
    public ReadOnlyCollection<MethodInfo> Properties => Parts.AsReadOnly();

    public void RegisterProperties(LibraryBase library)
    {
        TryAddLibraryParts(library);
    }

    protected override bool CanReflectedPartBeQueryable(MethodInfo reflectedInfo)
    {
        var parameters = reflectedInfo.GetParameters();
        return parameters.Length == 1 && parameters[0].GetCustomAttributes(typeof(InjectSourceAttribute)) != null;
    }

    protected override MethodInfo[] GetReflectedInfos(Type type)
    {
        return type.GetMethods();
    }
}