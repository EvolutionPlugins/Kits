using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Kits.API;
using Kits.Extensions;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.Core.Commands;
using OpenMod.Extensions.Games.Abstractions.Items;
using OpenMod.Extensions.Games.Abstractions.Players;

namespace Kits.Commands
{
    [Command("create")]
    [CommandAlias("add")]
    [CommandAlias("+")]
    [CommandActor(typeof(IPlayerUser))]
    [CommandParent(typeof(CommandKit))]
    [CommandSyntax("<name> [cooldown] [cost] [money] [vehicleId]")]
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
            if (Context.Parameters.Count is < 1 or > 5)
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

            var cooldown = Context.Parameters.Count >= 2
                ? await Context.Parameters.GetAsync<TimeSpan>(1)
                : TimeSpan.Zero;
            var cost = Context.Parameters.Count >= 3 ? await Context.Parameters.GetAsync<decimal>(2) : 0;
            var money = Context.Parameters.Count >= 4 ? await Context.Parameters.GetAsync<decimal>(3) : 0;
            var vehicleId = Context.Parameters.Count == 5 ? Context.Parameters[4] : null;

            var shouldForceCreate = cost != 0 || money != 0 || !string.IsNullOrEmpty(vehicleId);

            var kits = await m_KitStore.GetKitsAsync();
            if (kits.Any(x => x.Name?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:exist", new { Name = name }]);
            }

            var items = hasInventory.Inventory!.SelectMany(x => x.Items.Select(c => c.Item)).ToList();
            if (!shouldForceCreate && items.Count == 0)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:create:noItems"]);
            }

            var kit = new Kit
            {
                Cooldown = (float)cooldown.TotalSeconds,
                Items = items.ConvertAll(x => x.ConvertIItemToKitItem()),
                Name = name,
                Cost = cost,
                Money = money,
                VehicleId = vehicleId
            };

            // UnturnedExtension.AddClothes(playerUser, kit.Items);

            await m_KitStore.AddKitAsyc(kit);
            await PrintAsync(m_StringLocalizer["commands:kit:create:success", new { Kit = kit }]);
        }
    }
}