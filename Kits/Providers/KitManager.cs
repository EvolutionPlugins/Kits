using Kits.API;
using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;
using OpenMod.API.Prioritization;
using OpenMod.Extensions.Games.Abstractions.Items;
using OpenMod.Extensions.Games.Abstractions.Players;
using Serilog;
using System.Threading.Tasks;
using IHasInventoryEntity = OpenMod.Extensions.Games.Abstractions.Entities.IHasInventory;

namespace Kits.Providers
{
    [ServiceImplementation(Lifetime = ServiceLifetime.Singleton, Priority = Priority.Lowest)]
    public class KitManager : IKitManager
    {
        private readonly IItemSpawner m_ItemSpawner;

        public KitManager(IItemSpawner itemSpawner)
        {
            m_ItemSpawner = itemSpawner;
        }

        public async Task GiveKit(IPlayer user, Kit kit)
        {
            var hasInvertory = (IHasInventoryEntity)user;
            foreach (var item in kit.Items)
            {
                //await m_ItemSpawner.GiveItemAsync(hasInvertory.Inventory, item);
            }
        }
    }
}
