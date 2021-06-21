using System;
using System.Collections.Generic;
using System.Linq;
using Kits.API;
using OpenMod.Extensions.Games.Abstractions.Players;
using SDG.Unturned;
using Steamworks;

namespace Kits.Extensions
{
    public static class UnturnedExtension
    {
        private static readonly bool s_IsUnturned = AppDomain.CurrentDomain.GetAssemblies()
            .Any(x => x.GetName().Name.Equals("OpenMod.Unturned"));

        public static void AddClothes(IPlayerUser user, IList<KitItem> kitsItems)
        {
            if (!s_IsUnturned)
            {
                return;
            }

            if (!ulong.TryParse(user.Id, out var id))
            {
                return;
            }

            var player = PlayerTool.getPlayer((CSteamID)id);
            if (player == null)
            {
                return;
            }

            var clothing = player.clothing;

            if (clothing.hat != 0)
            {
                kitsItems.Insert(0,
                    new(clothing.hat.ToString(),
                        new KitItemState()
                        {
                            ItemAmount = 1,
                            ItemDurability = clothing.hatQuality,
                            ItemQuality = clothing.hatQuality,
                            StateData = clothing.hatState
                        }));
            }

            if (clothing.glasses != 0)
            {
                kitsItems.Insert(0,
                    new(clothing.glasses.ToString(),
                        new KitItemState()
                        {
                            ItemAmount = 1,
                            ItemDurability = clothing.glassesQuality,
                            ItemQuality = clothing.glassesQuality,
                            StateData = clothing.glassesState
                        }));
            }

            if (clothing.mask != 0)
            {
                kitsItems.Insert(0,
                    new(clothing.mask.ToString(),
                        new KitItemState()
                        {
                            ItemAmount = 1,
                            ItemDurability = clothing.maskQuality,
                            ItemQuality = clothing.maskQuality,
                            StateData = clothing.maskState
                        }));
            }

            if (clothing.pants != 0)
            {
                kitsItems.Insert(0,
                    new(clothing.pants.ToString(),
                        new KitItemState()
                        {
                            ItemAmount = 1,
                            ItemDurability = clothing.pantsQuality,
                            ItemQuality = clothing.pantsQuality,
                            StateData = clothing.pantsState
                        }));
            }

            if (clothing.vest != 0)
            {
                kitsItems.Insert(0,
                    new(clothing.vest.ToString(),
                        new KitItemState()
                        {
                            ItemAmount = 1,
                            ItemDurability = clothing.vestQuality,
                            ItemQuality = clothing.vestQuality,
                            StateData = clothing.vestState
                        }));
            }

            if (clothing.shirt != 0)
            {
                kitsItems.Insert(0,
                    new(clothing.shirt.ToString(),
                        new KitItemState()
                        {
                            ItemAmount = 1,
                            ItemDurability = clothing.shirtQuality,
                            ItemQuality = clothing.shirtQuality,
                            StateData = clothing.shirtState
                        }));
            }

            if (clothing.backpack != 0)
            {
                kitsItems.Insert(0,
                    new(clothing.backpack.ToString(),
                        new KitItemState()
                        {
                            ItemAmount = 1,
                            ItemDurability = clothing.backpackQuality,
                            ItemQuality = clothing.backpackQuality,
                            StateData = clothing.backpackState
                        }));
            }
        }
    }
}