using System.Collections.Generic;
using System.Threading.Tasks;
using Kits.API.Models;
using OpenMod.API.Commands;
using OpenMod.API.Ioc;
using OpenMod.Extensions.Games.Abstractions.Players;

namespace Kits.API;

[Service]
public interface IKitManager
{
    Task GiveKitAsync(IPlayerUser user, string name, ICommandActor? instigator = null, bool forceGiveKit = false);

    Task GiveKitAsync(IPlayerUser user, Kit kit, ICommandActor? instigator = null, bool forceGiveKit = false);

    Task<IReadOnlyCollection<Kit>> GetAvailableKitsForPlayerAsync(IPlayerUser player);
}