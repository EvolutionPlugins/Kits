using Kits.API.Cooldowns;
using OpenMod.API.Permissions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kits.Cooldowns.Providers;
public class MySqlKitCooldownStoreProvider : IKitCooldownStoreProvider
{
    public MySqlKitCooldownStoreProvider()
    {
    }

    public Task<TimeSpan?> GetLastCooldownAsync(IPermissionActor actor, string kitName)
    {
        throw new NotImplementedException();
    }

    public Task InitAsync()
    {
        throw new NotImplementedException();
    }

    public Task RegisterCooldownAsync(IPermissionActor actor, string kitName, DateTime time)
    {
        throw new NotImplementedException();
    }
}
