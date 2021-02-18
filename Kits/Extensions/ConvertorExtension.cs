using Kits.API;
using OpenMod.Extensions.Games.Abstractions.Items;

namespace Kits.Extensions
{
    internal static class ConvertorExtension
    {
        public static KitItem ConvertIItemToKitItem(this IItem item)
        {
            return new KitItem(item.Asset.ItemAssetId, item.State);
        }
    }
}
