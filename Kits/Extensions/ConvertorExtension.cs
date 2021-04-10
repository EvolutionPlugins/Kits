using Kits.API;
using OpenMod.Extensions.Games.Abstractions.Items;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kits.Extensions
{
    internal static class ConvertorExtension
    {
        internal static readonly byte[] s_Buffer = new byte[ushort.MaxValue];
        internal static readonly object s_Lock = new object();

        public static KitItem ConvertIItemToKitItem(this IItem item)
        {
            return new(item.Asset.ItemAssetId, item.State);
        }

        public static byte[] ConvertToByteArray(this IReadOnlyList<KitItem> items)
        {
            lock (s_Lock)
            {
                Array.Clear(s_Buffer, 0, s_Buffer.Length);

                using var stream = new MemoryStream(s_Buffer, true);
                using var writer = new BinaryWriter(stream);

                writer.Write(items.Count);
                foreach (var item in items)
                {
                    writer.Write(item.ItemAssetId);
                    writer.Write(item.State.ItemAmount);
                    writer.Write(item.State.ItemDurability);
                    writer.Write(item.State.ItemQuality);
                    writer.Write(item.State.StateData.Length);
                    writer.Write(item.State.StateData);
                }

                return stream.ToArray();
            }
        }

        public static List<KitItem> ConvertToKitItems(byte[] block)
        {
            using var stream = new MemoryStream(block, false);
            using var reader = new BinaryReader(stream);
            var list = new List<KitItem>(reader.ReadInt32());

            for (var i = 0; i < list.Capacity; i++)
            {
                list.Add(new()
                {
                    ItemAssetId = reader.ReadString(),
                    State = new()
                    {
                        ItemAmount = reader.ReadDouble(),
                        ItemDurability = reader.ReadDouble(),
                        ItemQuality = reader.ReadDouble(),
                        StateData = reader.ReadBytes(reader.ReadInt32())
                    }
                });
            }

            return list;
        }
    }
}