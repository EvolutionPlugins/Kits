using Kits.API.Models;
using Kits.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using OpenMod.EntityFrameworkCore;
using OpenMod.EntityFrameworkCore.Configurator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kits.Databases.MySql;

public class KitsDbContext : OpenModDbContext<KitsDbContext>
{
    public DbSet<Kit> Kits => Set<Kit>();

    public KitsDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public KitsDbContext(IDbContextConfigurator configurator, IServiceProvider serviceProvider) : base(configurator, serviceProvider)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // https://stackoverflow.com/questions/44829824/how-to-store-json-in-an-entity-field-with-ef-core
        var property = modelBuilder
            .Entity<Kit>()
            .Property(x => x.Items);

        property.HasConversion(
            v => v.ConvertToByteArray(),
            v => v.ConvertToKitItems());

        var comparer = new ValueComparer<List<KitItem>?>(
            (c1, c2) => c1.SequenceEqual(c2),
            c => c.Aggregate(0, (a, i) => HashCode.Combine(a, i.GetHashCode())),
            c => c.ToList());

        property.Metadata.SetValueComparer(comparer);
    }
}
