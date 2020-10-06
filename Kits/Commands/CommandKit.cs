﻿using Kits.API;
using OpenMod.Core.Commands;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Threading.Tasks;

namespace Kits.Commands
{
    [Command("kit")]
    [CommandSyntax("<create|give|remove> <name>")]
    [CommandActor(typeof(IPlayerUser))]
    public class CommandKit : Command
    {
        private readonly IKitManager m_KitManager;

        public CommandKit(IServiceProvider serviceProvider, IKitManager kitManager) : base(serviceProvider)
        {
            m_KitManager = kitManager;
        }

        protected override Task OnExecuteAsync()
        {
            var playerUser = (IPlayerUser)Context.Actor;
            var kitName = Context.Parameters[0];
            return m_KitManager.GiveKit(playerUser.Player, kitName);
        }
    }
}