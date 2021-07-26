using Autofac;
using JetBrains.Annotations;
using Kits.API;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.API.Ioc;
using OpenMod.API.Permissions;
using OpenMod.API.Prioritization;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.Providers
{
    [ServiceImplementation(Lifetime = ServiceLifetime.Transient, Priority = Priority.Lowest)]
    [UsedImplicitly]
    public class KitManager : IKitManager
    {
        private readonly IEconomyProvider m_EconomyProvider;
        private readonly Lazy<Kits> m_Kits;
        private readonly IKitCooldownStore m_KitCooldownStore;
        private readonly IKitDatabaseManager m_KitStore;
        private readonly IPermissionChecker m_PermissionChecker;
        private readonly IServiceProvider m_ServiceProvider;

        private IStringLocalizer m_StringLocalizer => m_Kits.Value.LifetimeScope.Resolve<IStringLocalizer>();

        public KitManager(Lazy<Kits> kits, IKitCooldownStore kitCooldownStore, IKitDatabaseManager kitStore, IEconomyProvider economyProvider,
            IPermissionChecker permissionChecker, IServiceProvider serviceProvider)
        {
            m_EconomyProvider = economyProvider;
            m_Kits = kits;
            m_KitCooldownStore = kitCooldownStore;
            m_KitStore = kitStore;
            m_PermissionChecker = permissionChecker;
            m_ServiceProvider = serviceProvider;
        }

        public async Task GiveKitAsync(IPlayerUser user, string name, ICommandActor? instigator = null,
            bool forceGiveKit = false)
        {
            var kit = await m_KitStore.FindKitByNameAsync(name);
            if (kit == null)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:notFound", new { Name = name }]);
            }

            if (!forceGiveKit && await m_PermissionChecker.CheckPermissionAsync(user,
                $"{KitDatabaseManager.c_KitsKey}.{kit.Name}") != PermissionGrantResult.Grant)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:noPermission", new { Kit = kit }]);
            }

            var cooldown = await m_KitCooldownStore.GetLastCooldownAsync(user, name);
            if (!forceGiveKit && cooldown != null)
            {
                if (cooldown.Value.TotalSeconds < kit.Cooldown)
                {
                    throw new UserFriendlyException(m_StringLocalizer["commands:kit:cooldown",
                        new { Kit = kit, Cooldown = kit.Cooldown - cooldown.Value.TotalSeconds }]);
                }
            }

            if (!forceGiveKit && kit.Cost != 0)
            {
                await m_EconomyProvider.UpdateBalanceAsync(user.Id, user.Type, -kit.Cost,
                    m_StringLocalizer["commands:kit:balanceUpdateReason:buy", new { Kit = kit }]);
            }

            await m_KitCooldownStore.RegisterCooldownAsync(user, name, DateTime.Now);

            await kit.GiveKitToPlayer(user, m_ServiceProvider);

            await user.PrintMessageAsync(m_StringLocalizer["commands:kit:success", new { Kit = kit }]);

            if (instigator != null)
            {
                await instigator.PrintMessageAsync(m_StringLocalizer["commands:kit:success", new { Kit = kit }]);
            }
        }

        public async Task<IReadOnlyCollection<Kit>> GetAvailableKitsForPlayerAsync(IPlayerUser player)
        {
            var list = new List<Kit>();
            foreach (var kit in await m_KitStore.GetKitsAsync())
            {
                if (await m_PermissionChecker.CheckPermissionAsync(player,
                    $"{KitDatabaseManager.c_KitsKey}.{kit.Name}") == PermissionGrantResult.Grant)
                {
                    list.Add(kit);
                }
            }

            return list;
        }
    }
}