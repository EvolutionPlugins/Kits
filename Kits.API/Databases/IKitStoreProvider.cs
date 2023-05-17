using System.Threading.Tasks;

namespace Kits.API.Databases;

public interface IKitStoreProvider : IKitStore
{
    Task InitAsync();
}