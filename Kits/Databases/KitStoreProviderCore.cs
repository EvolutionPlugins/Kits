﻿using Autofac;
using Microsoft.Extensions.Localization;
using OpenMod.API.Persistence;

namespace Kits.Databases;

public abstract class KitStoreProviderCore
{
    protected IDataStore DataStore { get; }
    protected IStringLocalizer StringLocalizer { get; }
    protected ILifetimeScope LifetimeScope { get; }

    protected KitStoreProviderCore(ILifetimeScope lifetimeScope)
    {
        StringLocalizer = lifetimeScope.Resolve<IStringLocalizer>();
        DataStore = lifetimeScope.Resolve<IDataStore>();
        LifetimeScope = lifetimeScope;
    }
}