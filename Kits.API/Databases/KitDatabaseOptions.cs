using System;
using System.Collections.Generic;
using System.Linq;

namespace Kits.API.Databases;
public class KitDatabaseOptions
{
    private readonly List<(Type implementationService, string name)> m_KitDatabaseProviders = new();
    public IReadOnlyCollection<(Type implementationService, string name)> KitDatabaseProviders => m_KitDatabaseProviders.AsReadOnly();

    public void AddProvider<TProvider>(string name) where TProvider : IKitDatabaseProvider
    {
        AddProvider(typeof(TProvider), name);
    }

    public void AddProvider(Type type, string name)
    {
        if (!typeof(IKitDatabaseProvider).IsAssignableFrom(type))
        {
            throw new Exception($"Type {type} must be an instance of IKitDatabaseProvider!");
        }

        if (m_KitDatabaseProviders.Any(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        m_KitDatabaseProviders.Add((type, name));
    }

    public void RemoveProvider<TProvider>() where TProvider : IKitDatabaseProvider
    {
        RemoveProvider(typeof(TProvider));
    }

    public void RemoveProvider(Type type)
    {
        m_KitDatabaseProviders.RemoveAll(c => c.implementationService == type);
    }

    public Type? GetPreferredDatabase(string type)
    {
        return m_KitDatabaseProviders.Find(x => x.name.Equals(type, StringComparison.OrdinalIgnoreCase))
            .implementationService;
    }
}
