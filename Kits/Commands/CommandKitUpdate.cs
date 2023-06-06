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

namespace Kits.Commands;
[Command("update")]
[CommandParent(typeof(CommandKit))]
[CommandActor(typeof(IPlayerUser))]
[CommandSyntax("<name>")]
public class CommandKitUpdate : Command
{
    private readonly IKitStore m_KitStore;
    private readonly IStringLocalizer m_StringLocalizer;

    public CommandKitUpdate(IServiceProvider serviceProvider, IKitStore kitStore, IStringLocalizer stringLocalizer) : base(serviceProvider)
    {
        m_KitStore = kitStore;
        m_StringLocalizer = stringLocalizer;
    }

    protected override async Task OnExecuteAsync()
    {
        if (Context.Parameters.Count != 1)
        {
            throw new CommandWrongUsageException(Context);
        }

        var name = Context.Parameters[0];
        var kit = await m_KitStore.FindKitByNameAsync(name) ?? throw new UserFriendlyException(m_StringLocalizer["commands:kit:notFound",
            new
            {
                Name = name
            }]);

        var user = (IPlayerUser)Context.Actor;
        if (user.Player is not IHasInventory hasInventory)
        {
            throw new Exception("IPlayer doesn't have compatibility IHasInventory");
        }

        kit.Items = hasInventory.Inventory!
            .SelectMany(x => x.Items
                .Select(c => c.Item.ConvertIItemToKitItem()))
            .ToList();
        await m_KitStore.UpdateKitAsync(kit);

        await PrintAsync("Updated the kit.");
    }
}
