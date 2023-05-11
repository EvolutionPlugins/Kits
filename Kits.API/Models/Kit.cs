using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.Extensions.Economy.Abstractions;
using OpenMod.Extensions.Games.Abstractions.Items;
using OpenMod.Extensions.Games.Abstractions.Players;
using OpenMod.Extensions.Games.Abstractions.Vehicles;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Kits.API.Models;

public sealed class Kit
{
    [YamlIgnore]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [StringLength(25)]
    [Required]
    public string Name { get; set; } = string.Empty;

    public float Cooldown { get; set; }

    [Column(TypeName = "decimal(18,2)")] // by default it creates decimal(65, 30)
    public decimal Cost { get; set; }

    [Column(TypeName = "decimal(18,2)")] // by default it creates decimal(65, 30)
    public decimal Money { get; set; }

    [StringLength(5)]
    public string? VehicleId { get; set; }

    [MaxLength(ushort.MaxValue)]
    [Column(TypeName = "blob")] // by default it creates longblob (2^32 - 1), we need only ushort.MaxValue length ( 65535 )
    public List<KitItem>? Items { get; set; }

    public async Task GiveKitToPlayer(IPlayerUser playerUser, IServiceProvider serviceProvider)
    {
        var stringLocalizer = serviceProvider.GetRequiredService<IStringLocalizer>();
        var logger = serviceProvider.GetRequiredService<ILogger<Kit>>();

        if (Items != null && playerUser.Player is IHasInventory hasInventory && hasInventory.Inventory != null)
        {
            var itemSpawner = serviceProvider.GetRequiredService<IItemSpawner>();

            foreach (var item in Items)
            {
                var result = await itemSpawner.GiveItemAsync(hasInventory.Inventory, item.ItemAssetId,
                        item.State);
                if (result == null)
                {
                    logger.LogWarning("Item {Id} was unable to give to player {Name})", item.ItemAssetId, playerUser.FullActorName);
                }
            }
        }

        if (!string.IsNullOrEmpty(VehicleId))
        {
            var vehicleSpawner = serviceProvider.GetRequiredService<IVehicleSpawner>();

            var result = await vehicleSpawner.SpawnVehicleAsync(playerUser.Player, VehicleId!);
            if (result == null)
            {
                logger.LogWarning("Vehicle {Id} was unable to give to player {Name})", VehicleId, playerUser.FullActorName);
            }
        }

        if (Money != 0)
        {
            var economyProvider = serviceProvider.GetRequiredService<IEconomyProvider>();

            await economyProvider.UpdateBalanceAsync(playerUser.Id, playerUser.Type, Money,
                stringLocalizer["commands:kit:balanceUpdateReason:got"]);
        }
    }
}
