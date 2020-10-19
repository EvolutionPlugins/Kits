using Kits.API;
using Kits.Extensions;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.Core.Commands;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using IHasInventory = OpenMod.Extensions.Games.Abstractions.Entities.IHasInventory;

namespace Kits.Commands
{
    [Command("create")]
    [CommandAlias("add")]
    [CommandAlias("+")]
    [CommandActor(typeof(IPlayerUser))]
    [CommandParent(typeof(CommandKit))]
    [CommandSyntax("<name> [cooldown]")]
    public class CommandKitCreate : Command
    {
        private readonly IKitManager m_KitManager;
        private readonly IStringLocalizer m_StringLocalizer;

        public CommandKitCreate(IServiceProvider serviceProvider, IKitManager kitManager,
            IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            m_KitManager = kitManager;
            m_StringLocalizer = stringLocalizer;
        }

        protected override async Task OnExecuteAsync()
        {
            var playerUser = (IPlayerUser)Context.Actor;
            if (Context.Parameters.Count < 1)
            {
                throw new CommandWrongUsageException(Context);
            }
            var name = Context.Parameters[0];
            float cooldown = 0;
            if (Context.Parameters.Count > 1)
            {
                cooldown = await Context.Parameters.GetAsync<float>(1);
            }

            var kits = await m_KitManager.GetRegisteredKitsAsync();
            if (kits.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new UserFriendlyException("Kit with the same name already exists");
            }

            var hasInventory = (IHasInventory)playerUser.Player;
            if (hasInventory == null)
            {
                throw new NotSupportedException("IPlayer doesn't have compatibility IHasInventory");
            }
            var items = hasInventory.Inventory.SelectMany(x => x.Items.Select(c => c.Item));
            if (!items.Any())
            {
                await PrintAsync(m_StringLocalizer["commands:kit:create:noItems"], Color.Red);
                return;
            }
            var kit = new Kit
            {
                Cooldown = cooldown,
                Items = items.Select(x => x.ConvertIItemToKitItem()).ToList(),
                Name = name
            };
            await m_KitManager.AddKitAsync(kit);
            await PrintAsync(m_StringLocalizer["commands:kit:create:success", new { Kit = kit }]);
        }
    }
}
