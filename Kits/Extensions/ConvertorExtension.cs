﻿using Kits.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenMod.Extensions.Games.Abstractions.Items;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kits.Extensions
{
    public static class ConvertorExtension
    {
        public static readonly byte s_SaveVersion = 1;

        public static KitItem ConvertIItemToKitItem(this IItem item)
        {
            return new(item.Asset.ItemAssetId, item.State);
        }

        public static byte[] ConvertToByteArray(this IList<KitItem>? items)
        {
            if (items == null)
            {
                return Array.Empty<byte>();
            }

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write(s_SaveVersion);
            bw.Write(items.Count);

            foreach (var item in items)
            {
                item.Serialize(bw);
            }

            return ms.ToArray();
        }

        public static List<KitItem> ConvertToKitItems(this byte[]? block)
        {
            var output = new List<KitItem>();

            if (block == null)
            {
                return output;
            }

            using var ms = new MemoryStream();
            using var br = new BinaryReader(ms);

            br.ReadByte(); // save version, for now ignored

            for (var i = 0; i < br.ReadInt32(); i++)
            {
                var kitItem = new KitItem();
                kitItem.Deserialize(br);

                output.Add(kitItem);
            }

            return output;
        }

        // https://stackoverflow.com/questions/44829824/how-to-store-json-in-an-entity-field-with-ef-core
        public static PropertyBuilder<IList<KitItem>?> HasByteArrayConversion(this PropertyBuilder<IList<KitItem>?> propertyBuilder)
        {
            var converter = new ValueConverter<IList<KitItem>?, byte[]>
            (
            v => v.ConvertToByteArray(),
            v => v.ConvertToKitItems()
            );

            var comparer = new ValueComparer<IList<KitItem>?>
            (
                (l, r) => l == r,
                v => v == null ? 0 : v.GetHashCode(),
                v => v.ConvertToByteArray().ConvertToKitItems()
            );

            propertyBuilder.HasConversion(converter);
            propertyBuilder.Metadata.SetValueConverter(converter);
            propertyBuilder.Metadata.SetValueComparer(comparer);
            //propertyBuilder.HasColumnType("longblob");

            return propertyBuilder;
        }
    }
}