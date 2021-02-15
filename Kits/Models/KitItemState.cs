using OpenMod.Extensions.Games.Abstractions.Items;
using System;

namespace Kits.Models
{
    [Serializable]
    public class KitItemState : IItemState
    {
        public KitItemState() : this(null!)
        {
        }

        public KitItemState(IItemState itemState)
        {
            ItemAmount = itemState?.ItemAmount ?? 0;
            ItemDurability = itemState?.ItemDurability ?? 0;
            ItemQuality = itemState?.ItemQuality ?? 0;
            StateData = itemState?.StateData ?? Array.Empty<byte>();
        }

        public double ItemQuality { get; set; }

        public double ItemDurability { get; set; }

        public double ItemAmount { get; set; }

#pragma warning disable CA1819 // Properties should not return arrays
        public byte[] StateData { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
