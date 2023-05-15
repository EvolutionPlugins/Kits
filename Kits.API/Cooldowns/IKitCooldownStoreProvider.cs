using System.Threading.Tasks;

namespace Kits.API.Cooldowns;
public interface IKitCooldownStoreProvider : IKitCooldownStore
{
    Task InitAsync();
}
