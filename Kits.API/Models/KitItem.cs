using OpenMod.Extensions.Games.Abstractions.Items;
using System;
using System.IO;
using System.Linq;

namespace Kits.API.Models;

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

    public override int GetHashCode()
    {
        var itemStateDataHash = State.StateData.Aggregate(new HashCode(), (hash, i) =>
        {
            hash.Add(i);
            return hash;
        }).ToHashCode();
        return HashCode.Combine(ItemAssetId, State.ItemAmount, State.ItemQuality, State.ItemDurability, itemStateDataHash);
    }
}