using System;
using System.IO;
using OpenMod.Extensions.Games.Abstractions.Items;

namespace Kits.API
{
    public class KitItemState : IItemState
    {
        public double ItemQuality { get; set; }

        public double ItemDurability { get; set; }

        public double ItemAmount { get; set; }

        public byte[] StateData { get; set; }

        public KitItemState() : this(null)
        {
        }

        public KitItemState(IItemState? itemState)
        {
            ItemAmount = itemState?.ItemAmount ?? 0;
            ItemDurability = itemState?.ItemDurability ?? 0;
            ItemQuality = itemState?.ItemQuality ?? 0;
            StateData = itemState?.StateData ?? Array.Empty<byte>();
        }

        public void Serialize(BinaryWriter bw)
        {
            bw.Write(ItemQuality);
            bw.Write(ItemDurability);
            bw.Write(ItemAmount);
            bw.Write(StateData.Length);
            bw.Write(StateData);
        }

        public void Deserialize(BinaryReader br)
        {
            ItemQuality = br.ReadDouble();
            ItemDurability = br.ReadDouble();
            ItemAmount = br.ReadDouble();
            StateData = br.ReadBytes(br.ReadInt32());
        }
    }
}