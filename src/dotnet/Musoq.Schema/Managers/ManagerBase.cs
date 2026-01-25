using System;
using System.Collections.Generic;
using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Schema.Managers;

public abstract class ManagerBase<TReflectedType>
{
    protected readonly List<TReflectedType> Parts;

    protected ManagerBase()
    {
        Parts = [];
    }

    protected bool TryAddLibraryParts(object library)
    {
        var type = library.GetType();

        if (type.GetCustomAttribute<BindableClassAttribute>() == null)
            return false;

        foreach (var reflectedInfo in GetReflectedInfos(type))
        {
            if (!CanReflectedPartBeQueryable(reflectedInfo))
                continue;

            Parts.Add(reflectedInfo);
        }

        return true;
    }

    public bool TryAdd(object library)
    {
        return TryAddLibraryParts(library);
    }

    protected abstract bool CanReflectedPartBeQueryable(TReflectedType reflectedInfo);

    protected abstract TReflectedType[] GetReflectedInfos(Type type);
}
