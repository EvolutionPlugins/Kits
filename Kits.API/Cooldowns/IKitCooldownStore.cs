using System;
using System.Threading.Tasks;
using OpenMod.API.Ioc;
using OpenMod.Extensions.Games.Abstractions.Players;

namespace Kits.API.Cooldowns;

[Service]
public interface IKitCooldownStore
{
    Task<TimeSpan?> GetLastCooldownAsync(IPlayerUser player, string kitName);

    Task RegisterCooldownAsync(IPlayerUser player, string kitName, DateTime time);
}