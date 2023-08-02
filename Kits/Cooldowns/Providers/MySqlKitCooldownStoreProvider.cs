using Autofac;
using Kits.API.Cooldowns;
using Kits.Cooldowns.MySql;
using Microsoft.EntityFrameworkCore;
using OpenMod.API.Permissions;
using OpenMod.Core.Users;
using System;
using System.Threading.Tasks;

namespace Kits.Cooldowns.Providers;
public class MySqlKitCooldownStoreProvider : IKitCooldownStoreProvider
{
    private readonly ILifetimeScope m_LifetimeScope;

    public MySqlKitCooldownStoreProvider(ILifetimeScope lifetimeScope)
    {
        m_LifetimeScope = lifetimeScope;
    }

    public async Task InitAsync()
    {
        await using var context = GetDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task<TimeSpan?> GetLastCooldownAsync(IPermissionActor actor, string kitName)
    {
        EnsureActorIsPlayer(actor);

        await using var context = GetDbContext();
        DateTime? usedTime = (await context.KitCooldowns.FirstOrDefaultAsync(c => c.Kit == kitName && c.PlayerId == actor.Id))
            ?.UsedTime;

        return usedTime == null ? null : DateTime.UtcNow - usedTime;
    }

    public async Task RegisterCooldownAsync(IPermissionActor actor, string kitName, DateTime time)
    {
        EnsureActorIsPlayer(actor);

        await using var context = GetDbContext();

        var cooldown = await context.KitCooldowns.FirstOrDefaultAsync(c => c.Kit == kitName && c.PlayerId == actor.Id);
        if (cooldown == null)
        {
            cooldown = new()
            {
                Kit = kitName,
                PlayerId = actor.Id,
                UsedTime = time
            };

            context.KitCooldowns.Add(cooldown);
        }
        else
        {
            cooldown.UsedTime = time;
        }

        await context.SaveChangesAsync();
    }

    private void EnsureActorIsPlayer(IPermissionActor actor)
    {
        if (!actor.Type.Equals(KnownActorTypes.Player, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Cooldowns are only handled for Player actor type");
        }
    }

    protected virtual KitCooldownsDbContext GetDbContext() => m_LifetimeScope.Resolve<KitCooldownsDbContext>();
}
