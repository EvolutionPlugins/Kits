using System;
using System.Collections.Generic;

namespace Kits.API.Database
{
    public class KitDatabaseOptions
    {
        private readonly List<Type> m_KitDatabaseProviders = new();
        public IReadOnlyCollection<Type> KitDatabaseProviders => m_KitDatabaseProviders.AsReadOnly();

        public void AddProvider<TProvider>() where TProvider : IKitDatabaseProvider
        {
            AddProvider(typeof(TProvider));
        }

        public void AddProvider(Type type)
        {
            if (!typeof(IKitDatabaseProvider).IsAssignableFrom(type))
            {
                throw new Exception($"Type {type} must be an instance of IKitDatabaseProvider!");
            }

            if (m_KitDatabaseProviders.Contains(type))
            {
                return;
            }

            m_KitDatabaseProviders.Add(type);
        }

        public void RemoveProvider<TProvider>() where TProvider : IKitDatabaseProvider
        {
            AddProvider(typeof(TProvider));
        }

        public void RemoveProvider(Type type)
        {
            m_KitDatabaseProviders.RemoveAll(c => c == type);
        }

        public Type? GetPreferredDatabase(string type)
        {
            return m_KitDatabaseProviders.Find(x => x.Name.Equals(type, StringComparison.OrdinalIgnoreCase));
        }
    }
}
