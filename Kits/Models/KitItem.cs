using OpenMod.Extensions.Games.Abstractions.Items;
using System;

namespace Kits.Models
{
    [Serializable]
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
