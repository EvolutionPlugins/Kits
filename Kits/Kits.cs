using Kits.Models;
using Kits.Providers;
using Microsoft.Extensions.Logging;
using OpenMod.API.Permissions;
using OpenMod.API.Plugins;
using OpenMod.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[assembly: PluginMetadata("Kits", DisplayName = "Kits", Author = "DiFFoZ",
    Website = "https://github.com/DiFFoZ/Kits \\ https://discord.gg/6KymqGv")]

namespace Kits
{
    public class Kits : OpenModUniversalPlugin
    {
        public const string NOCOOLDOWNPERMISSION = "nocooldown";

        private readonly ILogger<Kits> m_Logger;
        private readonly IPermissionRegistry m_PermissionRegistry;

        public Kits(ILogger<Kits> logger, IServiceProvider serviceProvider, IPermissionRegistry permissionRegistry) : base(serviceProvider)
        {
            m_Logger = logger;
            m_PermissionRegistry = permissionRegistry;
        }

        protected override async Task OnLoadAsync()
        {
            // Kits:nocooldown
            m_PermissionRegistry.RegisterPermission(this, NOCOOLDOWNPERMISSION, "Allows use kit without waiting for cooldown");
            m_Logger.LogInformation("Made with <3 by DiFFoZ");
            m_Logger.LogInformation("https://github.com/evolutionplugins \\ https://github.com/diffoz");
            m_Logger.LogInformation("Discord: DiFFoZ#6745 \\ https://discord.gg/6KymqGv");

            if (!await DataStore.ExistsAsync(KitManager.KITSKEY))
            {
                await DataStore.SaveAsync(KitManager.KITSKEY, new KitsData
                {
                    Kits = new List<Kit>()
                });
            }
            if (!await DataStore.ExistsAsync(KitManager.COOLDOWNKEY))
            {
                await DataStore.SaveAsync(KitManager.COOLDOWNKEY, new KitsCooldownData
                {
                    KitsCooldown = new Dictionary<string, List<KitCooldownData>>()
                });
            }
        }
    }
}
