using Kits.Models;
using OpenMod.Extensions.Games.Abstractions.Items;

namespace Kits.Extensions
{
    public static class ConvertorExtension
    {
        public static KitItem ConvertIItemToKitItem(this IItem item)
        {
            return new KitItem(item.Asset.ItemAssetId, item.State);
        }
    }
}
