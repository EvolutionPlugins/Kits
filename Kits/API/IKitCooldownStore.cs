using OpenMod.API.Ioc;
using OpenMod.Extensions.Games.Abstractions.Players;
using System;
using System.Threading.Tasks;

namespace Kits.API
{
    [Service]
    public interface IKitCooldownStore
    {
        Task<TimeSpan?> GetLastCooldown(IPlayerUser player, string kitName);

        Task RegisterCooldown(IPlayerUser player, string kitName, DateTime time);
    }
}