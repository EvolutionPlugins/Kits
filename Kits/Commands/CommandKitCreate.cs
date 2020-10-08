using Kits.API;
using Kits.Extensions;
using OpenMod.API.Commands;
using OpenMod.Core.Commands;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Linq;
using System.Threading.Tasks;
using IHasInventory = OpenMod.Extensions.Games.Abstractions.Entities.IHasInventory;

namespace Kits.Commands
{
    [Command("create")]
    [CommandActor(typeof(IPlayerUser))]
    [CommandParent(typeof(CommandKit))]
    [CommandSyntax("<name> [cooldown]")]
    public class CommandKitCreate : Command
    {
        private readonly IKitManager m_KitManager;

        public CommandKitCreate(IServiceProvider serviceProvider, IKitManager kitManager) : base(serviceProvider)
        {
            m_KitManager = kitManager;
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

            var kits = await m_KitManager.GetKits();
            if (kits.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                // todo: add custom exception ArgumentException extended by UserFriendlyException
                throw new UserFriendlyException("Kit with the same name already exists");
            }

            var hasInventory = (IHasInventory)playerUser.Player;
            if (hasInventory == null)
            {
                throw new NotSupportedException("IPlayer doesn't have compobillity IHasInventory");
            }
            var items = hasInventory.Inventory.SelectMany(x => x.Items.Select(c => c.Item));
            if (!items.Any())
            {
                // todo: shows a message
                return;
            }
            var kit = new Kit
            {
                Cooldown = cooldown,
                Items = items.Select(x => x.ConvertIItemToKitItem()).ToList(),
                Name = name
            };
            await m_KitManager.AddKit(kit);
        }
    }
}
