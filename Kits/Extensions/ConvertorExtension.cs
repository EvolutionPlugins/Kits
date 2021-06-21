using Kits.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenMod.Extensions.Games.Abstractions.Items;
using SDG.Framework.Debug.Parsers;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kits.Extensions
{
    public static class ConvertorExtension
    {
        public static KitItem ConvertIItemToKitItem(this IItem item)
        {
            return new(item.Asset.ItemAssetId, item.State);
        }

        public static byte[] ConvertToByteArray(this IList<KitItem> items)
        {
            if (items == null)
            {
                return Array.Empty<byte>();
            }

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write(items.Count);

            foreach (var item in items)
            {
                item.Serialize(bw);
            }

            return ms.ToArray();
        }

        public static List<KitItem> ConvertToKitItems(this byte[] block)
        {
            var output = new List<KitItem>();

            if (block == null)
            {
                return output;
            }

            using var ms = new MemoryStream();
            using var br = new BinaryReader(ms);

            for (var i = 0; i < br.ReadInt32(); i++)
            {
                var kitItem = new KitItem();
                kitItem.Deserialize(br);

                output.Add(kitItem);
            }

            return output;
        }

        public static PropertyBuilder<IList<KitItem>?> HasByteArrayConversion(this PropertyBuilder<IList<KitItem>?> propertyBuilder)
        {
            var converter = new ValueConverter<IList<KitItem>?, byte[]>
            (
            v => ConvertToByteArray(v!),
            v => ConvertToKitItems(v)
            );

            var comparer = new ValueComparer<IList<KitItem>>
            (
                (l, r) => l == r,
                v => v == null ? 0 : v.GetHashCode(),
                v => ConvertToKitItems(ConvertToByteArray(v))
            );

            propertyBuilder.HasConversion(converter);
            propertyBuilder.Metadata.SetValueConverter(converter);
            propertyBuilder.Metadata.SetValueComparer(comparer);
            propertyBuilder.HasColumnType("Items");

            return propertyBuilder;
        }
    }
}