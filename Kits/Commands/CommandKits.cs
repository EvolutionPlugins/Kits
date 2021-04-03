using Kits.API;
using Microsoft.Extensions.Localization;
using OpenMod.Core.Commands;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Kits.Commands
{
    [Command("kits")]
    [CommandActor(typeof(IPlayerUser))]
    public class CommandKits : Command
    {
        private readonly IKitManager m_KitManager;
        private readonly IStringLocalizer m_StringLocalizer;

        public CommandKits(IServiceProvider serviceProvider, IKitManager kitManager, IStringLocalizer stringLocalizer) :
            base(serviceProvider)
        {
            m_KitManager = kitManager;
            m_StringLocalizer = stringLocalizer;
        }

        protected override async Task OnExecuteAsync()
        {
            var playerUser = (IPlayerUser)Context.Actor;

            await PrintAsync(m_StringLocalizer["commands:kits", new
            {
                KitNames = string.Join(", ",
                    (await m_KitManager.GetAvailablePlayerKits(playerUser)).Select(x => x.Name))
            }]);
        }
    }
}
