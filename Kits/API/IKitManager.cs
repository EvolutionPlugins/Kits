using OpenMod.API.Ioc;
using OpenMod.Extensions.Games.Abstractions.Players;
using System.Threading.Tasks;

namespace Kits.API
{
    [Service]
    public interface IKitManager
    {
        Task GiveKit(IPlayer user, Kit kit);
    }
}
