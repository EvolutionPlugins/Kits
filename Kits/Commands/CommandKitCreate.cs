using JetBrains.Annotations;
using Kits.API;
using Kits.Extensions;
using Microsoft.Extensions.Localization;
using OpenMod.API.Commands;
using OpenMod.Core.Commands;
using OpenMod.Extensions.Games.Abstractions.Items;
using OpenMod.Extensions.Games.Abstractions.Players;
using OpenMod.Extensions.Games.Abstractions.Vehicles;
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
    [CommandSyntax("<name> [cooldown] [cost] [money] [vehicleId]")]
    [UsedImplicitly]
    public class CommandKitCreate : Command
    {
        private static readonly bool s_IsUnturned = AppDomain.CurrentDomain.GetAssemblies()
            .Any(x => x.GetName().Name.Equals("OpenMod.Unturned.Module.Shared"));

        private readonly IKitDatabaseManager m_KitStore;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly IVehicleDirectory m_VehicleDirectory;

        public CommandKitCreate(IServiceProvider serviceProvider, IKitDatabaseManager kitStore,
            IStringLocalizer stringLocalizer, IVehicleDirectory vehicleDirectory) : base(serviceProvider)
        {
            m_KitStore = kitStore;
            m_StringLocalizer = stringLocalizer;
            m_VehicleDirectory = vehicleDirectory;
        }

        protected override async Task OnExecuteAsync()
        {
            if (Context.Parameters.Count is < 1 or > 5)
            {
                throw new CommandWrongUsageException(Context);
            }

            var playerUser = (IPlayerUser)Context.Actor;
            if (playerUser.Player is not IHasInventory hasInventory)
            {
                throw new UserFriendlyException("IPlayer doesn't have compatibility IHasInventory");
            }

            var name = Context.Parameters[0];
            var cooldown = Context.Parameters.Count >= 2
                ? await Context.Parameters.GetAsync<TimeSpan>(1)
                : TimeSpan.Zero;
            var cost = Context.Parameters.Count >= 3 ? await Context.Parameters.GetAsync<decimal>(2) : 0;
            var money = Context.Parameters.Count >= 4 ? await Context.Parameters.GetAsync<decimal>(3) : 0;
            var vehicleId = Context.Parameters.Count == 5 ? Context.Parameters[4] : null;

            var shouldForceCreate = cost != 0 || money != 0 || !string.IsNullOrEmpty(vehicleId);

            if (cooldown < TimeSpan.Zero)
            {
                throw new UserFriendlyException("The cooldown cannot be negative!");
            }

            if (cost < 0)
            {
                throw new UserFriendlyException("The cost cannot be negative!");
            }

            if (money < 0)
            {
                throw new UserFriendlyException("The money cannot be negative!");
            }

            if (!string.IsNullOrEmpty(vehicleId) && await m_VehicleDirectory.FindByIdAsync(vehicleId!) == null)
            {
                throw new UserFriendlyException($"The vehicle {vehicleId} not found");
            }

            var kits = await m_KitStore.GetKitsAsync();
            if (kits.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:exist", new { Name = name }]);
            }

            var items = hasInventory.Inventory!
                .SelectMany(x => x.Items
                    .Select(c => c.Item.ConvertIItemToKitItem()))
                .ToList();

            if (!shouldForceCreate && items.Count == 0)
            {
                throw new UserFriendlyException(m_StringLocalizer["commands:kit:create:noItems"]);
            }

            var kit = new Kit
            {
                Cooldown = (float)cooldown.TotalSeconds,
                Items = items,
                Name = name,
                Cost = cost,
                Money = money,
                VehicleId = vehicleId
            };

            if (s_IsUnturned)
            {
                UnturnedExtension.AddClothes(playerUser, kit.Items);
            }

            await m_KitStore.AddKitAsync(kit);
            await PrintAsync(m_StringLocalizer["commands:kit:create:success", new { Kit = kit }]);
        }
    }
}