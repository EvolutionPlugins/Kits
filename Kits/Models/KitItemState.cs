using OpenMod.Extensions.Games.Abstractions.Items;
using System;

namespace Kits.Models
{
    [Serializable]
    public class KitItemState : IItemState
    {
        public KitItemState()
        {
        }

        public KitItemState(IItemState itemState)
        {
            ItemAmount = itemState.ItemAmount;
            ItemDurability = itemState.ItemDurability;
            ItemQuality = itemState.ItemQuality;
            StateData = itemState.StateData;
        }

        public double ItemQuality { get; set; }

        public double ItemDurability { get; set; }

        public double ItemAmount { get; set; }

#pragma warning disable CA1819 // Properties should not return arrays
        public byte[] StateData { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
