using OpenMod.Extensions.Games.Abstractions.Items;
using System.IO;

namespace Kits.API
{
    public class KitItem
    {
        public string ItemAssetId { get; set; }

        public KitItemState State { get; set; }


        public KitItem() : this(null, null)
        {
        }

        public KitItem(string? itemAssetId, IItemState? itemState)
        {
            ItemAssetId = itemAssetId ?? string.Empty;
            State = new KitItemState(itemState);
        }

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(ItemAssetId);
            State.Serialize(bw);
        }

        public void Deserialize(BinaryReader br)
        {
            ItemAssetId = br.ReadString();
            State.Deserialize(br);
        }
    }
}