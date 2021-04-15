using System;
using System.Collections.Generic;
using System.IO;
using Kits.API;
using Microsoft.Extensions.Logging;
using OpenMod.Extensions.Games.Abstractions.Items;

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

        public static byte[] ConvertToByteArray(this IReadOnlyList<KitItem> items, ILogger? logger = null)
        {
            lock (s_Lock)
            {
                try
                {
                    Array.Clear(s_Buffer, 0, s_Buffer.Length);
                    using var stream = new MemoryStream(s_Buffer, 0, s_Buffer.Length, true);
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

                    writer.Flush();

                    return stream.ToArray();
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Failed to convert items to data byte array");
                }
            }

            return Array.Empty<byte>();
        }

        public static List<KitItem> ConvertToKitItems(byte[] block, ILogger? logger = null)
        {
            var list = new List<KitItem>();

            try
            {
                using var stream = new MemoryStream(block, false);
                using var reader = new BinaryReader(stream);

                var count = reader.ReadInt32();

                for (var i = 0; i < count; i++)
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
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Error occur on deserializing the data");
                logger?.LogDebug("Hash data: {Data}", Convert.ToBase64String(block));
            }

            return list;
        }
    }
}