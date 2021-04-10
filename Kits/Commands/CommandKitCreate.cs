using JetBrains.Annotations;
using Kits.API;
using Kits.Extensions;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.Core.Commands;
using OpenMod.Extensions.Games.Abstractions.Items;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Kits.Commands
{
    [Command("create")]
    [CommandAlias("add")]
    [CommandAlias("+")]
    [CommandActor(typeof(IPlayerUser))]
    [CommandParent(typeof(CommandKit))]
    [CommandSyntax("<name> [cooldown]")]
    [UsedImplicitly]
    public class CommandKitCreate : Command
    {
        private readonly IKitStore m_KitStore;
        private readonly IStringLocalizer m_StringLocalizer;

        public CommandKitCreate(IServiceProvider serviceProvider, IKitStore kitStore,
            IStringLocalizer stringLocalizer) : base(serviceProvider)
        {
            m_KitStore = kitStore;
            m_StringLocalizer = stringLocalizer;
        }

        protected override async Task OnExecuteAsync()
        {
            if (Context.Parameters.Count < 1)
            {
                throw new CommandWrongUsageException(Context);
            }

            var playerUser = (IPlayerUser)Context.Actor;
            var name = Context.Parameters[0];

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (playerUser.Player is not IHasInventory hasInventory)
            {
                throw new UserFriendlyException("IPlayer doesn't have compatibility IHasInventory");
            }

            var cooldown = TimeSpan.Zero;
            if (Context.Parameters.Count > 1)
            {
                cooldown = await Context.Parameters.GetAsync<TimeSpan>(1);
            }

            var kits = await m_KitStore.GetKits();
            if (kits.Any(x => x.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                throw new UserFriendlyException("Kit with the same name already exists");
            }

            var items = hasInventory.Inventory!.SelectMany(x => x.Items.Select(c => c.Item)).ToList();
            if (items.Count == 0)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:create:noItems"]);
            }

            var kit = new Kit
            {
                Cooldown = (float)cooldown.TotalSeconds,
                Items = items.Select(x => x.ConvertIItemToKitItem()).ToList(),
                Name = name,
                Cost = 0,
                Money = 0
            };

            UnturnedExtension.AddClothes(playerUser, kit.Items);

            await m_KitStore.AddKit(kit);
            await PrintAsync(m_StringLocalizer["commands:kit:create:success", new { Kit = kit }]);
        }
    }
}