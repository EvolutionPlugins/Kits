using OpenMod.Extensions.Games.Abstractions.Items;

namespace Kits.API
{
    public class KitItem
    {
        public KitItem() : this(null!, null!)
        {
        }

        public KitItem(string itemAssetId, IItemState itemState)
        {
            ItemAssetId = itemAssetId ?? string.Empty;
            State = new KitItemState(itemState);
        }

        public string ItemAssetId { get; set; }

        public KitItemState State { get; set; }
    }
}
