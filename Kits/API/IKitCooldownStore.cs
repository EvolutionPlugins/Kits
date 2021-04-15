using System;
using System.Threading.Tasks;
using OpenMod.API.Ioc;
using OpenMod.Extensions.Games.Abstractions.Players;

namespace Kits.API
{
    [Service]
    public interface IKitCooldownStore
    {
        Task<TimeSpan?> GetLastCooldown(IPlayerUser player, string kitName);

        Task RegisterCooldown(IPlayerUser player, string kitName, DateTime time);
    }
}