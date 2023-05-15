using System;
using System.Collections.Generic;
using System.Linq;

namespace Kits.API.Configuration;
public class GenericOptions<TProvider> where TProvider : class
{
    private readonly List<(Type implementationService, string name)> m_KitProviders = new();
    public IReadOnlyCollection<(Type implementationService, string name)> KitProviders => m_KitProviders.AsReadOnly();

    public void AddProvider<T>(string name) where T : TProvider
    {
        AddProvider(typeof(T), name);
    }

    public void AddProvider(Type type, string name)
    {
        if (!typeof(TProvider).IsAssignableFrom(type))
        {
            throw new Exception($"Type {type} must be an instance of {typeof(TProvider).Name}!");
        }

        if (m_KitProviders.Any(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        m_KitProviders.Add((type, name));
    }

    public void RemoveProvider<T>() where T : TProvider
    {
        RemoveProvider(typeof(T));
    }

    public void RemoveProvider(Type type)
    {
        m_KitProviders.RemoveAll(c => c.implementationService == type);
    }

    public Type? FindType(string type)
    {
        return m_KitProviders.Find(x => x.name.Equals(type, StringComparison.OrdinalIgnoreCase))
            .implementationService;
    }
}
