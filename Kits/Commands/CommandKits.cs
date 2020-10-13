﻿using Kits.API;
using Kits.Providers;
using Microsoft.Extensions.Localization;
using OpenMod.API.Permissions;
using OpenMod.Core.Commands;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kits.Commands
{
    [Command("kits")]
    [CommandActor(typeof(IPlayerUser))]
    public class CommandKits : Command
    {
        private readonly IKitManager m_KitManager;
        private readonly IPermissionChecker m_PermissionChecker;
        private readonly IStringLocalizer m_StringLocalizer;

        public CommandKits(IServiceProvider serviceProvider, IKitManager kitManager,
            IPermissionChecker permissionChecker, IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            m_KitManager = kitManager;
            m_PermissionChecker = permissionChecker;
            m_StringLocalizer = stringLocalizer;
        }

        protected override async Task OnExecuteAsync()
        {
            var playerUser = (IPlayerUser)Context.Actor;
            var kits = await m_KitManager.GetRegisteredKitsAsync();
            var kitNames = new List<string>();
            foreach (var kit in kits)
            {
                if (await m_PermissionChecker.CheckPermissionAsync(playerUser, $"Kits:{KitManager.KITSKEY}.{kit.Name}")
                    == PermissionGrantResult.Grant)
                {
                    kitNames.Add(kit.Name);
                }
            }
            await PrintAsync(m_StringLocalizer["commands:kits", new { KitNames = string.Join(", ", kitNames) }]);
        }
    }
}
