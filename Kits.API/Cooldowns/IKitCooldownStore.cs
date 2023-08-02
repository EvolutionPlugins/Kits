using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using System;
using System.Threading.Tasks;

namespace Kits.API.Cooldowns;

[Service]
public interface IKitCooldownStore
{
    Task<TimeSpan?> GetLastCooldownAsync(IPermissionActor actor, string kitName);

    Task RegisterCooldownAsync(IPermissionActor actor, string kitName, DateTime time);
}