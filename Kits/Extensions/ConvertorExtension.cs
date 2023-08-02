using Kits.API.Models;
using OpenMod.Extensions.Games.Abstractions.Items;
using System.Collections.Generic;
using System.IO;

namespace Kits.Extensions;

internal static class ConvertorExtension
{
    public static readonly byte s_SaveVersion = 1;

    public static KitItem ConvertIItemToKitItem(this IItem item)
    {
        return new(item.Asset.ItemAssetId, item.State);
    }

    public static byte[] ConvertToByteArray(this List<KitItem>? items)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write(s_SaveVersion);
        bw.Write(items?.Count ?? 0);

        if (items != null)
        {
            foreach (var item in items)
            {
                item.Serialize(bw);
            }
        }

        return ms.ToArray();
    }

    public static List<KitItem>? ConvertToKitItems(this byte[]? block)
    {
        if (block == null || block.Length == 0)
        {
            return null;
        }

        var output = new List<KitItem>();

        using var ms = new MemoryStream(block, false);
        using var br = new BinaryReader(ms);

        br.ReadByte(); // save version, for now ignored

        var count = br.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            var kitItem = new KitItem();
            kitItem.Deserialize(br);

            output.Add(kitItem);
        }

        return output;
    }
}